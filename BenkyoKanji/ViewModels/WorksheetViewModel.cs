using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;
using Microsoft.Win32;

namespace BenkyoKanji.ViewModels;

public class WorksheetViewModel : ViewModelBase
{
    private readonly IPdfWorksheetService _pdfService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly ISrsEngineService _srsService;
    private readonly IJsonStorageService _storageService;

    private WorksheetType _selectedType = WorksheetType.KanjiQuiz;
    private JlptLevel _selectedLevel = JlptLevel.All;
    private int _questionCount = 20;
    private bool _includeExamples = true;
    private bool _includeStrokeCount = true;
    private bool _onlyDueItems = false;
    private StudyCountFilterType _selectedStudyFilter = StudyCountFilterType.All;
    private int _studyFilterThreshold = 3;
    private int _matchingCandidatesCount = 0;
    private string _sheetTitle = "JLPT 일본어 한자 학습 시험지";
    private string? _lastGeneratedPdfPath;

    public WorksheetType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                GeneratePreview();
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
                GeneratePreview();
            }
        }
    }

    public int QuestionCount
    {
        get => _questionCount;
        set
        {
            if (SetProperty(ref _questionCount, value))
            {
                GeneratePreview();
            }
        }
    }

    public bool IncludeExamples
    {
        get => _includeExamples;
        set => SetProperty(ref _includeExamples, value);
    }

    public bool IncludeStrokeCount
    {
        get => _includeStrokeCount;
        set => SetProperty(ref _includeStrokeCount, value);
    }

    public bool OnlyDueItems
    {
        get => _onlyDueItems;
        set
        {
            if (SetProperty(ref _onlyDueItems, value))
            {
                GeneratePreview();
            }
        }
    }

    public StudyCountFilterType SelectedStudyFilter
    {
        get => _selectedStudyFilter;
        set
        {
            if (SetProperty(ref _selectedStudyFilter, value))
            {
                GeneratePreview();
            }
        }
    }

    public int StudyFilterThreshold
    {
        get => _studyFilterThreshold;
        set
        {
            if (SetProperty(ref _studyFilterThreshold, value))
            {
                GeneratePreview();
            }
        }
    }

    public int MatchingCandidatesCount
    {
        get => _matchingCandidatesCount;
        set => SetProperty(ref _matchingCandidatesCount, value);
    }

    public string SheetTitle
    {
        get => _sheetTitle;
        set => SetProperty(ref _sheetTitle, value);
    }

    public string? LastGeneratedPdfPath
    {
        get => _lastGeneratedPdfPath;
        set => SetProperty(ref _lastGeneratedPdfPath, value);
    }

    private readonly HashSet<string> _excludedKanjiIds = [];

    public int ExcludedCount => _excludedKanjiIds.Count;

    public WorksheetConfig? CurrentConfig { get; private set; }
    public ObservableCollection<WorksheetItem> PreviewItems { get; } = [];
    public ObservableCollection<WorksheetConfig> WorksheetHistory { get; } = [];

    public IRelayCommand RefreshPreviewCommand { get; }
    public IRelayCommand MarkCurrentWorksheetAsStudiedCommand { get; }
    public IRelayCommand GenerateNextSetCommand { get; }
    public IRelayCommand ResetExcludedHistoryCommand { get; }
    public IRelayCommand ExportPdfCommand { get; }
    public IRelayCommand OpenPdfCommand { get; }
    public IRelayCommand QuickPresetStudyTableCommand { get; }
    public IRelayCommand QuickPresetKanjiQuizCommand { get; }
    public IRelayCommand QuickPresetReadingQuizCommand { get; }
    public IRelayCommand QuickPresetMeaningQuizCommand { get; }

    public WorksheetViewModel(
        IPdfWorksheetService pdfService, 
        IKanjiRepository kanjiRepo, 
        ISrsEngineService srsService, 
        IJsonStorageService storageService)
    {
        _pdfService = pdfService;
        _kanjiRepo = kanjiRepo;
        _srsService = srsService;
        _storageService = storageService;

        RefreshPreviewCommand = new RelayCommand(() => GeneratePreview(useExclusion: false));
        MarkCurrentWorksheetAsStudiedCommand = new AsyncRelayCommand(MarkCurrentWorksheetAsStudiedAsync);
        GenerateNextSetCommand = new RelayCommand(GenerateNextSet);
        ResetExcludedHistoryCommand = new RelayCommand(ResetExcludedHistory);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        OpenPdfCommand = new RelayCommand(OpenGeneratedPdf);

        QuickPresetStudyTableCommand = new RelayCommand(() => SelectedType = WorksheetType.FullStudyTable);
        QuickPresetKanjiQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.KanjiQuiz);
        QuickPresetReadingQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.ReadingQuiz);
        QuickPresetMeaningQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.MeaningQuiz);
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

    public override async Task InitializeAsync()
    {
        await _kanjiRepo.InitializeAsync();
        await _srsService.InitializeAsync();
        var history = await _storageService.LoadWorksheetHistoryAsync();
        RunOnUi(() =>
        {
            WorksheetHistory.Clear();
            foreach (var ws in history.Take(10))
            {
                WorksheetHistory.Add(ws);
            }
        });

        GeneratePreview(useExclusion: false);
    }

    public void GenerateNextSet()
    {
        if (CurrentConfig != null && CurrentConfig.Items.Count > 0)
        {
            foreach (var item in CurrentConfig.Items)
            {
                _excludedKanjiIds.Add(item.KanjiItem.Id);
            }
        }

        OnPropertyChanged(nameof(ExcludedCount));
        GeneratePreview(useExclusion: true);
    }

    public void ResetExcludedHistory()
    {
        _excludedKanjiIds.Clear();
        OnPropertyChanged(nameof(ExcludedCount));
        GeneratePreview(useExclusion: false);
        StatusMessage = "출제 제외 기록이 초기화되었습니다.";
    }

    public async Task MarkCurrentWorksheetAsStudiedAsync()
    {
        if (CurrentConfig == null || CurrentConfig.Items.Count == 0) return;

        IsBusy = true;
        try
        {
            int count = 0;
            foreach (var item in CurrentConfig.Items)
            {
                if (!string.IsNullOrWhiteSpace(item.KanjiItem.Id))
                {
                    await _srsService.IncrementStudyCountAsync(item.KanjiItem.Id);
                    count++;
                }
            }

            StatusMessage = $"✓ 현재 시험지에 출제된 {count}개 한자가 누적 학습에 1회씩 반영되었습니다!";
            GeneratePreview(useExclusion: false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"누적 반영 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void GeneratePreview(bool useExclusion = false)
    {
        IReadOnlyList<KanjiItem> candidates;
        if (OnlyDueItems)
        {
            var due = _srsService.GetDueReviewItems();
            candidates = due.Count > 0 ? due : _kanjiRepo.GetAll();
        }
        else
        {
            candidates = _kanjiRepo.GetAll();
        }

        var allRecords = _srsService.GetAllRecords();

        // Calculate matching candidates count
        var filteredCandidates = SelectedLevel == JlptLevel.All
            ? candidates.ToList()
            : candidates.Where(k => k.Level == SelectedLevel).ToList();

        if (SelectedStudyFilter != StudyCountFilterType.All)
        {
            filteredCandidates = filteredCandidates.Where(k =>
            {
                int count = allRecords.TryGetValue(k.Id, out var rec) ? rec.EffectiveStudyCount : 0;
                return SelectedStudyFilter switch
                {
                    StudyCountFilterType.UnstudiedOnly => count == 0,
                    StudyCountFilterType.LessThan => count < StudyFilterThreshold,
                    StudyCountFilterType.AtLeast => count >= StudyFilterThreshold,
                    _ => true
                };
            }).ToList();
        }

        // If next set is requested, exclude previously picked words
        if (useExclusion && _excludedKanjiIds.Count > 0)
        {
            var freshCandidates = filteredCandidates.Where(k => !_excludedKanjiIds.Contains(k.Id)).ToList();
            if (freshCandidates.Count > 0)
            {
                filteredCandidates = freshCandidates;
                StatusMessage = $"이전 시험지 출제 단어({_excludedKanjiIds.Count}개)를 제외하고 새로운 단어로 출제했습니다.";
            }
            else
            {
                // All candidates exhausted, reset exclusions
                _excludedKanjiIds.Clear();
                OnPropertyChanged(nameof(ExcludedCount));
                StatusMessage = "모든 일치 한자가 한 번씩 출제되어 다음 순환 출제를 시작합니다.";
            }
        }

        MatchingCandidatesCount = filteredCandidates.Count;

        CurrentConfig = _pdfService.CreateWorksheetConfig(
            SelectedType, 
            SelectedLevel, 
            QuestionCount, 
            filteredCandidates, 
            SheetTitle,
            allRecords,
            SelectedStudyFilter,
            StudyFilterThreshold);

        CurrentConfig.IncludeExamples = IncludeExamples;
        CurrentConfig.IncludeStrokeCount = IncludeStrokeCount;

        RunOnUi(() =>
        {
            PreviewItems.Clear();
            foreach (var item in CurrentConfig.Items)
            {
                PreviewItems.Add(item);
            }
        });
    }

    public async Task ExportPdfAsync()
    {
        if (CurrentConfig == null || CurrentConfig.Items.Count == 0)
        {
            GeneratePreview();
        }

        if (CurrentConfig == null) return;

        IsBusy = true;
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "PDF 시험지 저장",
                Filter = "PDF 파일 (*.pdf)|*.pdf",
                FileName = $"{CurrentConfig.WorksheetId}_{CurrentConfig.SheetTypeName}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var pdfBytes = _pdfService.GenerateWorksheetPdf(CurrentConfig);
                await File.WriteAllBytesAsync(saveDialog.FileName, pdfBytes);
                LastGeneratedPdfPath = saveDialog.FileName;

                // Save to history
                var history = await _storageService.LoadWorksheetHistoryAsync();
                history.Insert(0, CurrentConfig);
                await _storageService.SaveWorksheetHistoryAsync(history);

                WorksheetHistory.Insert(0, CurrentConfig);

                StatusMessage = $"PDF가 성공적으로 저장되었습니다: {Path.GetFileName(saveDialog.FileName)}";

                // Automatically open PDF
                OpenGeneratedPdf();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF 생성 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenGeneratedPdf()
    {
        if (!string.IsNullOrWhiteSpace(LastGeneratedPdfPath) && File.Exists(LastGeneratedPdfPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LastGeneratedPdfPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback
            }
        }
    }
}
