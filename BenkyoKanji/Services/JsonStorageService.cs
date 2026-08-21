using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using BenkyoKanji.Models;

namespace BenkyoKanji.Services;

public interface IJsonStorageService
{
    string DataDirectory { get; }
    Task<List<KanjiItem>> LoadKanjiLibraryAsync();
    Task SaveKanjiLibraryAsync(List<KanjiItem> items);
    Task<Dictionary<string, StudyRecord>> LoadStudyRecordsAsync();
    Task SaveStudyRecordsAsync(Dictionary<string, StudyRecord> records);
    Task<UserProfile> LoadUserProfileAsync();
    Task SaveUserProfileAsync(UserProfile profile);
    Task<List<WorksheetConfig>> LoadWorksheetHistoryAsync();
    Task SaveWorksheetHistoryAsync(List<WorksheetConfig> worksheets);
    Task<List<GradingResult>> LoadGradingHistoryAsync();
    Task SaveGradingHistoryAsync(List<GradingResult> results);
    Task ExportDataBackupAsync(string targetFilePath);
    Task ImportDataBackupAsync(string sourceFilePath);
}

public class JsonStorageService : IJsonStorageService
{
    private readonly string _dataDir;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly string _kanjiFile;
    private readonly string _studyFile;
    private readonly string _profileFile;
    private readonly string _worksheetFile;
    private readonly string _gradingFile;

    public string DataDirectory => _dataDir;

