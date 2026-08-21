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

    public WorksheetConfig? CurrentConfig { get; private set; }
    public ObservableCollection<WorksheetItem> PreviewItems { get; } = [];
    public ObservableCollection<WorksheetConfig> WorksheetHistory { get; } = [];

    public IRelayCommand RefreshPreviewCommand { get; }
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

        RefreshPreviewCommand = new RelayCommand(GeneratePreview);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        OpenPdfCommand = new RelayCommand(OpenGeneratedPdf);

        QuickPresetStudyTableCommand = new RelayCommand(() => SelectedType = WorksheetType.FullStudyTable);
        QuickPresetKanjiQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.KanjiQuiz);
        QuickPresetReadingQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.ReadingQuiz);
        QuickPresetMeaningQuizCommand = new RelayCommand(() => SelectedType = WorksheetType.MeaningQuiz);
    }

    public override async Task InitializeAsync()
    {
        await _kanjiRepo.InitializeAsync();
        await _srsService.InitializeAsync();
        var history = await _storageService.LoadWorksheetHistoryAsync();
        WorksheetHistory.Clear();
        foreach (var ws in history.Take(10))
        {
            WorksheetHistory.Add(ws);
        }

        GeneratePreview();
    }

    public void GeneratePreview()
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

        CurrentConfig = _pdfService.CreateWorksheetConfig(
            SelectedType, 
            SelectedLevel, 
            QuestionCount, 
            candidates, 
            SheetTitle);

        CurrentConfig.IncludeExamples = IncludeExamples;
        CurrentConfig.IncludeStrokeCount = IncludeStrokeCount;

        PreviewItems.Clear();
        foreach (var item in CurrentConfig.Items)
        {
            PreviewItems.Add(item);
        }
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
