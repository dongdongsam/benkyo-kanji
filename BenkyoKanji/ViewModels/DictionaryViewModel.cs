using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;

namespace BenkyoKanji.ViewModels;

public class DictionaryViewModel : ViewModelBase
{
    private readonly IKanjiRepository _kanjiRepo;
    private readonly ISrsEngineService _srsService;

    private string _searchQuery = string.Empty;
    private JlptLevel _selectedLevel = JlptLevel.All;
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
            else if (value != null && (SelectedStudyRecord == null || SelectedStudyRecord.KanjiId != value.Id))
            {
                SelectedStudyRecord = _srsService.GetOrCreateRecord(value.Id);
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

    public ObservableCollection<KanjiItem> FilteredItems { get; } = [];

    public IRelayCommand SearchCommand { get; }
    public IRelayCommand<JlptLevel> SelectLevelFilterCommand { get; }
    public IRelayCommand<KanjiItem> SelectKanjiCommand { get; }
    public IRelayCommand ToggleAddCustomCommand { get; }
    public IRelayCommand SaveCustomKanjiCommand { get; }
    public IRelayCommand<string> DeleteKanjiCommand { get; }

    public DictionaryViewModel(IKanjiRepository kanjiRepo, ISrsEngineService srsService)
    {
        _kanjiRepo = kanjiRepo;
        _srsService = srsService;

        SearchCommand = new RelayCommand(FilterItems);
        SelectLevelFilterCommand = new RelayCommand<JlptLevel>(level => SelectedLevel = level);
        SelectKanjiCommand = new RelayCommand<KanjiItem>(item =>
        {
            if (item != null)
            {
                SelectedKanji = item;
            }
        });
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
            SelectedKanji = item;
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

    private void FilterItems()
    {
        RunOnUi(() =>
        {
            var prevId = SelectedKanji?.Id;
            var results = _kanjiRepo.Search(SearchQuery, SelectedLevel);
            FilteredItems.Clear();
            foreach (var item in results)
            {
                FilteredItems.Add(item);
            }

            if (FilteredItems.Count > 0)
            {
                var match = FilteredItems.FirstOrDefault(k => k.Id == prevId);
                SelectedKanji = match ?? FilteredItems[0];
            }
            else
            {
                SelectedKanji = null;
            }
        });
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
        SelectedKanji = customItem;
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
        SelectedKanji = FilteredItems.FirstOrDefault();
        StatusMessage = "한자가 삭제되었습니다.";
    }
}
