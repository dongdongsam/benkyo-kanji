using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;
using Microsoft.Win32;

namespace BenkyoKanji.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISrsEngineService _srsService;
    private readonly IJsonStorageService _storageService;

    private int _dailyNewGoal = 10;
    private int _dailyReviewGoal = 20;
    private double _autoGradingThreshold = 0.75;
    private string? _geminiApiKey;
    private string? _openAiApiKey;
    private string _dataDirectoryPath = string.Empty;
    private string _theme = "Dark";

    public int DailyNewGoal
    {
        get => _dailyNewGoal;
        set => SetProperty(ref _dailyNewGoal, value);
    }

    public int DailyReviewGoal
    {
        get => _dailyReviewGoal;
        set => SetProperty(ref _dailyReviewGoal, value);
    }

    public double AutoGradingThreshold
    {
        get => _autoGradingThreshold;
        set => SetProperty(ref _autoGradingThreshold, value);
    }

    public string? GeminiApiKey
    {
        get => _geminiApiKey;
        set => SetProperty(ref _geminiApiKey, value);
    }

    public string? OpenAiApiKey
    {
        get => _openAiApiKey;
        set => SetProperty(ref _openAiApiKey, value);
    }

    public string DataDirectoryPath
    {
        get => _dataDirectoryPath;
        set => SetProperty(ref _dataDirectoryPath, value);
    }

    public string Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                ThemeManager.ApplyTheme(value);
            }
        }
    }

    public IRelayCommand SaveSettingsCommand { get; }
    public IRelayCommand ExportBackupCommand { get; }
    public IRelayCommand ImportBackupCommand { get; }
    public IRelayCommand<string> SetThemeCommand { get; }

    public SettingsViewModel(ISrsEngineService srsService, IJsonStorageService storageService)
    {
        _srsService = srsService;
        _storageService = storageService;
        DataDirectoryPath = _storageService.DataDirectory;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportBackupCommand = new AsyncRelayCommand(ExportBackupAsync);
        ImportBackupCommand = new AsyncRelayCommand(ImportBackupAsync);
        SetThemeCommand = new RelayCommand<string>(SetTheme);
    }

    public override async Task InitializeAsync()
    {
        var profile = await _srsService.GetUserProfileAsync();
        DailyNewGoal = profile.DailyNewGoal;
        DailyReviewGoal = profile.DailyReviewGoal;
        AutoGradingThreshold = profile.AutoGradingThreshold;
        GeminiApiKey = profile.GeminiApiKey;
        OpenAiApiKey = profile.OpenAiApiKey;
        Theme = profile.Theme;
    }

    private void SetTheme(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme)) return;
        Theme = theme;
    }

    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _srsService.GetUserProfileAsync();
            profile.DailyNewGoal = DailyNewGoal;
            profile.DailyReviewGoal = DailyReviewGoal;
            profile.AutoGradingThreshold = AutoGradingThreshold;
            profile.GeminiApiKey = GeminiApiKey;
            profile.OpenAiApiKey = OpenAiApiKey;
            profile.Theme = Theme;

            await _srsService.UpdateUserProfileAsync(profile);
            StatusMessage = "설정이 성공적으로 저장되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"설정 저장 오류: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportBackupAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "데이터 전체 백업 파일 저장",
            Filter = "JSON 백업 파일 (*.json)|*.json",
            FileName = $"BenkyoKanji_Backup_{DateTime.UtcNow:yyyyMMdd_HHmm}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            await _storageService.ExportDataBackupAsync(dialog.FileName);
            StatusMessage = "데이터가 성공적으로 백업되었습니다.";
        }
    }

    private async Task ImportBackupAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "데이터 백업 파일 선택",
            Filter = "JSON 백업 파일 (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            await _storageService.ImportDataBackupAsync(dialog.FileName);
            await InitializeAsync();
            StatusMessage = "데이터 복원이 완료되었습니다.";
        }
    }
}
