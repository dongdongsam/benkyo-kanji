using System.Windows.Controls;
using System.Windows.Input;
using BenkyoKanji.Models;
using BenkyoKanji.ViewModels;

namespace BenkyoKanji.Views;

public partial class StudyReviewView : UserControl
{
    public StudyReviewView()
    {
        InitializeComponent();
        Loaded += (s, e) => Focus();
        KeyDown += StudyReviewView_KeyDown;
    }

    private void StudyReviewView_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not StudyReviewViewModel vm) return;

        if (e.Key == Key.Space)
        {
            vm.FlipCardCommand.Execute(null);
            e.Handled = true;
        }
        else if (vm.IsCardFlipped)
        {
            if (e.Key == Key.D1 || e.Key == Key.NumPad1)
            {
                vm.RateCardCommand.Execute(ReviewRating.Again);
                e.Handled = true;
            }
            else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
            {
                vm.RateCardCommand.Execute(ReviewRating.Hard);
                e.Handled = true;
            }
            else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
            {
                vm.RateCardCommand.Execute(ReviewRating.Good);
                e.Handled = true;
            }
            else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
            {
                vm.RateCardCommand.Execute(ReviewRating.Easy);
                e.Handled = true;
            }
        }
    }
}
