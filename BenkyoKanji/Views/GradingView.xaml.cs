using System.IO;
using System.Windows;
using System.Windows.Controls;
using BenkyoKanji.ViewModels;

namespace BenkyoKanji.Views;

public partial class GradingView : UserControl
{
    public GradingView()
    {
        InitializeComponent();
    }

    private void ImageDropBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ImageDropBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var file = files[0];
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp")
                {
                    if (DataContext is GradingViewModel vm)
                    {
                        vm.SetImageFromPath(file);
                    }
                }
            }
        }
    }
}
