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
        bool b;
        if (value is bool flag)
        {
            b = flag;
        }
        else if (value is string s)
        {
            b = !string.IsNullOrWhiteSpace(s);
        }
        else
        {
            b = value != null;
        }

        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVis = value is Visibility v && v == Visibility.Visible;
        return Invert ? !isVis : isVis;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasValue = value != null;
        if (value is string s)
        {
            hasValue = !string.IsNullOrWhiteSpace(s);
        }

        if (Invert) hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null) return true;
        if (value == null || parameter == null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
        {
            if (targetType.IsEnum && Enum.TryParse(targetType, parameter.ToString(), true, out var enumVal))
            {
                return enumVal;
            }
            return parameter;
        }
        return Binding.DoNothing;
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
