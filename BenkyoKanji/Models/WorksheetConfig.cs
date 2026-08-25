using System.Text.Json.Serialization;

namespace BenkyoKanji.Models;

public enum WorksheetType
{
    FullStudyTable,    // Reference sheet (Kanji, Readings, Meanings, Examples all visible)
    KanjiQuiz,         // Kanji column blank, Readings & Meanings visible
    ReadingQuiz,       // Reading column blank, Kanji & Meanings visible
    MeaningQuiz,       // Meaning column blank, Kanji & Readings visible
    MixedQuiz          // Randomly blanks out one or two fields per item
}

public enum StudyCountFilterType
{
    All = 0,             // 전체 한자 (제한 없음)
    UnstudiedOnly = 1,   // 미학습 한자만 (누적 0회)
    LessThan = 2,        // 누적 N회 미만 (< N, 아직 덜 외운 한자 집중 출제)
    AtLeast = 3          // 누적 N회 이상 (>= N, 이미 학습한 한자 복습 테스트)
}

public class WorksheetItem
{
    public int Index { get; set; }
    public KanjiItem KanjiItem { get; set; } = new();
    public bool HideKanji { get; set; }
    public bool HideReading { get; set; }
    public bool HideMeaning { get; set; }
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string TargetField { get; set; } = "Kanji"; // Kanji, Reading, Meaning
}

public class WorksheetConfig
{
    public string WorksheetId { get; set; } = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
    public string Title { get; set; } = "JLPT 일본어 한자/어휘 학습 시험지";
    public string Subtitle { get; set; } = "망각 곡선 기반 주기적 복습 테스트";
    public WorksheetType SheetType { get; set; } = WorksheetType.KanjiQuiz;
    public JlptLevel JlptLevel { get; set; } = JlptLevel.All;
    public int QuestionCount { get; set; } = 20;
    public bool IncludeExamples { get; set; } = true;
    public bool IncludeStrokeCount { get; set; } = true;
    public bool ShowHeaderInfo { get; set; } = true;
    public bool IncludeQrCode { get; set; } = true;
    public bool OnlyDueItems { get; set; } = false;
    public StudyCountFilterType StudyFilterType { get; set; } = StudyCountFilterType.All;
    public int StudyFilterThreshold { get; set; } = 3;
    public List<WorksheetItem> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public string SheetTypeName => SheetType switch
    {
        WorksheetType.FullStudyTable => "전체 학습표 (정답 포함)",
        WorksheetType.KanjiQuiz => "한자 쓰기 테스트 (한자 빈칸)",
        WorksheetType.ReadingQuiz => "음독/훈독 테스트 (읽기 빈칸)",
        WorksheetType.MeaningQuiz => "한국어 뜻 테스트 (뜻 빈칸)",
        WorksheetType.MixedQuiz => "종합 혼합 테스트 (무작위 빈칸)",
        _ => "시험지"
    };
}
