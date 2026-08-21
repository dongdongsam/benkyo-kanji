# Benkyo Kanji (勉強漢字) - Development Progress & Context Log

## Project Summary
A Tailscale-aesthetic WPF desktop application for learning Japanese Kanji and Vocabulary across JLPT N5~N1, equipped with Ebbinghaus Spaced Repetition scheduling, custom printable PDF study/quiz worksheets, and automated photo worksheet grading via OCR.

---

## Architecture Context & System Decisions
- **Target Framework**: .NET 8 WPF (`net8.0-windows10.0.19041.0`) with C# 12.
- **PDF Engine**: QuestPDF (Clean A4 multi-column layout, Japanese/Korean typography support).
- **OCR & Grading**: Windows.Media.Ocr Native WinRT OCR + Intelligent Fuzzy Matching + Optional Vision fallback.
- **Data Persistence**: JSON-based storage for Kanji library, user profile, SRS study logs, and worksheet history.
- **UI Design System**: Tailscale modern dark/light aesthetic (Charcoal `#131217`, Dark Slate `#1e1d24`, Tailscale Indigo `#6366f1`, rounded cards, status badges, responsive navigation).

---

## Log of Changes & Commits

### [Step 1] Initial Setup & Solution Configuration
- **Date**: 2026-08-21
- **Commit Message**: `ADDED: Initialize BenkyoKanji WPF solution and test projects`
- **Context**: Configured `BenkyoKanji.sln`, `BenkyoKanji` (.NET 8 WPF with Windows SDK support), and `BenkyoKanji.Tests`. Added QuestPDF and CommunityToolkit.Mvvm dependencies. Set up `.gitignore`.

### [Step 2] Complete Implementation of Core Models, Data Layer, and Services
- **Date**: 2026-08-21
- **Commit Message**: `FEATURE: Implement Spaced Repetition engine, PDF generator, and OCR auto-grading`
- **Context**:
  - **Models**: `KanjiItem`, `JlptLevel`, `KanjiExample`, `StudyRecord`, `ReviewLogEntry`, `UserProfile`, `WorksheetConfig`, `WorksheetItem`, `GradingResult`, `GradingItemResult`.
  - **Data**: Seeded rich JLPT N5~N1 Kanji/Vocabulary dataset in `Data/kanji_dataset.json` with embedded resource fallback.
  - **Storage**: Implemented `JsonStorageService` for persisting library, study records, profile, worksheets, and gradings, plus backup export/import.
  - **SRS Engine**: Implemented `SrsEngineService` with calibrated Ebbinghaus / SM-2 algorithm, 4 mastery stages, streak tracker, and 7-day review forecast.
  - **PDF Generator**: Implemented `PdfWorksheetService` using QuestPDF generating pixel-perfect A4 study sheets (Full Study Table, Kanji Quiz, Reading Quiz, Meaning Quiz, Mixed Quiz).
  - **Auto-Grading**: Implemented `AutoGradingService` using `Windows.Media.Ocr`, Levenshtein fuzzy string similarity, worksheet ID pairing, and one-click SRS synchronization.

### [Step 3] Tailscale-Themed Adaptive UI, MVVM ViewModels, and Views
- **Date**: 2026-08-21
- **Commit Message**: `FEATURE: Build Tailscale-styled adaptive WPF UI and view components`
- **Context**:
  - **Styles & Themes**: Designed `TailscaleTheme.xaml` and `Icons.xaml` vector icons adhering to Tailscale's dark slate palette and status indicators.
  - **ViewModels**: Built `MainViewModel`, `DashboardViewModel`, `StudyReviewViewModel`, `WorksheetViewModel`, `GradingViewModel`, `DictionaryViewModel`, and `SettingsViewModel`.
  - **Views**: Implemented `DashboardView`, `StudyReviewView` (with keyboard shortcuts 1..4, Space), `WorksheetView` (live preview & export), `GradingView` (drag-and-drop photo upload, OCR analysis & scoring), `DictionaryView`, and `SettingsView`.

### [Step 4] Automated Test Suite & Sample Artifacts
- **Date**: 2026-08-21
- **Commit Message**: `ADDED: Add comprehensive xUnit test suite and sample worksheet artifacts`
- **Context**:
  - 19 automated xUnit tests covering SRS transitions, ease factor calculations, repository search, PDF byte signatures, OCR similarity matching, and JSON backup/restore.
  - Generated `samples/sample_kanji_quiz.pdf` and `samples/sample_completed_worksheet.png`.

### [Step 5] Bilingual Documentation & Branch Management
- **Date**: 2026-08-21
- **Commit Message**: `ADDED: Add bilingual documentation and configure branch structure`
- **Context**:
  - Updated `README.md` to fully support English and Korean side-by-side.
  - Established 3-tier branching model: `main` (Production/Release), `test` (Testing/QA), `develop` (Active Development).

### [Step 6] MIT License and 1,000+ Joyo Kanji Dataset with Attributions
- **Date**: 2026-08-21
- **Commit Message**: `FIXED: Stabilize Kanji dataset verification and synchronize dataset build across branches`
- **Context**:
  - Added MIT `LICENSE` file.
  - Expanded `kanji_dataset.json` to comprehensive 1,000+ Joyo/JLPT N5~N1 database with Onyomi, Kunyomi, Korean hun-eums, English meanings, stroke counts, and compound words.
  - Added `KanjiDatasetBuilder.cs` automated dataset builder and unit tests (20 passing tests).
  - Documented open-source data attributions in `README.md` (KANJIDIC2/JMdict under CC BY-SA 4.0, MEXT Joyo Kanji, Anki Japanese Core Decks, National Institute of Korean Language).
