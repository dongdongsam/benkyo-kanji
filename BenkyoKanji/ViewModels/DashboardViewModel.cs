using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;

namespace BenkyoKanji.ViewModels;

public class ForecastDayItem
{
    public string DayName { get; set; } = string.Empty;
    public string DateStr { get; set; } = string.Empty;
    public int DueCount { get; set; }
    public bool IsToday { get; set; }
}

public class DashboardViewModel : ViewModelBase
{
    private readonly ISrsEngineService _srsService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly Action<string> _navigateAction;

    private int _dueReviewCount;
    private int _newLearningCount;
    private int _streakDays;
    private double _retentionRate;
    private int _totalCards;
    private int _masteredCount;
    private int _learningCount;
    private int _reviewingCount;
    private int _newCount;

    public int DueReviewCount
    {
        get => _dueReviewCount;
        set => SetProperty(ref _dueReviewCount, value);
    }

    public int NewLearningCount
    {
        get => _newLearningCount;
        set => SetProperty(ref _newLearningCount, value);
    }

    public int StreakDays
    {
        get => _streakDays;
        set => SetProperty(ref _streakDays, value);
    }

    public double RetentionRate
    {
        get => _retentionRate;
        set => SetProperty(ref _retentionRate, value);
    }

    public int TotalCards
    {
        get => _totalCards;
        set => SetProperty(ref _totalCards, value);
    }

    public int MasteredCount
    {
        get => _masteredCount;
        set => SetProperty(ref _masteredCount, value);
    }

    public int LearningCount
    {
        get => _learningCount;
        set => SetProperty(ref _learningCount, value);
    }

    public int ReviewingCount
    {
        get => _reviewingCount;
        set => SetProperty(ref _reviewingCount, value);
    }

    public int NewCount
    {
        get => _newCount;
        set => SetProperty(ref _newCount, value);
    }

    public ObservableCollection<ForecastDayItem> ForecastDays { get; } = [];
    public ObservableCollection<KanjiItem> QuickReviewPreview { get; } = [];

    public IRelayCommand StartReviewCommand { get; }
    public IRelayCommand GenerateWorksheetCommand { get; }
    public IRelayCommand OpenGradingCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public DashboardViewModel(
        ISrsEngineService srsService, 
        IKanjiRepository kanjiRepo, 
        Action<string> navigateAction)
    {
        _srsService = srsService;
        _kanjiRepo = kanjiRepo;
        _navigateAction = navigateAction;

        StartReviewCommand = new RelayCommand(() => _navigateAction("Study"));
        GenerateWorksheetCommand = new RelayCommand(() => _navigateAction("Worksheet"));
        OpenGradingCommand = new RelayCommand(() => _navigateAction("Grading"));
        RefreshCommand = new AsyncRelayCommand(RefreshStatsAsync);
    }

    public override async Task InitializeAsync()
    {
        await RefreshStatsAsync();
    }

    public async Task RefreshStatsAsync()
    {
        IsBusy = true;
        try
        {
            await _srsService.InitializeAsync();
            var (newC, learnC, revC, mastC) = _srsService.GetStudyStats();

            NewCount = newC;
            LearningCount = learnC;
            ReviewingCount = revC;
            MasteredCount = mastC;
            TotalCards = newC + learnC + revC + mastC;

            var dueItems = _srsService.GetDueReviewItems();
            DueReviewCount = dueItems.Count;

            var newItems = _srsService.GetNewLearningItems();
            NewLearningCount = newItems.Count;

            StreakDays = _srsService.GetCurrentStreak();
            RetentionRate = _srsService.GetRetentionRate();

            // Preview due cards
            QuickReviewPreview.Clear();
            foreach (var item in dueItems.Take(5))
            {
                QuickReviewPreview.Add(item);
            }

            // 7-day forecast
            var forecast = _srsService.GetUpcomingReviewForecast(7);
            ForecastDays.Clear();
            var today = DateTime.UtcNow.Date;

            foreach (var kvp in forecast)
            {
                var isToday = kvp.Key == today;
                ForecastDays.Add(new ForecastDayItem
                {
                    DayName = isToday ? "오늘" : kvp.Key.ToString("ddd", System.Globalization.CultureInfo.GetCultureInfo("ko-KR")),
                    DateStr = kvp.Key.ToString("MM/dd"),
                    DueCount = kvp.Value,
                    IsToday = isToday
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
