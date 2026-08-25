using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;

namespace BenkyoKanji.ViewModels;

public enum StudyStatusFilter
{
    All = 0,
    Unstudied = 1,  // 0 times
    Learning = 2,   // 1~2 times
    Mastered = 3    // 3+ times
}

public enum KanjiSortOption
{
    Default = 0,        // JLPT 급수순
    StudyCountAsc = 1,  // 누적 횟수 적은순
    StudyCountDesc = 2, // 누적 횟수 많은순
    StrokeCount = 3,    // 획수순
    KoreanName = 4      // 한국어 훈음순
}

public class KanjiListItemViewModel : ViewModelBase
{
    private readonly Func<string, Task> _incrementAction;
    private readonly Func<string, Task> _decrementAction;
    private StudyRecord _record;

    public KanjiItem Kanji { get; }

    public StudyRecord Record
    {
        get => _record;
        set
        {
            if (SetProperty(ref _record, value))
            {
                OnPropertyChanged(nameof(CumulativeStudyCount));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(StudyCountDisplay));
                OnPropertyChanged(nameof(IsStudied));
            }
        }
    }

    public int CumulativeStudyCount => Record.EffectiveStudyCount;

    public StudyStatus Status => Record.Status;

    public string StatusDisplay => Status switch
    {
        StudyStatus.Mastered => "숙달",
        StudyStatus.Reviewing => "복습 중",
        StudyStatus.Learning => "학습 중",
        _ => "미학습"
    };

    public string StudyCountDisplay => CumulativeStudyCount == 0 ? "미학습 (0회)" : $"누적 {CumulativeStudyCount}회";

    public bool IsStudied => CumulativeStudyCount > 0;

    public IRelayCommand IncrementCountCommand { get; }
    public IRelayCommand DecrementCountCommand { get; }

    public KanjiListItemViewModel(
        KanjiItem kanji, 
        StudyRecord record, 
        Func<string, Task> incrementAction, 
        Func<string, Task> decrementAction)
    {
        Kanji = kanji;
        _record = record;
        _incrementAction = incrementAction;
        _decrementAction = decrementAction;

        IncrementCountCommand = new AsyncRelayCommand(async () => await _incrementAction(Kanji.Id));
        DecrementCountCommand = new AsyncRelayCommand(async () => await _decrementAction(Kanji.Id));
    }

    public void NotifyRecordUpdated()
    {
        OnPropertyChanged(nameof(CumulativeStudyCount));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(StudyCountDisplay));
        OnPropertyChanged(nameof(IsStudied));
    }
}

public class DictionaryViewModel : ViewModelBase
{
    private readonly IKanjiRepository _kanjiRepo;
    private readonly ISrsEngineService _srsService;

    private string _searchQuery = string.Empty;
    private JlptLevel _selectedLevel = JlptLevel.All;
    private StudyStatusFilter _selectedStudyFilter = StudyStatusFilter.All;
    private KanjiSortOption _selectedSortOption = KanjiSortOption.Default;
    private KanjiListItemViewModel? _selectedItem;
    private KanjiItem? _selectedKanji;
    private StudyRecord? _selectedStudyRecord;
    private bool _isAddingCustom;

