using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Services;

namespace BenkyoKanji.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IJsonStorageService _storageService;
    private readonly IKanjiRepository _kanjiRepo;
    private readonly ISrsEngineService _srsService;
    private readonly IPdfWorksheetService _pdfService;
    private readonly IAutoGradingService _gradingService;

    private ViewModelBase? _currentViewModel;
    private string _activeSection = "Dashboard";
    private string _currentTheme = "Dark";

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string ActiveSection
    {
        get => _activeSection;
        set => SetProperty(ref _activeSection, value);
    }

    public string CurrentTheme
    {
        get => _currentTheme;
        set => SetProperty(ref _currentTheme, value);
    }

    public DashboardViewModel DashboardVM { get; }
    public StudyReviewViewModel StudyReviewVM { get; }
    public WorksheetViewModel WorksheetVM { get; }
    public GradingViewModel GradingVM { get; }
    public DictionaryViewModel DictionaryVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public IRelayCommand<string> NavigateCommand { get; }
    public IRelayCommand ToggleThemeCommand { get; }

    public MainViewModel()
    {
        _storageService = new JsonStorageService();
        _kanjiRepo = new KanjiRepository(_storageService);
        _srsService = new SrsEngineService(_storageService, _kanjiRepo);
        _pdfService = new PdfWorksheetService();
        _gradingService = new AutoGradingService(_storageService, _srsService, _kanjiRepo);

        DashboardVM = new DashboardViewModel(_srsService, _kanjiRepo, Navigate);
        StudyReviewVM = new StudyReviewViewModel(_srsService, _kanjiRepo);
        WorksheetVM = new WorksheetViewModel(_pdfService, _kanjiRepo, _srsService, _storageService);
        GradingVM = new GradingViewModel(_gradingService, _srsService, _storageService);
        DictionaryVM = new DictionaryViewModel(_kanjiRepo, _srsService);
        SettingsVM = new SettingsViewModel(_srsService, _storageService);

        NavigateCommand = new RelayCommand<string>(Navigate);
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);

        // Start on Dashboard
        Navigate("Dashboard");
    }

    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _kanjiRepo.InitializeAsync();
            await _srsService.InitializeAsync();

            var profile = await _srsService.GetUserProfileAsync();
            CurrentTheme = profile.Theme;
            ThemeManager.ApplyTheme(CurrentTheme);

            await DashboardVM.InitializeAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        ThemeManager.ToggleTheme();
        CurrentTheme = ThemeManager.CurrentTheme;
        
        var profile = await _srsService.GetUserProfileAsync();
        profile.Theme = CurrentTheme;
        await _srsService.UpdateUserProfileAsync(profile);

        StatusMessage = CurrentTheme == "Dark" ? "다크 모드가 적용되었습니다." : "라이트 모드가 적용되었습니다.";
    }

    public void Navigate(string? section)
    {
        if (string.IsNullOrWhiteSpace(section)) return;

        ActiveSection = section;
        switch (section)
        {
            case "Dashboard":
                CurrentViewModel = DashboardVM;
                _ = DashboardVM.InitializeAsync();
                break;
            case "Study":
                CurrentViewModel = StudyReviewVM;
                _ = StudyReviewVM.InitializeAsync();
                break;
            case "Worksheet":
                CurrentViewModel = WorksheetVM;
                _ = WorksheetVM.InitializeAsync();
                break;
            case "Grading":
                CurrentViewModel = GradingVM;
                _ = GradingVM.InitializeAsync();
                break;
            case "Dictionary":
                CurrentViewModel = DictionaryVM;
                _ = DictionaryVM.InitializeAsync();
                break;
            case "Settings":
                CurrentViewModel = SettingsVM;
                _ = SettingsVM.InitializeAsync();
                break;
        }
    }
}
