using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;

namespace BenkyoKanji.ViewModels;

public class StudyReviewViewModel : ViewModelBase
{
    private readonly ISrsEngineService _srsService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly List<KanjiItem> _sessionQueue = [];
    private int _currentIndex = -1;
    private bool _isCardFlipped;
    private bool _isSessionComplete;
    private Stopwatch _cardStopwatch = new();

    private KanjiItem? _currentKanji;
    private StudyRecord? _currentRecord;
    private string _sessionTitle = "일일 복습 세션";
    private int _sessionTotal;
    private int _sessionCompleted;
    private int _againCount;
    private int _hardCount;
    private int _goodCount;
    private int _easyCount;

    public KanjiItem? CurrentKanji
    {
        get => _currentKanji;
        set => SetProperty(ref _currentKanji, value);
    }

    public StudyRecord? CurrentRecord
    {
        get => _currentRecord;
        set => SetProperty(ref _currentRecord, value);
    }

    public bool IsCardFlipped
    {
        get => _isCardFlipped;
        set => SetProperty(ref _isCardFlipped, value);
    }

    public bool IsSessionComplete
    {
        get => _isSessionComplete;
        set => SetProperty(ref _isSessionComplete, value);
    }

    public string SessionTitle
    {
        get => _sessionTitle;
        set => SetProperty(ref _sessionTitle, value);
    }

    public int SessionTotal
    {
        get => _sessionTotal;
        set => SetProperty(ref _sessionTotal, value);
    }

    public int SessionCompleted
    {
        get => _sessionCompleted;
        set => SetProperty(ref _sessionCompleted, value);
    }

    public int AgainCount
    {
        get => _againCount;
        set => SetProperty(ref _againCount, value);
    }

    public int HardCount
    {
        get => _hardCount;
        set => SetProperty(ref _hardCount, value);
    }

    public int GoodCount
    {
        get => _goodCount;
        set => SetProperty(ref _goodCount, value);
    }

    public int EasyCount
    {
        get => _easyCount;
        set => SetProperty(ref _easyCount, value);
    }

    public string ProgressDisplay => SessionTotal > 0 ? $"{SessionCompleted + 1} / {SessionTotal}" : "0 / 0";

    public IRelayCommand FlipCardCommand { get; }
    public IRelayCommand<ReviewRating> RateCardCommand { get; }
    public IRelayCommand StartDueReviewCommand { get; }
    public IRelayCommand StartNewCardsCommand { get; }
    public IRelayCommand RestartSessionCommand { get; }

    public StudyReviewViewModel(ISrsEngineService srsService, IKanjiRepository kanjiRepo)
    {
        _srsService = srsService;
        _kanjiRepo = kanjiRepo;

        FlipCardCommand = new RelayCommand(FlipCard);
        RateCardCommand = new AsyncRelayCommand<ReviewRating>(RateCardAsync);
        StartDueReviewCommand = new AsyncRelayCommand(StartDueReviewAsync);
        StartNewCardsCommand = new AsyncRelayCommand(StartNewCardsAsync);
        RestartSessionCommand = new AsyncRelayCommand(StartDueReviewAsync);
    }

    public override async Task InitializeAsync()
    {
        await StartDueReviewAsync();
    }

    public async Task StartDueReviewAsync()
    {
        IsBusy = true;
        try
        {
            await _srsService.InitializeAsync();
            var due = _srsService.GetDueReviewItems();
            _sessionQueue.Clear();
            _sessionQueue.AddRange(due);

            // If no due items, suggest new items
            if (_sessionQueue.Count == 0)
            {
                var newItems = _srsService.GetNewLearningItems(10);
                _sessionQueue.AddRange(newItems);
                SessionTitle = "새 단어 학습 세션";
            }
            else
            {
                SessionTitle = "에빙하우스 망각 곡선 복습 세션";
            }

            ResetSessionCounters();
            LoadNextCard();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartNewCardsAsync()
    {
        IsBusy = true;
        try
        {
            await _srsService.InitializeAsync();
            var newItems = _srsService.GetNewLearningItems(15);
            _sessionQueue.Clear();
            _sessionQueue.AddRange(newItems);
            SessionTitle = "새로운 한자 학습 세션";

            ResetSessionCounters();
            LoadNextCard();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetSessionCounters()
    {
        _currentIndex = -1;
        SessionTotal = _sessionQueue.Count;
        SessionCompleted = 0;
        AgainCount = 0;
        HardCount = 0;
        GoodCount = 0;
        EasyCount = 0;
        IsSessionComplete = false;
        IsCardFlipped = false;
    }

    private void LoadNextCard()
    {
        _currentIndex++;
        if (_currentIndex < _sessionQueue.Count)
        {
            CurrentKanji = _sessionQueue[_currentIndex];
            CurrentRecord = _srsService.GetOrCreateRecord(CurrentKanji.Id);
            IsCardFlipped = false;
            IsSessionComplete = false;
            OnPropertyChanged(nameof(ProgressDisplay));
            _cardStopwatch.Restart();
        }
        else
        {
            CurrentKanji = null;
            CurrentRecord = null;
            IsSessionComplete = true;
            _cardStopwatch.Stop();
        }
    }

    private void FlipCard()
    {
        if (CurrentKanji != null)
        {
            IsCardFlipped = !IsCardFlipped;
        }
    }

    private async Task RateCardAsync(ReviewRating rating)
    {
        if (CurrentKanji == null) return;

        _cardStopwatch.Stop();
        double elapsed = _cardStopwatch.Elapsed.TotalSeconds;

        switch (rating)
        {
            case ReviewRating.Again: AgainCount++; break;
            case ReviewRating.Hard: HardCount++; break;
            case ReviewRating.Good: GoodCount++; break;
            case ReviewRating.Easy:
            case ReviewRating.Perfect: EasyCount++; break;
        }

        await _srsService.ProcessReviewAsync(CurrentKanji.Id, rating, ReviewSource.DigitalFlashcard, elapsed);
        SessionCompleted++;

        LoadNextCard();
    }
}
