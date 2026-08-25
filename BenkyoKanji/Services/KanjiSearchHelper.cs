using System.Text;
using BenkyoKanji.Models;

namespace BenkyoKanji.Services;

public static class KanjiSearchHelper
{
    private static readonly char[] ChosungList = 
    [
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 
        'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    ];

    public static string DecomposeKoreanChosung(string text, bool removeWhitespace = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int chosungIndex = (c - 0xAC00) / (21 * 28);
                sb.Append(ChosungList[chosungIndex]);
            }
            else if (!removeWhitespace || !char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static bool IsChosungOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        foreach (char c in text.Trim())
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c < 0x3131 || c > 0x314E) // Hangul Compatibility Jamo range (ㄱ ~ ㅎ)
            {
                return false;
            }
        }
        return true;
    }

    public static string ToHiragana(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c >= 0x30A1 && c <= 0x30F6)
            {
                sb.Append((char)(c - 0x60));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static string ToKatakana(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder();
        foreach (char c in text)
        {
            if (c >= 0x3041 && c <= 0x3096)
            {
                sb.Append((char)(c + 0x60));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static bool Matches(KanjiItem item, string? query, JlptLevel level = JlptLevel.All, string? tag = null)
    {
        if (level != JlptLevel.All && item.Level != level)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            if (!item.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var q = query.Trim().ToLowerInvariant();

        // 1. Exact / Substring Kanji match
        if (item.Kanji.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 2. Korean meaning match
        if (item.MeaningKo.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 3. Korean Chosung search (e.g. "ㅂㅇㅎ" -> "배울 학", "ㄴㅇ" -> "날 일", "ㅁㅅ" -> "물 수")
        var chosungMeaning = DecomposeKoreanChosung(item.MeaningKo).ToLowerInvariant();
        var chosungClean = chosungMeaning.Replace(" ", "");
        var queryClean = q.Replace(" ", "");

        if (chosungMeaning.Contains(q, StringComparison.OrdinalIgnoreCase) || 
            chosungClean.Contains(queryClean, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Also check chosung for example meanings
        if (IsChosungOnly(queryClean) && item.Examples.Any(e => 
            DecomposeKoreanChosung(e.Meaning).Replace(" ", "").Contains(queryClean, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 4. Japanese readings (Onyomi, Kunyomi) with Hiragana <-> Katakana cross-search
        var qHira = ToHiragana(q);
        var qKata = ToKatakana(q);

        var onyomiHira = ToHiragana(item.Onyomi);
        var onyomiKata = ToKatakana(item.Onyomi);
        var kunyomiHira = ToHiragana(item.Kunyomi);
        var kunyomiKata = ToKatakana(item.Kunyomi);

        if (item.Onyomi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            item.Kunyomi.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            onyomiHira.Contains(qHira, StringComparison.OrdinalIgnoreCase) ||
            onyomiKata.Contains(qKata, StringComparison.OrdinalIgnoreCase) ||
            kunyomiHira.Contains(qHira, StringComparison.OrdinalIgnoreCase) ||
            kunyomiKata.Contains(qKata, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 5. English meaning match
        if (!string.IsNullOrWhiteSpace(item.MeaningEn) && item.MeaningEn.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 6. Radical and Stroke count match (e.g. "4획" or "부수: 日")
        if (!string.IsNullOrWhiteSpace(item.Radical) && item.Radical.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (q.EndsWith("획") && int.TryParse(q.Replace("획", "").Trim(), out int strokeSearch) && item.StrokeCount == strokeSearch)
        {
            return true;
        }

        // 7. Examples match (Word, Reading, Meaning)
        if (item.Examples.Any(e =>
            e.Word.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            e.Reading.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            ToHiragana(e.Reading).Contains(qHira, StringComparison.OrdinalIgnoreCase) ||
            ToKatakana(e.Reading).Contains(qKata, StringComparison.OrdinalIgnoreCase) ||
            e.Meaning.Contains(q, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }
}