    // Custom Kanji Form fields
    private string _newKanji = string.Empty;
    private string _newOnyomi = string.Empty;
    private string _newKunyomi = string.Empty;
    private string _newMeaningKo = string.Empty;
    private string _newMeaningEn = string.Empty;
    private JlptLevel _newLevel = JlptLevel.N5;
    private int _newStrokeCount = 1;
    private string _newRadical = string.Empty;
    private string _newExampleWord = string.Empty;
    private string _newExampleReading = string.Empty;
    private string _newExampleMeaning = string.Empty;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                FilterItems();
            }
        }
    }

    public JlptLevel SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                FilterItems();
            }
        }
    }

    public StudyStatusFilter SelectedStudyFilter
    {
        get => _selectedStudyFilter;
        set
        {
            if (SetProperty(ref _selectedStudyFilter, value))
            {
                FilterItems();
            }
        }
    }

    public KanjiSortOption SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetProperty(ref _selectedSortOption, value))
            {
                FilterItems();
            }
        }
    }

    public KanjiListItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                if (value != null)
                {
                    SelectedKanji = value.Kanji;
                    SelectedStudyRecord = value.Record;
                }
                else
                {
                    SelectedKanji = null;
                    SelectedStudyRecord = null;
                }
            }
        }
    }

    public KanjiItem? SelectedKanji
    {
        get => _selectedKanji;
        set
        {
            if (SetProperty(ref _selectedKanji, value))
            {
                if (value != null)
                {
                    SelectedStudyRecord = _srsService.GetOrCreateRecord(value.Id);
                }
                else
                {
                    SelectedStudyRecord = null;
                }
            }
        }
    }

    public StudyRecord? SelectedStudyRecord
    {
        get => _selectedStudyRecord;
        set => SetProperty(ref _selectedStudyRecord, value);
    }

    public bool IsAddingCustom
    {
        get => _isAddingCustom;
        set => SetProperty(ref _isAddingCustom, value);
    }

    public string NewKanji
    {
        get => _newKanji;
        set => SetProperty(ref _newKanji, value);
    }

    public string NewOnyomi
    {
        get => _newOnyomi;
        set => SetProperty(ref _newOnyomi, value);
    }

    public string NewKunyomi
    {
        get => _newKunyomi;
        set => SetProperty(ref _newKunyomi, value);
    }

    public string NewMeaningKo
    {
        get => _newMeaningKo;
        set => SetProperty(ref _newMeaningKo, value);
    }

    public string NewMeaningEn
    {
        get => _newMeaningEn;
        set => SetProperty(ref _newMeaningEn, value);
    }

    public JlptLevel NewLevel
    {
        get => _newLevel;
        set => SetProperty(ref _newLevel, value);
    }

    public int NewStrokeCount
    {
        get => _newStrokeCount;
        set => SetProperty(ref _newStrokeCount, value);
    }

    public string NewRadical
    {
        get => _newRadical;
        set => SetProperty(ref _newRadical, value);
    }

    public string NewExampleWord
    {
        get => _newExampleWord;
        set => SetProperty(ref _newExampleWord, value);
    }

    public string NewExampleReading
    {
        get => _newExampleReading;
        set => SetProperty(ref _newExampleReading, value);
    }

    public string NewExampleMeaning
    {
        get => _newExampleMeaning;
        set => SetProperty(ref _newExampleMeaning, value);
    }

    public ObservableCollection<KanjiListItemViewModel> FilteredItems { get; } = [];

    public IRelayCommand SearchCommand { get; }
    public IRelayCommand ClearSearchCommand { get; }
    public IRelayCommand<JlptLevel> SelectLevelFilterCommand { get; }
    public IRelayCommand<StudyStatusFilter> SelectStudyStatusFilterCommand { get; }
    public IRelayCommand<KanjiListItemViewModel> SelectKanjiCommand { get; }
    public IRelayCommand<string> ManualIncrementStudyCountCommand { get; }
    public IRelayCommand<string> ManualDecrementStudyCountCommand { get; }
    public IRelayCommand ToggleAddCustomCommand { get; }
    public IRelayCommand SaveCustomKanjiCommand { get; }
    public IRelayCommand<string> DeleteKanjiCommand { get; }

    public DictionaryViewModel(IKanjiRepository kanjiRepo, ISrsEngineService srsService)
    {
        _kanjiRepo = kanjiRepo;
        _srsService = srsService;

        SearchCommand = new RelayCommand(FilterItems);
        ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);
        SelectLevelFilterCommand = new RelayCommand<JlptLevel>(level => SelectedLevel = level);
        SelectStudyStatusFilterCommand = new RelayCommand<StudyStatusFilter>(filter => SelectedStudyFilter = filter);
        SelectKanjiCommand = new RelayCommand<KanjiListItemViewModel>(item =>
        {
            if (item != null)
            {
                SelectedItem = item;
            }
        });
        ManualIncrementStudyCountCommand = new AsyncRelayCommand<string>(IncrementStudyCountAsync);
        ManualDecrementStudyCountCommand = new AsyncRelayCommand<string>(DecrementStudyCountAsync);
        ToggleAddCustomCommand = new RelayCommand(() => IsAddingCustom = !IsAddingCustom);
        SaveCustomKanjiCommand = new AsyncRelayCommand(SaveCustomKanjiAsync);
        DeleteKanjiCommand = new AsyncRelayCommand<string>(DeleteKanjiAsync);
    }

    public override async Task InitializeAsync()
    {
        await _kanjiRepo.InitializeAsync();
        await _srsService.InitializeAsync();
        FilterItems();
    }

    public void SelectItem(KanjiItem? item)
    {
        if (item != null)
        {
            var match = FilteredItems.FirstOrDefault(i => i.Kanji.Id == item.Id);
            if (match != null)
            {
                SelectedItem = match;
            }
            else
            {
                SelectedKanji = item;
            }
        }
    }

    private void RunOnUi(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public void FilterItems()
    {
        RunOnUi(() =>
        {
            var prevId = SelectedKanji?.Id ?? SelectedItem?.Kanji.Id;
            var results = _kanjiRepo.Search(SearchQuery, SelectedLevel);

            // Filter by Study Status
            var records = _srsService.GetAllRecords();
            var list = new List<KanjiListItemViewModel>();

            foreach (var kanji in results)
            {
                var rec = records.TryGetValue(kanji.Id, out var existingRec) 
                    ? existingRec 
                    : _srsService.GetOrCreateRecord(kanji.Id);

                int studyCount = rec.EffectiveStudyCount;

                bool matchesStudyFilter = SelectedStudyFilter switch
                {
                    StudyStatusFilter.Unstudied => studyCount == 0,
                    StudyStatusFilter.Learning => studyCount >= 1 && studyCount <= 2,
                    StudyStatusFilter.Mastered => studyCount >= 3,
                    _ => true
                };

                if (matchesStudyFilter)
                {
                    list.Add(new KanjiListItemViewModel(kanji, rec, IncrementStudyCountAsync, DecrementStudyCountAsync));
                }
            }

            // Sorting
            IEnumerable<KanjiListItemViewModel> sorted = SelectedSortOption switch
            {
                KanjiSortOption.StudyCountAsc => list.OrderBy(x => x.CumulativeStudyCount).ThenBy(x => x.Kanji.Level),
                KanjiSortOption.StudyCountDesc => list.OrderByDescending(x => x.CumulativeStudyCount).ThenBy(x => x.Kanji.Level),
                KanjiSortOption.StrokeCount => list.OrderBy(x => x.Kanji.StrokeCount).ThenBy(x => x.Kanji.Level),
                KanjiSortOption.KoreanName => list.OrderBy(x => x.Kanji.MeaningKo),
                _ => list.OrderBy(x => x.Kanji.Level).ThenBy(x => x.Kanji.StrokeCount)
            };

            FilteredItems.Clear();
            foreach (var item in sorted)
            {
                FilteredItems.Add(item);
            }

            if (FilteredItems.Count > 0)
            {
                var match = FilteredItems.FirstOrDefault(k => k.Kanji.Id == prevId);
                SelectedItem = match ?? FilteredItems[0];
            }
            else
            {
                SelectedItem = null;
                SelectedKanji = null;
                SelectedStudyRecord = null;
            }
        });
    }

    public async Task IncrementStudyCountAsync(string? kanjiId)
    {
        if (string.IsNullOrWhiteSpace(kanjiId)) return;

        var updated = await _srsService.IncrementStudyCountAsync(kanjiId);
        var targetItem = FilteredItems.FirstOrDefault(i => i.Kanji.Id == kanjiId);
        if (targetItem != null)
        {
            targetItem.Record = updated;
            targetItem.NotifyRecordUpdated();
            if (SelectedItem?.Kanji.Id == kanjiId)
            {
                SelectedStudyRecord = updated;
            }
            StatusMessage = $"'{targetItem.Kanji.Kanji}' 누적 학습 횟수: {updated.EffectiveStudyCount}회";
        }
    }

    public async Task DecrementStudyCountAsync(string? kanjiId)
    {
        if (string.IsNullOrWhiteSpace(kanjiId)) return;

        var updated = await _srsService.DecrementStudyCountAsync(kanjiId);
        var targetItem = FilteredItems.FirstOrDefault(i => i.Kanji.Id == kanjiId);
        if (targetItem != null)
        {
            targetItem.Record = updated;
            targetItem.NotifyRecordUpdated();
            if (SelectedItem?.Kanji.Id == kanjiId)
            {
                SelectedStudyRecord = updated;
            }
            StatusMessage = $"'{targetItem.Kanji.Kanji}' 학습 횟수가 차감되었습니다. (누적: {updated.EffectiveStudyCount}회)";
        }
    }

    private async Task SaveCustomKanjiAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKanji) || string.IsNullOrWhiteSpace(NewMeaningKo))
        {
            StatusMessage = "한자와 한국어 뜻을 반드시 입력해 주세요.";
            return;
        }

        var customItem = new KanjiItem
        {
            Kanji = NewKanji.Trim(),
            Onyomi = NewOnyomi.Trim(),
            Kunyomi = NewKunyomi.Trim(),
            MeaningKo = NewMeaningKo.Trim(),
            MeaningEn = NewMeaningEn.Trim(),
            Level = NewLevel,
            StrokeCount = Math.Max(1, NewStrokeCount),
            Radical = NewRadical.Trim(),
            IsCustom = true
        };

        if (!string.IsNullOrWhiteSpace(NewExampleWord))
        {
            customItem.Examples.Add(new KanjiExample
            {
                Word = NewExampleWord.Trim(),
                Reading = NewExampleReading.Trim(),
                Meaning = NewExampleMeaning.Trim()
            });
        }

        await _kanjiRepo.AddOrUpdateAsync(customItem);
        IsAddingCustom = false;
        ClearNewForm();

        FilterItems();
        SelectItem(customItem);
        StatusMessage = $"'{customItem.Kanji}' 단어가 사전에 추가되었습니다.";
    }

    private void ClearNewForm()
    {
        NewKanji = string.Empty;
        NewOnyomi = string.Empty;
        NewKunyomi = string.Empty;
        NewMeaningKo = string.Empty;
        NewMeaningEn = string.Empty;
        NewStrokeCount = 1;
        NewRadical = string.Empty;
        NewExampleWord = string.Empty;
        NewExampleReading = string.Empty;
        NewExampleMeaning = string.Empty;
    }

    private async Task DeleteKanjiAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _kanjiRepo.DeleteAsync(id);
        FilterItems();
        SelectedItem = FilteredItems.FirstOrDefault();
        StatusMessage = "한자가 삭제되었습니다.";
    }
}
