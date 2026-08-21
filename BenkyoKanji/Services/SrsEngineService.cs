using BenkyoKanji.Models;

namespace BenkyoKanji.Services;

public interface ISrsEngineService
{
    Task InitializeAsync();
    StudyRecord GetOrCreateRecord(string kanjiId);
    Task<StudyRecord> ProcessReviewAsync(string kanjiId, ReviewRating rating, ReviewSource source, double timeTakenSeconds = 0);
    IReadOnlyList<KanjiItem> GetDueReviewItems();
    IReadOnlyList<KanjiItem> GetNewLearningItems(int? count = null);
    Task<UserProfile> GetUserProfileAsync();
    Task UpdateUserProfileAsync(UserProfile profile);
    (int newCount, int learningCount, int reviewCount, int masteredCount) GetStudyStats();
    double GetRetentionRate();
    int GetCurrentStreak();
    Dictionary<DateTime, int> GetUpcomingReviewForecast(int days = 7);
    IReadOnlyDictionary<string, StudyRecord> GetAllRecords();
}

public class SrsEngineService : ISrsEngineService
{
    private readonly IJsonStorageService _storageService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly Dictionary<string, StudyRecord> _records = [];
    private UserProfile _profile = new();
    private bool _initialized;

    public SrsEngineService(IJsonStorageService storageService, IKanjiRepository kanjiRepo)
    {
        _storageService = storageService;
        _kanjiRepo = kanjiRepo;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _kanjiRepo.InitializeAsync();
        var records = await _storageService.LoadStudyRecordsAsync();
        _records.Clear();
        foreach (var kvp in records)
        {
            _records[kvp.Key] = kvp.Value;
        }

        _profile = await _storageService.LoadUserProfileAsync();
        UpdateStreakStatus();
        _initialized = true;
    }

    public IReadOnlyDictionary<string, StudyRecord> GetAllRecords() => _records;

    public StudyRecord GetOrCreateRecord(string kanjiId)
    {
        if (!_records.TryGetValue(kanjiId, out var record))
        {
            record = new StudyRecord
            {
                KanjiId = kanjiId,
                Repetitions = 0,
                IntervalDays = 0,
                EaseFactor = 2.5,
                NextReviewDate = DateTime.UtcNow,
                Status = StudyStatus.New,
                Lapses = 0
            };
            _records[kanjiId] = record;
        }

        return record;
    }

    public async Task<StudyRecord> ProcessReviewAsync(string kanjiId, ReviewRating rating, ReviewSource source, double timeTakenSeconds = 0)
    {
        var record = GetOrCreateRecord(kanjiId);
        var now = DateTime.UtcNow;

        var log = new ReviewLogEntry
        {
            ReviewedAt = now,
            Rating = rating,
            TimeTakenSeconds = timeTakenSeconds,
            Source = source
        };
        record.History.Add(log);
        record.LastReviewedDate = now;

        int score = (int)rating; // 1 to 5

        if (rating == ReviewRating.Again)
        {
            // Failed recall (Ebbinghaus reset)
            record.Repetitions = 0;
            record.IntervalDays = 1.0;
            record.Lapses++;
            record.Status = StudyStatus.Learning;
            record.EaseFactor = Math.Max(1.3, record.EaseFactor - 0.20);
            record.NextReviewDate = now.AddDays(1);
        }
        else
        {
            // Successful recall
            if (record.Repetitions == 0)
            {
                record.IntervalDays = rating >= ReviewRating.Easy ? 2.0 : 1.0;
                record.Status = StudyStatus.Learning;
            }
            else if (record.Repetitions == 1)
            {
                record.IntervalDays = rating >= ReviewRating.Easy ? 6.0 : 3.0;
                record.Status = StudyStatus.Reviewing;
            }
            else
            {
                double multiplier = record.EaseFactor;
                if (rating == ReviewRating.Hard) multiplier *= 0.85;
                if (rating == ReviewRating.Easy) multiplier *= 1.30;
                if (rating == ReviewRating.Perfect) multiplier *= 1.50;

                record.IntervalDays = Math.Max(1.0, Math.Round(record.IntervalDays * multiplier, 1));
            }

            // Update Ease Factor: Good is neutral (0), Hard is (-0.15), Easy is (+0.15), Perfect is (+0.25)
            double efDelta = rating switch
            {
                ReviewRating.Hard => -0.15,
                ReviewRating.Good => 0.0,
                ReviewRating.Easy => 0.15,
                ReviewRating.Perfect => 0.25,
                _ => -0.20
            };
            record.EaseFactor = Math.Max(1.3, Math.Round(record.EaseFactor + efDelta, 2));
            record.Repetitions++;

            // Status transitions
            if (record.Repetitions >= 5 && record.IntervalDays >= 21)
            {
                record.Status = StudyStatus.Mastered;
            }
            else if (record.Repetitions >= 2)
            {
                record.Status = StudyStatus.Reviewing;
            }
            else
            {
                record.Status = StudyStatus.Learning;
            }

            record.NextReviewDate = now.AddDays(record.IntervalDays);
        }

        // Update streak
        UpdateStudyStreak();

        // Save records and profile
        await _storageService.SaveStudyRecordsAsync(_records);
        await _storageService.SaveUserProfileAsync(_profile);

        return record;
    }

