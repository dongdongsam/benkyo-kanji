using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BenkyoKanji.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BenkyoKanji.Services;

public interface IAutoGradingService
{
    Task<GradingResult> GradeWorksheetPhotoAsync(string imagePath, WorksheetConfig? targetConfig = null);
    Task SyncGradingToSrsAsync(GradingResult result);
    double CalculateSimilarity(string source, string target);
}

public class AutoGradingService : IAutoGradingService
{
    private readonly IJsonStorageService _storageService;
    private readonly ISrsEngineService _srsService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly HttpClient _httpClient = new();

    public AutoGradingService(
        IJsonStorageService storageService, 
        ISrsEngineService srsService, 
        IKanjiRepository kanjiRepo)
    {
        _storageService = storageService;
        _srsService = srsService;
        _kanjiRepo = kanjiRepo;
    }

    public async Task<GradingResult> GradeWorksheetPhotoAsync(string imagePath, WorksheetConfig? targetConfig = null)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("업로드된 이미지 파일을 찾을 수 없습니다.", imagePath);
        }

        var profile = await _srsService.GetUserProfileAsync();
        
        // 1. Perform OCR extraction from image
        string extractedText = await ExtractTextFromImageAsync(imagePath, profile);

        // 2. Identify Worksheet Config if not explicitly provided
        if (targetConfig == null)
        {
            targetConfig = await FindMatchingWorksheetConfigAsync(extractedText);
        }

        // If still null, create a fallback config from due items or library items
        if (targetConfig == null)
        {
            var dueItems = _srsService.GetDueReviewItems();
            var candidates = dueItems.Count > 0 ? dueItems : _kanjiRepo.GetAll();
            targetConfig = new WorksheetConfig
            {
                Title = "자동 감지 시험지",
                QuestionCount = Math.Min(10, candidates.Count)
            };
            int idx = 1;
            foreach (var k in candidates.Take(10))
            {
                targetConfig.Items.Add(new WorksheetItem
                {
                    Index = idx++,
                    KanjiItem = k,
                    HideKanji = true,
                    TargetField = "Kanji",
                    ExpectedAnswer = k.Kanji
                });
            }
        }

        // 3. Grade each item
        var result = new GradingResult
        {
            WorksheetId = targetConfig.WorksheetId,
            ImagePath = imagePath,
            TotalQuestions = targetConfig.Items.Count
        };

        var extractedLines = extractedText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                                          .Select(l => l.Trim())
                                          .Where(l => !string.IsNullOrWhiteSpace(l))
                                          .ToList();

        int correctCount = 0;
        int partialCount = 0;
        int incorrectCount = 0;

        for (int i = 0; i < targetConfig.Items.Count; i++)
        {
            var item = targetConfig.Items[i];
            var expected = item.ExpectedAnswer;
            var targetField = item.TargetField;

            // Find best matching recognized text segment for this question index or content
            var (recognizedAnswer, similarity) = FindBestAnswerMatch(item.Index, expected, extractedLines, extractedText);

            var itemResult = new GradingItemResult
            {
                Index = item.Index,
                KanjiId = item.KanjiItem.Id,
                Kanji = item.KanjiItem.Kanji,
                TargetField = targetField,
                ExpectedAnswer = expected,
                RecognizedAnswer = recognizedAnswer,
                Similarity = similarity
            };

            if (similarity >= profile.AutoGradingThreshold)
            {
                itemResult.Status = GradingStatus.Correct;
                itemResult.Feedback = "정답입니다! 정확하게 작성되었습니다.";
                correctCount++;
            }
            else if (similarity >= 0.45)
            {
                itemResult.Status = GradingStatus.Partial;
                itemResult.Feedback = $"부분 정답 (유사도 {similarity * 100:F0}%). 정답: {expected}";
                partialCount++;
            }
            else if (string.IsNullOrWhiteSpace(recognizedAnswer))
            {
                itemResult.Status = GradingStatus.Unanswered;
                itemResult.Feedback = $"미응답 또는 인식 불가. 정답: {expected}";
                incorrectCount++;
            }
            else
            {
                itemResult.Status = GradingStatus.Incorrect;
                itemResult.Feedback = $"오답. 정답: {expected} (인식: {recognizedAnswer})";
                incorrectCount++;
            }

            result.Items.Add(itemResult);
        }

        result.CorrectCount = correctCount;
        result.PartialCount = partialCount;
        result.IncorrectCount = incorrectCount;
        result.ScorePercentage = result.TotalQuestions > 0 
            ? Math.Round(((correctCount + (partialCount * 0.5)) / result.TotalQuestions) * 100.0, 1) 
            : 0;

        // Save grading result to history
        var history = await _storageService.LoadGradingHistoryAsync();
        history.Insert(0, result);
        await _storageService.SaveGradingHistoryAsync(history);

        return result;
    }

    private async Task<string> ExtractTextFromImageAsync(string imagePath, UserProfile profile)
    {
        var sb = new StringBuilder();

        // Check if Windows Media OCR is available
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(imagePath);
            using var stream = new InMemoryRandomAccessStream();
            using var writer = new DataWriter(stream);
            writer.WriteBytes(fileBytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // Try Japanese OCR first, fallback to Korean / English / User language
            var lang = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
                       ?? OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
                       ?? OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();

            OcrEngine? engine = lang != null ? OcrEngine.TryCreateFromLanguage(lang) : OcrEngine.TryCreateFromUserProfileLanguages();

            if (engine != null && softwareBitmap != null)
            {
                var ocrResult = await engine.RecognizeAsync(softwareBitmap);
                if (ocrResult != null)
                {
                    foreach (var line in ocrResult.Lines)
                    {
                        sb.AppendLine(line.Text);
                    }
                }
            }
        }
        catch
        {
            // Windows OCR fallback
        }

        // If Windows OCR returned empty and AI key is provided
        if (sb.Length == 0 && !string.IsNullOrWhiteSpace(profile.GeminiApiKey))
        {
            try
            {
                var aiResult = await CallGeminiVisionOcrAsync(imagePath, profile.GeminiApiKey);
                if (!string.IsNullOrWhiteSpace(aiResult))
                {
                    return aiResult;
                }
            }
            catch
            {
                // Fallback
            }
        }

        return sb.ToString();
    }

    private async Task<string> CallGeminiVisionOcrAsync(string imagePath, string apiKey)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath);
        var base64 = Convert.ToBase64String(bytes);

        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Extract all text and Japanese/Korean characters from this worksheet image line by line." },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64 } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUrl, content);

        if (response.IsSuccessStatusCode)
        {
            var resJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resJson);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
            return text ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task<WorksheetConfig?> FindMatchingWorksheetConfigAsync(string extractedText)
    {
        // Try regex match for BK-YYYYMMDD-XXXX
        var match = Regex.Match(extractedText, @"BK-\d{8}-\d{4}");
        if (match.Success)
        {
            var wsId = match.Value;
            var history = await _storageService.LoadWorksheetHistoryAsync();
            var found = history.FirstOrDefault(w => w.WorksheetId.Equals(wsId, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
        }

        return null;
    }

    private (string recognized, double similarity) FindBestAnswerMatch(int index, string expected, List<string> lines, string fullText)
    {
        if (string.IsNullOrWhiteSpace(expected)) return (string.Empty, 0.0);

        string cleanExpected = NormalizeAnswer(expected);
        double bestSim = 0.0;
        string bestMatch = string.Empty;

        // Check if question index pattern exists (e.g. "1.", "1 ", "1)")
        string indexPrefix = $"{index}";
        foreach (var line in lines)
        {
            var cleanLine = NormalizeAnswer(line);

            // Exact or substring match
            if (cleanLine.Contains(cleanExpected, StringComparison.OrdinalIgnoreCase))
            {
                return (expected, 1.0);
            }

            // Calculate similarity against entire line or line tokens
            double sim = CalculateSimilarity(cleanLine, cleanExpected);
            if (sim > bestSim)
            {
                bestSim = sim;
                bestMatch = line;
            }

            var tokens = line.Split([' ', ',', '/', ':', '•', '-'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var cleanToken = NormalizeAnswer(token);
                double tokenSim = CalculateSimilarity(cleanToken, cleanExpected);
                if (tokenSim > bestSim)
                {
                    bestSim = tokenSim;
                    bestMatch = token;
                }
            }
        }

        // Check against full text for exact match
        if (fullText.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            return (expected, 1.0);
        }

        return (bestMatch, Math.Round(bestSim, 2));
    }

    private static string NormalizeAnswer(string text)
    {
        return Regex.Replace(text, @"[\s\.,\/#!$%\^&\*;:{}=\-_`~()\[\]]", "").ToLowerInvariant();
    }

    public double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target)) return 1.0;
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase)) return 1.0;

        int distance = ComputeLevenshteinDistance(source, target);
        int maxLen = Math.Max(source.Length, target.Length);
        if (maxLen == 0) return 1.0;

        return 1.0 - ((double)distance / maxLen);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        var d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public async Task SyncGradingToSrsAsync(GradingResult result)
    {
        if (result.SyncedToSrs) return;

        foreach (var item in result.Items)
        {
            if (string.IsNullOrWhiteSpace(item.KanjiId)) continue;

            ReviewRating rating = item.Status switch
            {
                GradingStatus.Correct => item.Similarity >= 0.95 ? ReviewRating.Easy : ReviewRating.Good,
                GradingStatus.Partial => ReviewRating.Hard,
                _ => ReviewRating.Again
            };

            await _srsService.ProcessReviewAsync(item.KanjiId, rating, ReviewSource.PhotoWorksheet);
        }

        result.SyncedToSrs = true;

        // Update history
        var history = await _storageService.LoadGradingHistoryAsync();
        var existing = history.FirstOrDefault(h => h.WorksheetId == result.WorksheetId && h.GradedAt == result.GradedAt);
        if (existing != null)
        {
            existing.SyncedToSrs = true;
            await _storageService.SaveGradingHistoryAsync(history);
        }
    }
}
