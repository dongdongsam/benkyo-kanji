using System.Text.Json.Serialization;

namespace BenkyoKanji.Models;

public enum StudyStatus
{
    New = 0,
    Learning = 1,
    Reviewing = 2,
    Mastered = 3
}

public enum ReviewRating
{
    Again = 1,  // Complete blackout / incorrect
    Hard = 2,   // Correct with great difficulty / slow
    Good = 3,   // Correct with reasonable effort
    Easy = 4,   // Perfect recall without hesitation
    Perfect = 5 // Instantly mastered
}

public enum ReviewSource
{
    DigitalFlashcard,
    PhotoWorksheet,
    QuickQuiz,
    ManualUpdate
}

public class ReviewLogEntry
{
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public ReviewRating Rating { get; set; } = ReviewRating.Good;
    public double TimeTakenSeconds { get; set; }
    public ReviewSource Source { get; set; } = ReviewSource.DigitalFlashcard;
    public string? Note { get; set; }
}

public class StudyRecord
{
    public string KanjiId { get; set; } = string.Empty;
    public int Repetitions { get; set; } = 0;
    public double IntervalDays { get; set; } = 0;
    public double EaseFactor { get; set; } = 2.5;
    public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastReviewedDate { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.New;
    public int Lapses { get; set; } = 0;
    public List<ReviewLogEntry> History { get; set; } = [];

    [JsonIgnore]
    public bool IsDue => NextReviewDate <= DateTime.UtcNow;

    [JsonIgnore]
    public int DueInDays => (int)Math.Ceiling((NextReviewDate - DateTime.UtcNow).TotalDays);
}

public class UserProfile
{
    public int DailyNewGoal { get; set; } = 10;
    public int DailyReviewGoal { get; set; } = 20;
    public int CurrentStreak { get; set; } = 0;
    public DateTime? LastStudiedDate { get; set; }
    public List<JlptLevel> TargetLevels { get; set; } = [JlptLevel.N5, JlptLevel.N4, JlptLevel.N3, JlptLevel.N2, JlptLevel.N1];
    public string Theme { get; set; } = "Dark";
    public double AutoGradingThreshold { get; set; } = 0.75;
    public string? OpenAiApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string PreferredVisionEngine { get; set; } = "WindowsOcr"; // "WindowsOcr", "LocalHybrid", "Gemini", "OpenAI"
}