    private void UpdateStudyStreak()
    {
        var today = DateTime.UtcNow.Date;
        if (!_profile.LastStudiedDate.HasValue)
        {
            _profile.CurrentStreak = 1;
            _profile.LastStudiedDate = today;
        }
        else
        {
            var lastDate = _profile.LastStudiedDate.Value.Date;
            if (lastDate == today)
            {
                // Already studied today, streak remains
            }
            else if (lastDate == today.AddDays(-1))
            {
                // Consecutive day
                _profile.CurrentStreak++;
                _profile.LastStudiedDate = today;
            }
            else
            {
                // Streak broken
                _profile.CurrentStreak = 1;
                _profile.LastStudiedDate = today;
            }
        }
    }

    private void UpdateStreakStatus()
    {
        if (!_profile.LastStudiedDate.HasValue) return;

        var today = DateTime.UtcNow.Date;
        var lastDate = _profile.LastStudiedDate.Value.Date;
        if (today - lastDate > TimeSpan.FromDays(1))
        {
            _profile.CurrentStreak = 0;
        }
    }

    public IReadOnlyList<KanjiItem> GetDueReviewItems()
    {
        var now = DateTime.UtcNow;
        var allKanji = _kanjiRepo.GetAll();
        var dueKanji = new List<(KanjiItem Item, double OverdueHours)>();

        foreach (var kanji in allKanji)
        {
            if (_records.TryGetValue(kanji.Id, out var rec))
            {
                if (rec.Status != StudyStatus.New && rec.NextReviewDate <= now)
                {
                    dueKanji.Add((kanji, (now - rec.NextReviewDate).TotalHours));
                }
            }
        }

        return dueKanji.OrderByDescending(x => x.OverdueHours)
                       .Select(x => x.Item)
                       .ToList();
    }

    public IReadOnlyList<KanjiItem> GetNewLearningItems(int? count = null)
    {
        int limit = count ?? _profile.DailyNewGoal;
        var allKanji = _kanjiRepo.GetAll();
        var newItems = new List<KanjiItem>();

        foreach (var kanji in allKanji)
        {
            if (!_records.TryGetValue(kanji.Id, out var rec) || rec.Status == StudyStatus.New)
            {
                newItems.Add(kanji);
                if (newItems.Count >= limit) break;
            }
        }

        return newItems;
    }

    public Task<UserProfile> GetUserProfileAsync() => Task.FromResult(_profile);

    public async Task UpdateUserProfileAsync(UserProfile profile)
    {
        _profile = profile;
        await _storageService.SaveUserProfileAsync(_profile);
    }

    public (int newCount, int learningCount, int reviewCount, int masteredCount) GetStudyStats()
    {
        var allKanji = _kanjiRepo.GetAll();
        int newC = 0, learnC = 0, revC = 0, mastC = 0;

        foreach (var kanji in allKanji)
        {
            if (_records.TryGetValue(kanji.Id, out var rec))
            {
                switch (rec.Status)
                {
                    case StudyStatus.Learning: learnC++; break;
                    case StudyStatus.Reviewing: revC++; break;
                    case StudyStatus.Mastered: mastC++; break;
                    default: newC++; break;
                }
            }
            else
            {
                newC++;
            }
        }

        return (newC, learnC, revC, mastC);
    }

    public double GetRetentionRate()
    {
        int totalLogs = 0;
        int passedLogs = 0;

        foreach (var rec in _records.Values)
        {
            foreach (var log in rec.History)
            {
                totalLogs++;
                if (log.Rating >= ReviewRating.Good)
                {
                    passedLogs++;
                }
            }
        }

        if (totalLogs == 0) return 100.0;
        return Math.Round((double)passedLogs / totalLogs * 100.0, 1);
    }

    public int GetCurrentStreak() => _profile.CurrentStreak;

    public Dictionary<DateTime, int> GetUpcomingReviewForecast(int days = 7)
    {
        var forecast = new Dictionary<DateTime, int>();
        var startDate = DateTime.UtcNow.Date;

        for (int i = 0; i < days; i++)
        {
            forecast[startDate.AddDays(i)] = 0;
        }

        foreach (var rec in _records.Values)
        {
            if (rec.Status != StudyStatus.New)
            {
                var dueDate = rec.NextReviewDate.Date;
                if (dueDate < startDate)
                {
                    forecast[startDate]++; // Overdue assigned to today
                }
                else if (forecast.ContainsKey(dueDate))
                {
                    forecast[dueDate]++;
                }
            }
        }

        return forecast;
    }
}
