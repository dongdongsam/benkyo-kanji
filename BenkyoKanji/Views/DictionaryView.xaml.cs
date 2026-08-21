using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BenkyoKanji.Models;
using BenkyoKanji.ViewModels;

namespace BenkyoKanji.Views;

public partial class DictionaryView : UserControl
{
    public DictionaryView()
    {
        InitializeComponent();
    }

    private void KanjiListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KanjiListBox.SelectedItem is KanjiItem item && DataContext is DictionaryViewModel vm)
        {
            vm.SelectedKanji = item;
        }
    }

    private void KanjiItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is KanjiItem item && DataContext is DictionaryViewModel vm)
        {
            vm.SelectedKanji = item;
            KanjiListBox.SelectedItem = item;
        }
    }
}
