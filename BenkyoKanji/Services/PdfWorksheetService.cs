using System.IO;
using BenkyoKanji.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BenkyoKanji.Services;

public interface IPdfWorksheetService
{
    byte[] GenerateWorksheetPdf(WorksheetConfig config);
    string SaveWorksheetPdf(WorksheetConfig config, string? targetDirectory = null);
    WorksheetConfig CreateWorksheetConfig(
        WorksheetType type, 
        JlptLevel level, 
        int questionCount, 
        IReadOnlyList<KanjiItem> candidates,
        string? title = null);
}

public class PdfWorksheetService : IPdfWorksheetService
{
    private static readonly string PrimaryFont = "Malgun Gothic";
    private static readonly string[] CjkFallbackFonts = ["Yu Gothic", "Meiryo", "MS Gothic", "Yu Gothic UI", "Gulim", "Batang", "SimSun", "Arial Unicode MS", "Segoe UI"];
    private static bool _fontsRegistered;

    static PdfWorksheetService()
    {
        RegisterSystemFonts();
    }

    private static void RegisterSystemFonts()
    {
        if (_fontsRegistered) return;
        _fontsRegistered = true;

        try
        {
            var windowsFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            if (Directory.Exists(windowsFonts))
            {
                string[] fontFiles = ["malgun.ttf", "malgunbd.ttf", "msgothic.ttc", "meiryo.ttc", "meiryob.ttc", "YuGothR.ttc", "YuGothB.ttc", "gulim.ttc", "batang.ttc"];
                foreach (var fontFile in fontFiles)
                {
                    var fullPath = Path.Combine(windowsFonts, fontFile);
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            using var stream = File.OpenRead(fullPath);
                            QuestPDF.Drawing.FontManager.RegisterFont(stream);
                        }
                        catch
                        {
                            // Ignore individual font registration issues
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore system font directory access issues
        }
    }

    public PdfWorksheetService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        RegisterSystemFonts();
    }

    public WorksheetConfig CreateWorksheetConfig(
        WorksheetType type, 
        JlptLevel level, 
        int questionCount, 
        IReadOnlyList<KanjiItem> candidates,
        string? title = null)
    {
        var filtered = level == JlptLevel.All 
            ? candidates.ToList() 
            : candidates.Where(k => k.Level == level).ToList();

        // Shuffle
        var shuffled = filtered.OrderBy(_ => Random.Shared.Next()).Take(questionCount).ToList();

        var config = new WorksheetConfig
        {
            SheetType = type,
            JlptLevel = level,
            QuestionCount = shuffled.Count,
            Title = !string.IsNullOrWhiteSpace(title) ? title : GetDefaultTitle(type, level)
        };

        int idx = 1;
        foreach (var kanji in shuffled)
        {
            var item = new WorksheetItem
            {
                Index = idx++,
                KanjiItem = kanji
            };

            switch (type)
            {
                case WorksheetType.FullStudyTable:
                    item.HideKanji = false;
                    item.HideReading = false;
                    item.HideMeaning = false;
                    item.TargetField = "None";
                    item.ExpectedAnswer = kanji.Kanji;
                    break;

                case WorksheetType.KanjiQuiz:
                    item.HideKanji = true;
                    item.HideReading = false;
                    item.HideMeaning = false;
                    item.TargetField = "Kanji";
                    item.ExpectedAnswer = kanji.Kanji;
                    break;

                case WorksheetType.ReadingQuiz:
                    item.HideKanji = false;
                    item.HideReading = true;
                    item.HideMeaning = false;
                    item.TargetField = "Reading";
                    item.ExpectedAnswer = kanji.AllReadings;
                    break;

                case WorksheetType.MeaningQuiz:
                    item.HideKanji = false;
                    item.HideReading = false;
                    item.HideMeaning = true;
                    item.TargetField = "Meaning";
                    item.ExpectedAnswer = kanji.MeaningKo;
                    break;

                case WorksheetType.MixedQuiz:
                    int pick = Random.Shared.Next(3);
                    if (pick == 0)
                    {
                        item.HideKanji = true;
                        item.TargetField = "Kanji";
                        item.ExpectedAnswer = kanji.Kanji;
                    }
                    else if (pick == 1)
                    {
                        item.HideReading = true;
                        item.TargetField = "Reading";
                        item.ExpectedAnswer = kanji.AllReadings;
                    }
                    else
                    {
                        item.HideMeaning = true;
                        item.TargetField = "Meaning";
                        item.ExpectedAnswer = kanji.MeaningKo;
                    }
                    break;
            }

            config.Items.Add(item);
        }

        return config;
    }

    private static string GetDefaultTitle(WorksheetType type, JlptLevel level)
    {
        string levelStr = level == JlptLevel.All ? "전체 레벨" : $"JLPT {level}";
        return type switch
        {
            WorksheetType.FullStudyTable => $"{levelStr} 일본어 한자·어휘 학습 정리표",
            WorksheetType.KanjiQuiz => $"{levelStr} 일본어 한자 쓰기 테스트",
            WorksheetType.ReadingQuiz => $"{levelStr} 한자 음독·훈독 읽기 테스트",
            WorksheetType.MeaningQuiz => $"{levelStr} 일본어 한자·어휘 뜻 확인 테스트",
            WorksheetType.MixedQuiz => $"{levelStr} 일본어 한자 종합 실전 테스트",
            _ => "일본어 한자 학습지"
        };
    }

    public byte[] GenerateWorksheetPdf(WorksheetConfig config)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(PrimaryFont).FontColor("#1f2937"));

