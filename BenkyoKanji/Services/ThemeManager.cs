using System;
using System.Linq;
using System.Windows;

namespace BenkyoKanji.Services;

public static class ThemeManager
{
    private const string DarkThemeSource = "Styles/DarkTheme.xaml";
    private const string LightThemeSource = "Styles/LightTheme.xaml";

    public static string CurrentTheme { get; private set; } = "Dark";

    public static void ApplyTheme(string themeName)
    {
        var app = Application.Current;
        if (app == null) return;

        var source = themeName.Equals("Light", StringComparison.OrdinalIgnoreCase) 
            ? LightThemeSource 
            : DarkThemeSource;

        var newDict = new ResourceDictionary
        {
            Source = new Uri(source, UriKind.RelativeOrAbsolute)
        };

        // Find existing theme dictionary if present and replace it
        var merged = app.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => 
            d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

        if (existing != null)
        {
            int index = merged.IndexOf(existing);
            merged[index] = newDict;
        }
        else
        {
            merged.Insert(0, newDict);
        }

        CurrentTheme = themeName.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
    }

    public static void ToggleTheme()
    {
        ApplyTheme(CurrentTheme == "Dark" ? "Light" : "Dark");
    }
}