    public JsonStorageService(string? customDataDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customDataDir))
        {
            _dataDir = customDataDir;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDir = Path.Combine(appData, "BenkyoKanji", "data");
        }

        Directory.CreateDirectory(_dataDir);

        _kanjiFile = Path.Combine(_dataDir, "kanji_library.json");
        _studyFile = Path.Combine(_dataDir, "study_records.json");
        _profileFile = Path.Combine(_dataDir, "user_profile.json");
        _worksheetFile = Path.Combine(_dataDir, "worksheet_history.json");
        _gradingFile = Path.Combine(_dataDir, "grading_history.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<KanjiItem>> LoadKanjiLibraryAsync()
    {
        if (File.Exists(_kanjiFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_kanjiFile);
                var items = JsonSerializer.Deserialize<List<KanjiItem>>(json, _jsonOptions);
                if (items != null && items.Count > 0)
                {
                    // Check if existing library has old placeholder/untranslated data OR if it's the uncurated 13,000 items
                    bool needsUpgrade = items.Count > 2500 || items.Any(k => !k.IsCustom && (k.MeaningKo.Contains("한자") || k.MeaningKo.Contains("Dream") || k.MeaningKo.Contains("?") || k.MeaningKo.Contains("(") || string.IsNullOrWhiteSpace(k.MeaningKo) || System.Text.RegularExpressions.Regex.IsMatch(k.MeaningKo, "[a-zA-Z]")));
                    if (!needsUpgrade)
                    {
                        return items;
                    }

                    // Upgrade with bundled dataset and merge user's custom kanji
                    var bundled = await LoadBundledDatasetAsync();
                    if (bundled.Count > 0)
                    {
                        var customItems = items.Where(k => k.IsCustom).ToList();
                        foreach (var custom in customItems)
                        {
                            var idx = bundled.FindIndex(b => b.Id == custom.Id || b.Kanji == custom.Kanji);
                            if (idx >= 0) bundled[idx] = custom;
                            else bundled.Add(custom);
                        }
                        await SaveKanjiLibraryAsync(bundled);
                        return bundled;
                    }

                    return items;
                }
            }
            catch
            {
                // Fallback to embedded seed
            }
        }

        // Load bundled dataset
        var seedItems = await LoadBundledDatasetAsync();
        await SaveKanjiLibraryAsync(seedItems);
        return seedItems;
    }

    private async Task<List<KanjiItem>> LoadBundledDatasetAsync()
    {
        // Try local file next to exe or embedded resource
        var localDataset = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "kanji_dataset.json");
        if (File.Exists(localDataset))
        {
            var json = await File.ReadAllTextAsync(localDataset);
            var items = JsonSerializer.Deserialize<List<KanjiItem>>(json, _jsonOptions);
            if (items != null) return items;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("kanji_dataset.json"));
        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var items = JsonSerializer.Deserialize<List<KanjiItem>>(json, _jsonOptions);
                if (items != null) return items;
            }
        }

        return [];
    }

    public async Task SaveKanjiLibraryAsync(List<KanjiItem> items)
    {
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_kanjiFile, json);
    }

    public async Task<Dictionary<string, StudyRecord>> LoadStudyRecordsAsync()
    {
        if (!File.Exists(_studyFile))
        {
            return new Dictionary<string, StudyRecord>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_studyFile);
            return JsonSerializer.Deserialize<Dictionary<string, StudyRecord>>(json, _jsonOptions) 
                   ?? new Dictionary<string, StudyRecord>();
        }
        catch
        {
            return new Dictionary<string, StudyRecord>();
        }
    }

    public async Task SaveStudyRecordsAsync(Dictionary<string, StudyRecord> records)
    {
        var json = JsonSerializer.Serialize(records, _jsonOptions);
        await File.WriteAllTextAsync(_studyFile, json);
    }

    public async Task<UserProfile> LoadUserProfileAsync()
    {
        if (!File.Exists(_profileFile))
        {
            var defaultProfile = new UserProfile();
            await SaveUserProfileAsync(defaultProfile);
            return defaultProfile;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_profileFile);
            return JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions) ?? new UserProfile();
        }
        catch
        {
            return new UserProfile();
        }
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(_profileFile, json);
    }

    public async Task<List<WorksheetConfig>> LoadWorksheetHistoryAsync()
    {
        if (!File.Exists(_worksheetFile)) return [];

        try
        {
            var json = await File.ReadAllTextAsync(_worksheetFile);
            return JsonSerializer.Deserialize<List<WorksheetConfig>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveWorksheetHistoryAsync(List<WorksheetConfig> worksheets)
    {
        var json = JsonSerializer.Serialize(worksheets, _jsonOptions);
        await File.WriteAllTextAsync(_worksheetFile, json);
    }

    public async Task<List<GradingResult>> LoadGradingHistoryAsync()
    {
        if (!File.Exists(_gradingFile)) return [];

        try
        {
            var json = await File.ReadAllTextAsync(_gradingFile);
            return JsonSerializer.Deserialize<List<GradingResult>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveGradingHistoryAsync(List<GradingResult> results)
    {
        var json = JsonSerializer.Serialize(results, _jsonOptions);
        await File.WriteAllTextAsync(_gradingFile, json);
    }

    public async Task ExportDataBackupAsync(string targetFilePath)
    {
        var backup = new
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Kanji = await LoadKanjiLibraryAsync(),
            Records = await LoadStudyRecordsAsync(),
            Profile = await LoadUserProfileAsync(),
            Worksheets = await LoadWorksheetHistoryAsync(),
            Gradings = await LoadGradingHistoryAsync()
        };

        var json = JsonSerializer.Serialize(backup, _jsonOptions);
        await File.WriteAllTextAsync(targetFilePath, json);
    }

    public async Task ImportDataBackupAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return;

        var json = await File.ReadAllTextAsync(sourceFilePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("Kanji", out var kanjiEl))
        {
            var items = JsonSerializer.Deserialize<List<KanjiItem>>(kanjiEl.GetRawText(), _jsonOptions);
            if (items != null) await SaveKanjiLibraryAsync(items);
        }

        if (root.TryGetProperty("Records", out var recEl))
        {
            var records = JsonSerializer.Deserialize<Dictionary<string, StudyRecord>>(recEl.GetRawText(), _jsonOptions);
            if (records != null) await SaveStudyRecordsAsync(records);
        }

        if (root.TryGetProperty("Profile", out var profEl))
        {
            var prof = JsonSerializer.Deserialize<UserProfile>(profEl.GetRawText(), _jsonOptions);
            if (prof != null) await SaveUserProfileAsync(prof);
        }

        if (root.TryGetProperty("Worksheets", out var wsEl))
        {
            var ws = JsonSerializer.Deserialize<List<WorksheetConfig>>(wsEl.GetRawText(), _jsonOptions);
            if (ws != null) await SaveWorksheetHistoryAsync(ws);
        }

        if (root.TryGetProperty("Gradings", out var grEl))
        {
            var gr = JsonSerializer.Deserialize<List<GradingResult>>(grEl.GetRawText(), _jsonOptions);
            if (gr != null) await SaveGradingHistoryAsync(gr);
        }
    }
}