                page.Header().Element(header => ComposeHeader(header, config));
                page.Content().Element(content => ComposeContent(content, config));
                page.Footer().Element(footer => ComposeFooter(footer, config));
            });
        });

        return doc.GeneratePdf();
    }

    public string SaveWorksheetPdf(WorksheetConfig config, string? targetDirectory = null)
    {
        var dir = targetDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BenkyoKanji_Worksheets");
        Directory.CreateDirectory(dir);

        var safeTitle = string.Join("_", config.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var fileName = $"{config.WorksheetId}_{safeTitle}.pdf";
        var fullPath = Path.Combine(dir, fileName);

        var pdfBytes = GenerateWorksheetPdf(config);
        File.WriteAllBytes(fullPath, pdfBytes);
        return fullPath;
    }

    private void ComposeHeader(IContainer container, WorksheetConfig config)
    {
        container.PaddingBottom(10).Column(col =>
        {
            // Top Bar with Sheet ID badge & Date
            col.Item().Row(row =>
            {
                row.RelativeItem().Row(r =>
                {
                    r.AutoItem().Background("#4f46e5").PaddingVertical(3).PaddingHorizontal(8).Text(text =>
                    {
                        text.Span("BENKYO KANJI").FontSize(8).Bold().FontColor(Colors.White);
                    });
                    r.AutoItem().PaddingLeft(6).Text($"ID: {config.WorksheetId}").FontSize(8).FontColor("#6b7280");
                });

                row.AutoItem().Text($"생성일: {config.CreatedAt:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#6b7280");
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(config.Title).FontSize(16).Bold().FontColor("#111827");
                    c.Item().Text(config.Subtitle).FontSize(9).FontColor("#4b5563");
                });

                if (config.ShowHeaderInfo)
                {
                    row.AutoItem().Border(1).BorderColor("#e5e7eb").Padding(5).Width(200).Column(info =>
                    {
                        info.Item().Row(r =>
                        {
                            r.RelativeItem().Text("이름: ______________").FontSize(9);
                            r.RelativeItem().Text("점수: _____ / 100").FontSize(9).Bold();
                        });
                        info.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text($"레벨: {config.JlptLevel}").FontSize(8).FontColor("#6b7280");
                            r.RelativeItem().Text($"문항: {config.Items.Count}문항").FontSize(8).FontColor("#6b7280");
                        });
                    });
                }
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#e5e7eb");
        });
    }

    private void ComposeContent(IContainer container, WorksheetConfig config)
    {
        container.Table(table =>
        {
            // Define Table Columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);   // Index
                columns.ConstantColumn(75);   // Kanji
                columns.ConstantColumn(120);  // Onyomi & Kunyomi
                columns.ConstantColumn(130);  // Korean Meaning
                columns.RelativeColumn();     // Examples & Radicals
            });

            // Table Header Row
            table.Header(header =>
            {
                header.Cell().Background("#1e1d24").Padding(5).AlignCenter().Text("#").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background("#1e1d24").Padding(5).AlignCenter().Text("한자 (漢字)").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background("#1e1d24").Padding(5).AlignCenter().Text("음독 / 훈독 (音·訓)").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background("#1e1d24").Padding(5).AlignCenter().Text("한국어 뜻 / 훈음").FontSize(8).Bold().FontColor(Colors.White);
                header.Cell().Background("#1e1d24").Padding(5).AlignCenter().Text("어휘 예문 및 부수").FontSize(8).Bold().FontColor(Colors.White);
            });

            // Items
            for (int i = 0; i < config.Items.Count; i++)
            {
                var item = config.Items[i];
                var kanji = item.KanjiItem;
                var bg = i % 2 == 0 ? "#ffffff" : "#f9fafb";

                // Col 1: Index
                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().AlignMiddle()
                    .Text($"{item.Index}").FontSize(8).FontColor("#6b7280");

                // Col 2: Kanji
                var kanjiCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignCenter().AlignMiddle();
                if (item.HideKanji)
                {
                    kanjiCell.Width(50).Height(40).Border(1).BorderColor("#d1d5db").AlignCenter().AlignMiddle()
                        .Text("").FontSize(10);
                }
                else
                {
                    kanjiCell.Column(c =>
                    {
                        c.Item().AlignCenter().Text(t =>
                        {
                            var span = t.Span(kanji.Kanji).FontSize(22).Bold().FontColor("#111827");
                            span.FontFamily(PrimaryFont);
                        });
                        if (config.IncludeStrokeCount)
                        {
                            c.Item().AlignCenter().Text($"{kanji.StrokeCount}획 | {kanji.Level}").FontSize(7).FontColor("#9ca3af");
                        }
                    });
                }

                // Col 3: Reading (Onyomi / Kunyomi)
                var readingCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignMiddle();
                if (item.HideReading)
                {
                    readingCell.Height(38).Border(1).BorderColor("#d1d5db").Padding(3)
                        .Text("음: \n훈: ").FontSize(7).FontColor("#9ca3af");
                }
                else
                {
                    readingCell.Column(c =>
                    {
                        if (!string.IsNullOrWhiteSpace(kanji.Onyomi))
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("음: ").FontSize(7.5f).Bold().FontColor("#4f46e5");
                                t.Span(kanji.Onyomi).FontSize(8.5f).FontColor("#1f2937");
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(kanji.Kunyomi))
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("훈: ").FontSize(7.5f).Bold().FontColor("#059669");
                                t.Span(kanji.Kunyomi).FontSize(8.5f).FontColor("#1f2937");
                            });
                        }
                    });
                }

                // Col 4: Meaning (Korean)
                var meaningCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignMiddle();
                if (item.HideMeaning)
                {
                    meaningCell.Height(38).Border(1).BorderColor("#d1d5db").Padding(3)
                        .Text("뜻: ").FontSize(7).FontColor("#9ca3af");
                }
                else
                {
                    meaningCell.Column(c =>
                    {
                        c.Item().Text(kanji.MeaningKo).FontSize(9).Bold().FontColor("#111827");
                        if (!string.IsNullOrWhiteSpace(kanji.MeaningEn))
                        {
                            c.Item().Text(kanji.MeaningEn).FontSize(7.5f).FontColor("#6b7280");
                        }
                    });
                }

                // Col 5: Examples & Info
                var exampleCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(4).AlignMiddle();
                exampleCell.Column(c =>
                {
                    if (kanji.Examples != null && kanji.Examples.Count > 0 && config.IncludeExamples)
                    {
                        foreach (var ex in kanji.Examples.Take(2))
                        {
                            c.Item().Text(t =>
                            {
                                t.Span($"• {ex.Word} ").Bold().FontSize(8).FontColor("#1f2937");
                                t.Span($"[{ex.Reading}] ").FontSize(7.5f).FontColor("#4b5563");
                                t.Span($": {ex.Meaning}").FontSize(7.5f).FontColor("#6b7280");
                            });
                        }
                    }
                    else
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("부수: ").FontSize(7.5f).FontColor("#6b7280");
                            t.Span(string.IsNullOrWhiteSpace(kanji.Radical) ? "-" : kanji.Radical).Bold().FontSize(8);
                        });
                    }
                });
            }
        });
    }

    private void ComposeFooter(IContainer container, WorksheetConfig config)
    {
        container.PaddingTop(10).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Benkyo Kanji (勉強漢字) • 에빙하우스 망각 곡선 맞춤 학습지").FontSize(7.5f).FontColor("#9ca3af");
            });

            row.AutoItem().Text(text =>
            {
                text.Span("Page ").FontSize(7.5f).FontColor("#9ca3af");
                text.CurrentPageNumber().FontSize(7.5f).FontColor("#9ca3af");
                text.Span(" / ").FontSize(7.5f).FontColor("#9ca3af");
                text.TotalPages().FontSize(7.5f).FontColor("#9ca3af");
            });
        });
    }
}
