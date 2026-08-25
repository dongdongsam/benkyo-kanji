using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using BenkyoKanji.Models;
using BenkyoKanji.Services;
using Microsoft.Win32;

namespace BenkyoKanji.ViewModels;

public class GradingViewModel : ViewModelBase
{
    private readonly IAutoGradingService _gradingService;
    private readonly ISrsEngineService _srsService;
    private readonly IJsonStorageService _storageService;

    private string? _selectedImagePath;
    private BitmapImage? _previewImage;
    private GradingResult? _currentResult;
    private bool _hasGraded;
    private bool _isSynced;
    private string _syncButtonText = "학습 기록에 자동 반영하기 (SRS 동기화)";

    public string? SelectedImagePath
    {
        get => _selectedImagePath;
        set => SetProperty(ref _selectedImagePath, value);
    }

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    public GradingResult? CurrentResult
    {
        get => _currentResult;
        set => SetProperty(ref _currentResult, value);
    }

    public bool HasGraded
    {
        get => _hasGraded;
        set => SetProperty(ref _hasGraded, value);
    }

    public bool IsSynced
    {
        get => _isSynced;
        set => SetProperty(ref _isSynced, value);
    }

    public string SyncButtonText
    {
        get => _syncButtonText;
        set => SetProperty(ref _syncButtonText, value);
    }

    public ObservableCollection<GradingItemResult> GradingItems { get; } = [];
    public ObservableCollection<GradingResult> GradingHistory { get; } = [];

    public IRelayCommand BrowseImageCommand { get; }
    public IRelayCommand PasteClipboardCommand { get; }
    public IRelayCommand RunGradingCommand { get; }
    public IRelayCommand SyncToSrsCommand { get; }
    public IRelayCommand<GradingItemResult> ToggleItemStatusCommand { get; }

    public GradingViewModel(
        IAutoGradingService gradingService, 
        ISrsEngineService srsService, 
        IJsonStorageService storageService)
    {
        _gradingService = gradingService;
        _srsService = srsService;
        _storageService = storageService;

        BrowseImageCommand = new RelayCommand(BrowseImageFile);
        PasteClipboardCommand = new RelayCommand(PasteClipboardImage);
        RunGradingCommand = new AsyncRelayCommand(RunGradingAsync);
        SyncToSrsCommand = new AsyncRelayCommand(SyncToSrsAsync);
        ToggleItemStatusCommand = new RelayCommand<GradingItemResult>(ToggleItemStatus);
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
        await _srsService.InitializeAsync();
        var history = await _storageService.LoadGradingHistoryAsync();
        RunOnUi(() =>
        {
            GradingHistory.Clear();
            foreach (var item in history.Take(10))
            {
                GradingHistory.Add(item);
            }
        });
    }

    public void SetImageFromPath(string path)
    {
        if (File.Exists(path))
        {
            SelectedImagePath = path;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                PreviewImage = bitmap;
            }
            catch
            {
                // Image decode fallback
            }

            HasGraded = false;
            IsSynced = false;
            SyncButtonText = "학습 기록에 자동 반영하기 (SRS 동기화)";
        }
    }

    private void BrowseImageFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "채점할 시험지 사진 선택",
            Filter = "이미지 파일 (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|모든 파일 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SetImageFromPath(dialog.FileName);
        }
    }

    private void PasteClipboardImage()
    {
        if (Clipboard.ContainsImage())
        {
            var image = Clipboard.GetImage();
            if (image != null)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"benkyo_scan_{DateTime.UtcNow.Ticks}.png");
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }

                SetImageFromPath(tempPath);
                StatusMessage = "클립보드 이미지가 불러와졌습니다.";
            }
        }
        else
        {
            StatusMessage = "클립보드에 이미지 데이터가 없습니다.";
        }
    }

    public async Task RunGradingAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedImagePath) || !File.Exists(SelectedImagePath))
        {
            StatusMessage = "채점할 시험지 이미지를 먼저 업로드해 주세요.";
            return;
        }

        IsBusy = true;
        StatusMessage = "OCR 문자 인식 및 자동 채점 분석 중...";
        try
        {
            var result = await _gradingService.GradeWorksheetPhotoAsync(SelectedImagePath);
            CurrentResult = result;

            var history = await _storageService.LoadGradingHistoryAsync();

            RunOnUi(() =>
            {
                GradingItems.Clear();
                foreach (var item in result.Items)
                {
                    GradingItems.Add(item);
                }

                GradingHistory.Clear();
                foreach (var h in history.Take(10))
                {
                    GradingHistory.Add(h);
                }
            });

            HasGraded = true;
            IsSynced = false;
            SyncButtonText = "학습 기록에 자동 반영하기 (SRS 동기화)";
            StatusMessage = $"채점 완료! 총 {result.TotalQuestions}문항 중 {result.CorrectCount}문항 정답 ({result.ScorePercentage:F1}%)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"채점 중 오류가 발생했습니다: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ToggleItemStatus(GradingItemResult? item)
    {
        if (item == null || CurrentResult == null) return;

        item.UserOverridden = true;
        if (item.Status == GradingStatus.Correct)
        {
            item.Status = GradingStatus.Incorrect;
            item.Feedback = "사용자 직접 수정: 오답";
        }
        else
        {
            item.Status = GradingStatus.Correct;
            item.Feedback = "사용자 직접 수정: 정답";
        }

        // Recalculate score
        int correct = GradingItems.Count(i => i.Status == GradingStatus.Correct);
        int partial = GradingItems.Count(i => i.Status == GradingStatus.Partial);
        int incorrect = GradingItems.Count(i => i.Status == GradingStatus.Incorrect || i.Status == GradingStatus.Unanswered);

        CurrentResult.CorrectCount = correct;
        CurrentResult.PartialCount = partial;
        CurrentResult.IncorrectCount = incorrect;
        CurrentResult.ScorePercentage = GradingItems.Count > 0 
            ? Math.Round(((correct + (partial * 0.5)) / GradingItems.Count) * 100.0, 1) 
            : 0;

        OnPropertyChanged(nameof(CurrentResult));
    }

    public async Task SyncToSrsAsync()
    {
        if (CurrentResult == null || IsSynced) return;

        IsBusy = true;
        try
        {
            await _gradingService.SyncGradingToSrsAsync(CurrentResult);
            IsSynced = true;
            SyncButtonText = "✓ 망각 곡선 복습 일정에 동기화 완료됨";
            StatusMessage = "채점 결과가 에빙하우스 망각 곡선 학습 데이터에 성공적으로 반영되었습니다!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"동기화 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
