using System.Windows;
using BenkyoKanji.ViewModels;

namespace BenkyoKanji;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var mainVm = new MainViewModel();
        DataContext = mainVm;
        Loaded += async (s, e) => await mainVm.InitializeAsync();
    }
}