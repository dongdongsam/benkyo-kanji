using System.Text.Json.Serialization;

namespace BenkyoKanji.Models;

public enum GradingStatus
{
    Correct,
    Partial,
    Incorrect,
    Unanswered
}

public class GradingItemResult
{
    public int Index { get; set; }
    public string KanjiId { get; set; } = string.Empty;
    public string Kanji { get; set; } = string.Empty;
    public string TargetField { get; set; } = "Kanji"; // Kanji, Reading, Meaning
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string RecognizedAnswer { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public GradingStatus Status { get; set; } = GradingStatus.Incorrect;
    public string Feedback { get; set; } = string.Empty;
    public bool UserOverridden { get; set; }
}

public class GradingResult
{
    public string WorksheetId { get; set; } = string.Empty;
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int PartialCount { get; set; }
    public int IncorrectCount { get; set; }
    public double ScorePercentage { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public List<GradingItemResult> Items { get; set; } = [];
    public bool SyncedToSrs { get; set; }

    [JsonIgnore]
    public string ScoreDisplay => $"{CorrectCount}/{TotalQuestions} ({ScorePercentage:F1}%)";

    [JsonIgnore]
    public string SummaryText => ScorePercentage >= 90 ? "완벽합니다! 뛰어난 성취입니다."
        : ScorePercentage >= 70 ? "우수한 성적입니다! 틀린 단어들을 복습해 보세요."
        : ScorePercentage >= 50 ? "조금 더 연습이 필요합니다. 복습 주기가 자동 조정됩니다."
        : "망각 곡선 주기에 맞춰 다시 학습할 수 있도록 등록되었습니다.";
}
