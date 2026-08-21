# 🌸 Benkyo Kanji (勉強漢字)

[English](#english) | [한국어](#한국어)

---

<a name="english"></a>
## 🇬🇧 English

> **Adaptive Japanese Kanji & Vocabulary Spaced Repetition Learning Desktop App with Printable PDF Worksheets & OCR Photo Auto-Grading**  
> Built with C# .NET 8, WPF, and Tailscale-inspired modern dark aesthetics.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512bd4)](https://dotnet.microsoft.com/)
[![JLPT Kanji](https://img.shields.io/badge/Kanji%20Database-1000%2B%20Entries-10b981)](#-data-sources--attribution)

---

### 🌟 Key Features

#### 1. 🧠 Spaced Repetition System (SRS) powered by Ebbinghaus Forgetting Curve
- **Scientifically Scheduled Reviews**: Uses modified SuperMemo SM-2 and Ebbinghaus forgetting curve algorithms to schedule flashcard reviews right before memory decay.
- **4-Stage Mastery Progression**: Cards transition seamlessly across `New` ➜ `Learning` ➜ `Reviewing` ➜ `Mastered`.
- **Retention Analytics & 7-Day Forecast**: Visual breakdown of memory retention rate, daily streak tracking, and a 7-day upcoming review load forecast.
- **Interactive Flashcards**: Smooth card flipping, Onyomi (katakana/hiragana), Kunyomi (hiragana), Korean/English meanings, stroke count, radicals, example compounds, and keyboard shortcuts (`1`, `2`, `3`, `4`, `Space`).

#### 2. 📄 Printable A4 PDF Study & Quiz Worksheets (QuestPDF)
- **Versatile Quiz Modes**:
  - **Kanji Quiz**: Blanks out the Kanji column for handwriting practice based on readings and meanings.
  - **Reading Quiz**: Blanks out Onyomi/Kunyomi for phonetic reading practice based on Kanji and meanings.
  - **Meaning Quiz**: Blanks out Korean meanings to test definition recall.
  - **Full Reference Table**: Complete reference sheet containing Kanji, readings, meanings, stroke counts, and example compounds.
  - **Mixed Challenge Quiz**: Dynamically randomizes blank fields across items.
- **Configurable Scope**:
  - Filter by JLPT levels (`N5`, `N4`, `N3`, `N2`, `N1`, or `All`).
  - Prioritize cards that are currently due for review (`Due Items`).
  - Toggle stroke counts, radicals, example sentences, and header fields (Name, Date, Score).
  - Embedded Worksheet ID barcode for pairing with the photo auto-grader.

#### 3. 📷 Photo Worksheet Auto-Grading Scanner (WinRT OCR & Vision)
- **Handwritten Worksheet Photo Grading**: Upload photos or scans of filled worksheets via drag & drop, file picker, or clipboard paste (`Ctrl+V`).
- **Multi-Engine Recognition**:
  - Native offline `Windows.Media.Ocr` engine for Windows 10/11.
  - Smart Levenshtein distance & fuzzy character matching for handwriting recognition.
  - Optional AI Vision API (Google Gemini / OpenAI Vision) integration for challenging handwriting.
- **Detailed Scoring Breakdown**: Real-time score calculation (`Correct`, `Partial`, `Incorrect`), visual color badges, and manual override capabilities.
- **Instant SRS Synchronization**: One-click sync that updates your Ebbinghaus memory intervals and study logs directly from the graded photo.

#### 4. 📖 Comprehensive 1,000+ Kanji Database & Custom Library
- **1,000+ Jōyō & JLPT N5~N1 Kanji**: Pre-seeded with over a thousand standard Japanese characters, On/Kun readings, Korean definitions (훈음), English definitions, stroke counts, radicals, and example compounds.
- Search Kanji, readings, and definitions in real time.
- Create and manage custom Kanji and vocabulary cards.
- Complete JSON export and import for seamless data backup and restoration.

#### 5. 🎨 Tailscale-Inspired Adaptive GUI
- Sleek dark theme using Tailscale's Charcoal (`#131217`), Slate (`#1e1d24`), Indigo (`#6366f1`), and Emerald (`#10b981`).
- Responsive layouts that adapt smoothly to FHD, QHD, 4K, and window resizing.

---

### 📚 Data Sources & Attribution

The Kanji & vocabulary database included in this project incorporates open-source linguistic data from the following verified resources:

1. **KANJIDIC2 & JMdict Project**:
   - Maintained by the **Electronic Dictionary Research and Development Group (EDRDG)** / Jim Breen.
   - License: [Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)](https://creativecommons.org/licenses/by-sa/4.0/).
   - URL: [http://www.edrdg.org/wiki/index.php/KANJIDIC_Project](http://www.edrdg.org/wiki/index.php/KANJIDIC_Project)
2. **Japanese Ministry of Education (MEXT) Jōyō Kanji List & JLPT Anki Shared Decks**:
   - Official 2,136 Jōyō Kanji (常用漢字) stroke counts, radicals, and JLPT N5~N1 classification based on open Anki core decks (Japanese Core 2000/6000 & JLPT Kanji Master).
3. **National Institute of Korean Language (국립국어원)**:
   - Standard Sino-Korean readings (훈음, 訓音) and Korean definitions mapping.

---

### 🛠 Tech Stack

- **Framework**: C# 12, .NET 8 WPF (`net8.0-windows10.0.19041.0`)
- **Architecture**: MVVM Pattern (`CommunityToolkit.Mvvm`)
- **PDF Generation**: QuestPDF
- **OCR & Image Processing**: `Windows.Media.Ocr`, `Windows.Graphics.Imaging`
- **Data Persistence**: `System.Text.Json` (Local storage in `%AppData%/BenkyoKanji/data`)
- **Testing**: xUnit 2.5, .NET Test SDK

---

### 🚀 Getting Started & Build

#### Prerequisites
- Windows 10 (Build 19041+) or Windows 11
- .NET 8.0 SDK or higher

#### Commands
```powershell
# Clone the repository
git clone git@github.com:dongdongsam/benkyo-kanji.git
cd benkyo-kanji

# Build solution
dotnet build

# Run automated test suite (20 unit tests)
dotnet test

# Launch WPF application
dotnet run --project BenkyoKanji/BenkyoKanji.csproj
```

---

<a name="한국어"></a>
## 🇰🇷 한국어

> **에빙하우스 망각 곡선 기반 일본어 한자·어휘 학습 & 인쇄용 PDF 시험지 자동 채점 데스크톱 시스템**  
> Tailscale 스타일의 모던 다크 UI와 고해상도 반응형 레이아웃을 제공하는 C# .NET 8 WPF 데스크톱 애플리케이션입니다.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512bd4)](https://dotnet.microsoft.com/)
[![상용한자 데이터베이스](https://img.shields.io/badge/한자%20데이터셋-1000자%20이상-10b981)](#-데이터-출처-및-라이선스-표기)

---

### 🌟 주요 기능

#### 1. 🧠 에빙하우스 망각 곡선 기반 간격 반복 시스템 (SRS)
- **과학적 복습 스케줄링**: SuperMemo SM-2 및 에빙하우스 망각 곡선 알고리즘을 적용하여 망각 직전 최적의 타이밍에 복습 큐(Due Queue) 자동 생성.
- **4단계 숙련도 관리**: 미학습(`New`) ➜ 초기 학습(`Learning`) ➜ 주기 복습(`Reviewing`) ➜ 완전 암기(`Mastered`) 단계 자동 전이.
- **학습 통계 & 향후 7일 복습 예보**: 정답률, 연속 학습 스트릭(Streak), 일일 목표 달성도, 향후 7일간의 복습 부하 예측 차트 제공.
- **인터랙티브 플래시카드**: 단어 카드 뒤집기, 음독/훈독/뜻/예문 확인, 키보드 단축키(`1`, `2`, `3`, `4`, `Space`) 지원.

#### 2. 📄 종이 출력용 맞춤형 A4 PDF 시험지·학습표 생성기 (QuestPDF)
- **다양한 시험지 모드**:
  - **한자 쓰기 테스트**: 음훈독과 뜻을 보고 한자를 직접 적는 한자 빈칸 시험지.
  - **음독·훈독 읽기 테스트**: 한자와 뜻을 보고 히라가나/가타카나를 적는 읽기 빈칸 시험지.
  - **한국어 뜻 테스트**: 한자와 음훈독을 보고 한국어 훈음을 적는 뜻 빈칸 시험지.
  - **전체 학습 정리표**: 한자, 음훈독, 뜻, 대표 활용 어휘가 모두 포함된 암기용 완성표.
  - **종합 혼합 테스트**: 항목별 무작위 빈칸 챌린지.
- **출제 범위 & 세부 옵션**:
  - JLPT 급수별(`N5`, `N4`, `N3`, `N2`, `N1`, `전체`) 필터링 및 문항 수(10~50) 조절.
  - 망각 곡선 복습 대기(`Due`) 한자 우선 출제 지원.
  - 획수, 부수, 대표 예문 및 파생 어휘 표기 옵션.
  - 시험지 고유 식별 코드(Worksheet ID) 자동 삽입으로 사진 채점 시 원본 시험지 자동 매칭.

#### 3. 📷 작성한 시험지 사진 자동 채점기 (OCR Scanner & Vision)
- **손글씨 사진 채점**: 사용자가 종이 시험지에 빈칸을 채우고 촬영한 사진을 드래그 앤 드롭 / 파일 선택 / 클립보드 붙여넣기(`Ctrl+V`)로 업로드.
- **다중 엔진 지원**:
  - Windows 10/11 네이티브 `Windows.Media.Ocr` 내장 오프라인 OCR 지원.
  - 레벤슈타인 거리(Levenshtein Distance) 및 자모/카나 정규화 기반 유사도 판정.
  - 선택 사항: Google Gemini / OpenAI Vision API 키 연동 지원.
- **문항별 상세 피드백 & 수동 수정**: 정답(초록), 부분 정답(노랑), 오답(빨강) 시각화 및 사용자 수정 기능.
- **망각 곡선 즉시 동기화**: 채점 결과를 원클릭으로 사용자 SRS 학습 데이터에 반영(오답 시 복습 주기 자동 초기화).

#### 4. 📖 1,000자 이상 상용한자·어휘 사전 & 커스텀 단어장
- **1,000자 이상의 일본 상용한자 및 JLPT N5~N1 한자 데이터셋 기본 탑재**.
- 한자, 음독, 훈독, 한국어 뜻(훈음), 영문 뜻, 파생 어휘 실시간 통합 검색.
- 사용자 정의 커스텀 단어 등록 및 관리.
- 표준 JSON 기반 전체 데이터 백업 및 복원 지원.

#### 5. 🎨 Tailscale 스타일의 적응형 GUI (Modern Dark UI)
- Tailscale 딥 차콜(`#131217`), 슬레이트(`#1e1d24`), 인디고(`#6366f1`), 에메랄드(`#10b981`) 정제된 컬러 팔레트.
- 라운드 카드 레이아웃, 상태 표시등 펄스, 세련된 사이드바 네비게이션.
- FHD, QHD, 4K 및 창 크기 변경에 유연하게 대응하는 반응형 적응형 레이아웃.

---

### 📚 데이터 출처 및 라이선스 표기

본 프로젝트에 탑재된 일본어 한자 및 어휘 데이터는 다음의 검증된 오픈소스 데이터베이스를 기반으로 구축되었습니다:

1. **KANJIDIC2 & JMdict Project**:
   - **Electronic Dictionary Research and Development Group (EDRDG)** / Jim Breen 제작 및 유지보수.
   - 라이선스: [Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)](https://creativecommons.org/licenses/by-sa/4.0/).
   - 공식 웹사이트: [http://www.edrdg.org/wiki/index.php/KANJIDIC_Project](http://www.edrdg.org/wiki/index.php/KANJIDIC_Project)
2. **일본 문부과학성(MEXT) 상용한자 목록 및 Anki JLPT 공유 덱**:
   - 일본 상용한자 2,136자 획수, 부수 및 JLPT N5~N1 난이도 분류 (Anki Japanese Core 2000/6000 & JLPT Kanji Master Deck).
3. **국립국어원 표준 한자음 및 한국어 훈음 매핑 데이터**:
   - 각 한자의 대표 한국어 훈음(예: '날 일, 해', '사람 인', '배울 학') 및 한자어 뜻풀이.

---

### 🌿 브랜치 전략 (Branching Strategy)

- **`main`**: 정식 릴리스 버전 (Production / Stable Release)
- **`test`**: 테스트 및 QA 검증 버전 (Test / Staging / QA)
- **`develop`**: 신규 기능 개발 및 작업 버전 (Active Development)

---

## 📁 프로젝트 구조 (Project Structure)

```
benkyo-kanji/
├── BenkyoKanji/                  # 메인 WPF 애플리케이션
│   ├── Converters/              # XAML 바인딩 변환기
│   ├── Data/                    # JLPT N5~N1 1,000자 이상 한자/어휘 데이터셋 (JSON)
│   ├── Models/                  # KanjiItem, StudyRecord, WorksheetConfig, GradingResult
│   ├── Services/                # JsonStorage, SrsEngine, PdfWorksheet, AutoGrading
│   ├── Styles/                  # TailscaleTheme.xaml, Icons.xaml
│   ├── ViewModels/              # Dashboard, Study, Worksheet, Grading, Dictionary, Settings
│   ├── Views/                   # DashboardView, StudyReviewView, WorksheetView, GradingView, etc.
│   ├── App.xaml / MainWindow.xaml
│   └── BenkyoKanji.csproj
├── BenkyoKanji.Tests/           # 단위 테스트 프로젝트 (xUnit)
│   ├── UnitTest1.cs
│   └── KanjiDatasetBuilder.cs
├── samples/                     # 샘플 출력 PDF 및 채점용 이미지
│   ├── sample_kanji_quiz.pdf
│   └── sample_completed_worksheet.png
├── LICENSE                      # MIT License
├── Main_Prompots.txt            # 요구사항 원본
├── PROGRESS.md                  # 커밋별 진행 상황 및 맥락 기록
└── README.md                    # 프로젝트 가이드 (Bilingual & Attribution)
```

---

## 📄 라이선스 (License)
본 소프트웨어는 [MIT License](LICENSE)에 따라 배포됩니다.  
포함된 한자 데이터는 [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) 라이선스를 준수합니다.
