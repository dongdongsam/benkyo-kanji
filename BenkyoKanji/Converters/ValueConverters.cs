using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BenkyoKanji.Models;

namespace BenkyoKanji.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool flag && flag;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVis = value is Visibility v && v == Visibility.Visible;
        return Invert ? !isVis : isVis;
    }
}

public class JlptLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is JlptLevel level)
        {
            return level switch
            {
                JlptLevel.N5 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), // Emerald
                JlptLevel.N4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0ea5e9")), // Sky
                JlptLevel.N3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366f1")), // Indigo
                JlptLevel.N2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a855f7")), // Purple
                JlptLevel.N1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e")), // Rose
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class GradingStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GradingStatus status)
        {
            return status switch
            {
                GradingStatus.Correct => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                GradingStatus.Partial => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                GradingStatus.Incorrect => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                GradingStatus.Unanswered => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StudyStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is StudyStatus status)
        {
            return status switch
            {
                StudyStatus.New => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b82f6")),
                StudyStatus.Learning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                StudyStatus.Reviewing => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
                StudyStatus.Mastered => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool empty = string.IsNullOrWhiteSpace(value as string);
        if (Invert) empty = !empty;
        return empty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
