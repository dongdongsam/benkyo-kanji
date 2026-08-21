using System.IO;
using BenkyoKanji.Models;
using BenkyoKanji.Services;
using BenkyoKanji.ViewModels;
using Xunit;

namespace BenkyoKanji.Tests;

public class SrsEngineTests
{
    private readonly string _testDir;
    private readonly IJsonStorageService _storage;
    private readonly IKanjiRepository _repo;
    private readonly ISrsEngineService _srs;

    public SrsEngineTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"BenkyoTest_Srs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _storage = new JsonStorageService(_testDir);
        _repo = new KanjiRepository(_storage);
        _srs = new SrsEngineService(_storage, _repo);
    }

    [Fact]
    public async Task ProcessReview_Again_ResetsIntervalAndIncreasesLapses()
    {
        await _srs.InitializeAsync();
        var record = await _srs.ProcessReviewAsync("n5-001", ReviewRating.Again, ReviewSource.DigitalFlashcard);

        Assert.Equal(0, record.Repetitions);
        Assert.Equal(1.0, record.IntervalDays);
        Assert.Equal(1, record.Lapses);
        Assert.Equal(StudyStatus.Learning, record.Status);
    }

    [Fact]
    public async Task ProcessReview_GoodSequence_CalculatesExponentialIntervals()
    {
        await _srs.InitializeAsync();

        // 1st review: Good -> 1 day
        var r1 = await _srs.ProcessReviewAsync("n5-002", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        Assert.Equal(1, r1.Repetitions);
        Assert.Equal(1.0, r1.IntervalDays);
        Assert.Equal(StudyStatus.Learning, r1.Status);

        // 2nd review: Good -> 3 days
        var r2 = await _srs.ProcessReviewAsync("n5-002", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        Assert.Equal(2, r2.Repetitions);
        Assert.Equal(3.0, r2.IntervalDays);
        Assert.Equal(StudyStatus.Reviewing, r2.Status);

        // 3rd review: Good -> 3 * 2.5 = 7.5 days
        var r3 = await _srs.ProcessReviewAsync("n5-002", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        Assert.Equal(3, r3.Repetitions);
        Assert.True(r3.IntervalDays >= 7.0);
        Assert.Equal(StudyStatus.Reviewing, r3.Status);

        // 4th review: Good -> 7.5 * 2.5 = 18.8 days
        var r4 = await _srs.ProcessReviewAsync("n5-002", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        Assert.Equal(4, r4.Repetitions);
        Assert.True(r4.IntervalDays >= 18.0);

        // 5th review: Good -> 18.8 * 2.5 = 47 days -> Mastered
        var r5 = await _srs.ProcessReviewAsync("n5-002", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        Assert.Equal(5, r5.Repetitions);
        Assert.True(r5.IntervalDays >= 21.0);
        Assert.Equal(StudyStatus.Mastered, r5.Status);
    }

    [Fact]
    public async Task RetentionRate_CalculatesProperly()
    {
        await _srs.InitializeAsync();
        await _srs.ProcessReviewAsync("test-1", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        await _srs.ProcessReviewAsync("test-2", ReviewRating.Easy, ReviewSource.DigitalFlashcard);
        await _srs.ProcessReviewAsync("test-3", ReviewRating.Again, ReviewSource.DigitalFlashcard);

        double rate = _srs.GetRetentionRate();
        Assert.Equal(66.7, rate);
    }

    [Fact]
    public async Task UpcomingReviewForecast_PopulatesDays()
    {
        await _srs.InitializeAsync();
        await _srs.ProcessReviewAsync("test-fc-1", ReviewRating.Good, ReviewSource.DigitalFlashcard);
        var forecast = _srs.GetUpcomingReviewForecast(7);

        Assert.Equal(7, forecast.Count);
    }
}

public class KanjiRepositoryTests
{
    private readonly string _testDir;
    private readonly IJsonStorageService _storage;
    private readonly IKanjiRepository _repo;

    public KanjiRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"BenkyoTest_Repo_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _storage = new JsonStorageService(_testDir);
        _repo = new KanjiRepository(_storage);
    }

    [Fact]
    public async Task Search_FindsMatchingKanjiAndMeanings()
    {
        await _repo.InitializeAsync();

        // Search by Japanese character
        var results = _repo.Search("日");
        Assert.NotEmpty(results);

        // Search by Korean meaning
        var resultsKo = _repo.Search("사람");
        Assert.NotEmpty(resultsKo);
        Assert.Contains(resultsKo, k => k.Kanji == "人");

        // Filter by Level
        var n4Results = _repo.Search("", JlptLevel.N4);
        Assert.All(n4Results, k => Assert.Equal(JlptLevel.N4, k.Level));

        var n1Results = _repo.Search("", JlptLevel.N1);
        Assert.NotEmpty(n1Results);
        Assert.All(n1Results, k => Assert.Equal(JlptLevel.N1, k.Level));
    }

    [Fact]
    public async Task AddAndCustomItem_PersistsCorrectly()
    {
        await _repo.InitializeAsync();
        var custom = new KanjiItem
        {
            Id = "custom-test-01",
            Kanji = "猫",
            Onyomi = "ビョウ",
            Kunyomi = "ねこ",
            MeaningKo = "고양이 묘",
            Level = JlptLevel.N4,
            IsCustom = true
        };

        await _repo.AddOrUpdateAsync(custom);

        var retrieved = _repo.GetById("custom-test-01");
        Assert.NotNull(retrieved);
        Assert.Equal("猫", retrieved.Kanji);
        Assert.Equal("고양이 묘", retrieved.MeaningKo);

        // Delete
        await _repo.DeleteAsync("custom-test-01");
        var deleted = _repo.GetById("custom-test-01");
        Assert.Null(deleted);
    }
}

public class PdfWorksheetTests
{
    private readonly IPdfWorksheetService _pdfService;

    public PdfWorksheetTests()
    {
        _pdfService = new PdfWorksheetService();
    }

    [Theory]
    [InlineData(WorksheetType.FullStudyTable)]
    [InlineData(WorksheetType.KanjiQuiz)]
    [InlineData(WorksheetType.ReadingQuiz)]
    [InlineData(WorksheetType.MeaningQuiz)]
    [InlineData(WorksheetType.MixedQuiz)]
    public void GenerateWorksheetPdf_AllTypes_ReturnsValidPdfBytes(WorksheetType type)
    {
        var sampleItems = new List<KanjiItem>
        {
            new() { Kanji = "日", Onyomi = "ニチ", Kunyomi = "ひ", MeaningKo = "날 일", Level = JlptLevel.N5 },
            new() { Kanji = "月", Onyomi = "ゲツ", Kunyomi = "つき", MeaningKo = "달 월", Level = JlptLevel.N5 },
            new() { Kanji = "火", Onyomi = "カ", Kunyomi = "ひ", MeaningKo = "불 화", Level = JlptLevel.N5 }
        };

        var config = _pdfService.CreateWorksheetConfig(type, JlptLevel.N5, 3, sampleItems, "테스트 시험지");
        var pdfBytes = _pdfService.GenerateWorksheetPdf(config);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
        // PDF header magic bytes %PDF
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public void GenerateSampleWorksheetFiles_WritesToSampleFolder()
    {
        var sampleDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "samples");
        Directory.CreateDirectory(sampleDir);

        var sampleItems = new List<KanjiItem>
        {
            new() { Kanji = "日", Onyomi = "ニチ, ジツ", Kunyomi = "ひ, -び", MeaningKo = "날 일, 해", Level = JlptLevel.N5, StrokeCount = 4 },
            new() { Kanji = "本", Onyomi = "ホン", Kunyomi = "もと", MeaningKo = "근본 본, 책", Level = JlptLevel.N5, StrokeCount = 5 },
            new() { Kanji = "人", Onyomi = "ジン, ニン", Kunyomi = "ひと", MeaningKo = "사람 인", Level = JlptLevel.N5, StrokeCount = 2 },
            new() { Kanji = "月", Onyomi = "ゲツ, ガツ", Kunyomi = "つき", MeaningKo = "달 월", Level = JlptLevel.N5, StrokeCount = 4 },
            new() { Kanji = "火", Onyomi = "カ", Kunyomi = "ひ", MeaningKo = "불 화", Level = JlptLevel.N5, StrokeCount = 4 }
        };

        var config = _pdfService.CreateWorksheetConfig(WorksheetType.KanjiQuiz, JlptLevel.N5, 5, sampleItems, "JLPT N5 한자 쓰기 실전 테스트");
        var pdfBytes = _pdfService.GenerateWorksheetPdf(config);

        var pdfPath = Path.Combine(sampleDir, "sample_kanji_quiz.pdf");
        File.WriteAllBytes(pdfPath, pdfBytes);
        Assert.True(File.Exists(pdfPath));

        // Generate sample image for OCR testing
        var visual = new System.Windows.Media.DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new System.Windows.Rect(0, 0, 800, 600));
            
            var typeface = new System.Windows.Media.Typeface("Segoe UI");
            var jpTypeface = new System.Windows.Media.Typeface("Yu Gothic");
            
            dc.DrawText(new System.Windows.Media.FormattedText(
                $"{config.WorksheetId} - JLPT N5 한자 쓰기 테스트",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface, 16, System.Windows.Media.Brushes.Black, 1.0), new System.Windows.Point(20, 20));

            dc.DrawText(new System.Windows.Media.FormattedText(
                "1. 日 (ニチ / ひ) - 날 일",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                jpTypeface, 18, System.Windows.Media.Brushes.DarkSlateBlue, 1.0), new System.Windows.Point(20, 70));

            dc.DrawText(new System.Windows.Media.FormattedText(
                "2. 本 (ホン / もと) - 근본 본, 책",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                jpTypeface, 18, System.Windows.Media.Brushes.DarkSlateBlue, 1.0), new System.Windows.Point(20, 110));

            dc.DrawText(new System.Windows.Media.FormattedText(
                "3. 人 (ジン / ひと) - 사람 인",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                jpTypeface, 18, System.Windows.Media.Brushes.DarkSlateBlue, 1.0), new System.Windows.Point(20, 150));

            dc.DrawText(new System.Windows.Media.FormattedText(
                "4. 月 (ゲツ / つき) - 달 월",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                jpTypeface, 18, System.Windows.Media.Brushes.DarkSlateBlue, 1.0), new System.Windows.Point(20, 190));

            dc.DrawText(new System.Windows.Media.FormattedText(
                "5. 火 (カ / ひ) - 불 화",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                jpTypeface, 18, System.Windows.Media.Brushes.DarkSlateBlue, 1.0), new System.Windows.Point(20, 230));
        }

        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(800, 600, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        rtb.Render(visual);

        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        var imgPath = Path.Combine(sampleDir, "sample_completed_worksheet.png");
        using var fs = File.OpenWrite(imgPath);
        enc.Save(fs);
        Assert.True(File.Exists(imgPath));
    }

    [Fact]
    public void CreateWorksheetConfig_KanjiQuiz_HidesKanjiOnly()
    {
        var sampleItems = new List<KanjiItem>
        {
            new() { Kanji = "木", Onyomi = "モク", Kunyomi = "き", MeaningKo = "나무 목", Level = JlptLevel.N5 }
        };

        var config = _pdfService.CreateWorksheetConfig(WorksheetType.KanjiQuiz, JlptLevel.N5, 1, sampleItems);
        Assert.Single(config.Items);
        Assert.True(config.Items[0].HideKanji);
        Assert.False(config.Items[0].HideReading);
        Assert.False(config.Items[0].HideMeaning);
        Assert.Equal("木", config.Items[0].ExpectedAnswer);
    }
}

public class AutoGradingTests
{
    private readonly IAutoGradingService _gradingService;
    private readonly ISrsEngineService _srs;

    public AutoGradingTests()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"BenkyoTest_Grading_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
        var storage = new JsonStorageService(testDir);
        var repo = new KanjiRepository(storage);
        _srs = new SrsEngineService(storage, repo);
        _gradingService = new AutoGradingService(storage, _srs, repo);
    }

    [Theory]
    [InlineData("にほん", "にほん", 1.0)]
    [InlineData("にほん", "にほんご", 0.75)]
    [InlineData("日", "日", 1.0)]
    [InlineData("날 일", "날 일", 1.0)]
    public void CalculateSimilarity_ReturnsExpectedScore(string src, string target, double expectedMin)
    {
        double sim = _gradingService.CalculateSimilarity(src, target);
        Assert.True(sim >= expectedMin - 0.05);
    }

    [Fact]
    public async Task SyncGradingToSrsAsync_UpdatesStudyRecords()
    {
        await _srs.InitializeAsync();

        var result = new GradingResult
        {
            WorksheetId = "TEST-WS-01",
            TotalQuestions = 2,
            Items = new List<GradingItemResult>
            {
                new()
                {
                    Index = 1,
                    KanjiId = "n5-001",
                    Kanji = "日",
                    Status = GradingStatus.Correct,
                    Similarity = 1.0
                },
                new()
                {
                    Index = 2,
                    KanjiId = "n5-002",
                    Kanji = "本",
                    Status = GradingStatus.Incorrect,
                    Similarity = 0.1
                }
            }
        };

        await _gradingService.SyncGradingToSrsAsync(result);

        Assert.True(result.SyncedToSrs);

        var allRecs = _srs.GetAllRecords();
        Assert.True(allRecs.ContainsKey("n5-001"));
        Assert.True(allRecs.ContainsKey("n5-002"));

        Assert.Equal(1, allRecs["n5-001"].Repetitions);
        Assert.Equal(0, allRecs["n5-002"].Repetitions); // Again resets repetitions
        Assert.Equal(1, allRecs["n5-002"].Lapses);
    }
}

public class JsonStorageBackupTests
{
    [Fact]
    public async Task ExportAndImport_PreservesDataIntegrity()
    {
        var dir1 = Path.Combine(Path.GetTempPath(), $"BenkyoTest_Storage1_{Guid.NewGuid():N}");
        var dir2 = Path.Combine(Path.GetTempPath(), $"BenkyoTest_Storage2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        var storage1 = new JsonStorageService(dir1);
        var storage2 = new JsonStorageService(dir2);

        var repo1 = new KanjiRepository(storage1);
        await repo1.InitializeAsync();
        await repo1.AddOrUpdateAsync(new KanjiItem
        {
            Id = "backup-kanji-1",
            Kanji = "夢",
            Onyomi = "ム",
            Kunyomi = "ゆめ",
            MeaningKo = "꿈 몽",
            Level = JlptLevel.N3
        });

        var backupFile = Path.Combine(dir1, "backup.json");
        await storage1.ExportDataBackupAsync(backupFile);
        Assert.True(File.Exists(backupFile));

        await storage2.ImportDataBackupAsync(backupFile);
        var repo2 = new KanjiRepository(storage2);
        await repo2.InitializeAsync();

        var imported = repo2.GetById("backup-kanji-1");
        Assert.NotNull(imported);
        Assert.Equal("夢", imported.Kanji);
        Assert.Equal("꿈 몽", imported.MeaningKo);
    }
}

public class ValueConverterTests
{
    [Fact]
    public void BoolToVisibilityConverter_HandlesObjectsAndBooleans()
    {
        var conv = new BenkyoKanji.Converters.BoolToVisibilityConverter();
        var invConv = new BenkyoKanji.Converters.BoolToVisibilityConverter { Invert = true };

        // Boolean tests
        Assert.Equal(System.Windows.Visibility.Visible, conv.Convert(true, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, conv.Convert(false, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, invConv.Convert(true, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invConv.Convert(false, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));

        // Object tests (such as SelectedKanji)
        var kanji = new KanjiItem { Kanji = "日" };
        Assert.Equal(System.Windows.Visibility.Visible, conv.Convert(kanji, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, invConv.Convert(kanji, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, conv.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invConv.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NullToVisibilityConverter_HandlesNulls()
    {
        var conv = new BenkyoKanji.Converters.NullToVisibilityConverter();
        var invConv = new BenkyoKanji.Converters.NullToVisibilityConverter { Invert = true };

        Assert.Equal(System.Windows.Visibility.Visible, conv.Convert(new object(), typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, conv.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, invConv.Convert(new object(), typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invConv.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EqualityToBoolConverter_HandlesEnumsAndStrings()
    {
        var conv = new BenkyoKanji.Converters.EqualityToBoolConverter();

        // Enum equality
        Assert.True((bool)conv.Convert(JlptLevel.N5, typeof(bool), JlptLevel.N5, System.Globalization.CultureInfo.InvariantCulture)!);
        Assert.False((bool)conv.Convert(JlptLevel.N5, typeof(bool), JlptLevel.N1, System.Globalization.CultureInfo.InvariantCulture)!);

        // String equality
        Assert.True((bool)conv.Convert("Dashboard", typeof(bool), "Dashboard", System.Globalization.CultureInfo.InvariantCulture)!);
        Assert.False((bool)conv.Convert("Dashboard", typeof(bool), "Study", System.Globalization.CultureInfo.InvariantCulture)!);
    }
}

public class DictionaryViewModelTests
{
    private readonly string _testDir;
    private readonly IJsonStorageService _storage;
    private readonly IKanjiRepository _repo;
    private readonly ISrsEngineService _srs;
    private readonly DictionaryViewModel _vm;

    public DictionaryViewModelTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"BenkyoTest_DictVM_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _storage = new JsonStorageService(_testDir);
        _repo = new KanjiRepository(_storage);
        _srs = new SrsEngineService(_storage, _repo);
        _vm = new DictionaryViewModel(_repo, _srs);
    }

    [Fact]
    public async Task InitializeAndSelectKanji_UpdatesDetailsAndSrsRecord()
    {
        await _vm.InitializeAsync();
        Assert.NotEmpty(_vm.FilteredItems);
        Assert.NotNull(_vm.SelectedKanji);
        Assert.Equal(_vm.FilteredItems[0].Id, _vm.SelectedKanji.Id);
        Assert.NotNull(_vm.SelectedStudyRecord);

        // Select second item
        if (_vm.FilteredItems.Count > 1)
        {
            var second = _vm.FilteredItems[1];
            _vm.SelectedKanji = second;
            Assert.Equal(second.Id, _vm.SelectedKanji.Id);
            Assert.Equal(second.Kanji, _vm.SelectedKanji.Kanji);
            Assert.Equal(second.MeaningKo, _vm.SelectedKanji.MeaningKo);
            Assert.NotNull(_vm.SelectedStudyRecord);
            Assert.Equal(second.Id, _vm.SelectedStudyRecord.KanjiId);
        }

        // Filter and verify SelectedKanji is retained or updated to first matched
        _vm.SearchQuery = "日";
        Assert.NotEmpty(_vm.FilteredItems);
        Assert.NotNull(_vm.SelectedKanji);
        Assert.Contains("日", _vm.SelectedKanji.Kanji);
    }
}