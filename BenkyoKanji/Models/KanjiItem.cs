using System.Text.Json.Serialization;

namespace BenkyoKanji.Models;

public enum JlptLevel
{
    All = 0,
    N5 = 5,
    N4 = 4,
    N3 = 3,
    N2 = 2,
    N1 = 1
}

public class KanjiExample
{
    public string Word { get; set; } = string.Empty;
    public string Reading { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
}

public class KanjiItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kanji { get; set; } = string.Empty;
    public string Onyomi { get; set; } = string.Empty;
    public string Kunyomi { get; set; } = string.Empty;
    public string MeaningKo { get; set; } = string.Empty;
    public string MeaningEn { get; set; } = string.Empty;
    public JlptLevel Level { get; set; } = JlptLevel.N5;
    public int StrokeCount { get; set; } = 1;
    public string Radical { get; set; } = string.Empty;
    public List<KanjiExample> Examples { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public bool IsCustom { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public string AllReadings => string.IsNullOrWhiteSpace(Kunyomi) 
        ? Onyomi 
        : string.IsNullOrWhiteSpace(Onyomi) 
            ? Kunyomi 
            : $"{Onyomi} / {Kunyomi}";

    [JsonIgnore]
    public string LevelDisplay => Level == JlptLevel.All ? "All" : $"JLPT {Level}";
}
