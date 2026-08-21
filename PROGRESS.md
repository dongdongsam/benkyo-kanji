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
