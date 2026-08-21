using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using BenkyoKanji.Models;
using Xunit;

namespace BenkyoKanji.Tests;

public class KanjiDatasetBuilderTests
{
    private static readonly Dictionary<string, string> KoreanHunEumMap = new()
    {
        {"日", "날 일, 해"}, {"一", "한 일"}, {"国", "나라 국"}, {"会", "모일 회"}, {"年", "해 년"},
        {"大", "큰 대"}, {"十", "열 십"}, {"二", "두 이"}, {"本", "근본 본, 책"}, {"中", "가운데 중"},
        {"長", "길 장, 어른"}, {"出", "날 출"}, {"三", "석 삼"}, {"同", "한가지 동"}, {"時", "때 시, 시간"},
        {"行", "다닐 행"}, {"見", "볼 견"}, {"月", "달 월"}, {"分", "나눌 분"}, {"後", "뒤 후"},
        {"前", "앞 전"}, {"生", "날 생, 살 생"}, {"五", "다섯 오"}, {"間", "사이 간"}, {"上", "위 상"},
        {"東", "동녘 동"}, {"四", "넉 사"}, {"今", "이제 금"}, {"金", "쇠 금, 돈"}, {"九", "아홉 구"},
        {"入", "들 입"}, {"学", "배울 학"}, {"高", "높을 고"}, {"円", "둥글 원, 엔"}, {"子", "아들 자, 아이"},
        {"外", "바깥 외"}, {"八", "여덟 팔"}, {"六", "여섯 육"}, {"下", "아래 하"}, {"来", "올 래"},
        {"気", "기운 기, 기분"}, {"小", "작을 소"}, {"七", "일곱 칠"}, {"山", "뫼 산"}, {"話", "말씀 화, 이야기"},
        {"女", "계집 녀, 여자"}, {"北", "북녘 북"}, {"午", "낮 오"}, {"百", "일백 백"}, {"書", "글 서, 쓸 서"},
        {"先", "먼저 선"}, {"名", "이름 명"}, {"川", "내 천, 강"}, {"千", "일천 천"}, {"水", "물 수"},
        {"半", "반 반, 절반"}, {"男", "사내 남"}, {"西", "서녘 서"}, {"電", "번개 전, 전기"}, {"校", "학교 교"},
        {"語", "말씀 어, 언어"}, {"土", "흙 토"}, {"木", "나무 목"}, {"聞", "들을 문"}, {"食", "밥 식, 먹을 식"},
        {"車", "수레 차, 자동차"}, {"何", "어찌 하, 무엇"}, {"南", "남녘 남"}, {"万", "일만 만"}, {"毎", "매양 매, 매일"},
        {"白", "흰 백"}, {"天", "하늘 천"}, {"母", "어미 모, 어머니"}, {"火", "불 화"}, {"右", "오른 우"},
        {"読", "읽을 독"}, {"友", "벗 우, 친구"}, {"左", "왼 좌"}, {"休", "쉴 휴"}, {"父", "아비 부, 아버지"},
        {"雨", "비 우"}, {"安", "편안할 안"}, {"新", "새 신"}, {"少", "적을 소"}, {"多", "많을 다"},
        {"店", "가게 점"}, {"道", "길 도"}, {"社", "모일 사, 회사"}, {"買", "살 매"}, {"飲", "마실 음"},
        {"立", "설 립"}, {"手", "손 수"}, {"目", "눈 목"}, {"足", "발 족"}, {"耳", "귀 이"},
        {"口", "입 구"}, {"花", "꽃 화"}, {"魚", "물고기 어"}, {"空", "빌 공, 하늘"}, {"犬", "개 견"},
        {"人", "사람 인"}, {"心", "마음 심"}, {"身", "몸 신"}, {"体", "몸 체"}, {"力", "힘 력"},
        {"強", "강할 강"}, {"勉", "힘쓸 면"}, {"旅", "나그네 려"}, {"館", "집 관, 큰 건물"}, {"質", "바탕 질, 질문"},
        {"問", "물을 문"}, {"題", "제목 제"}, {"験", "시험 험"}, {"政", "정사 정, 정치"}, {"治", "다스릴 치, 치료"},
        {"経", "지날 경, 경영"}, {"済", "건널 제, 끝날 제"}, {"保", "지킬 보, 보존"}, {"険", "험할 험, 위험"}, {"際", "즈음 제, 국제"},
        {"構", "얽을 구, 구조"}, {"造", "지을 조, 제조"}, {"識", "알 식, 지식"}, {"拡", "넓힐 확, 확장"}, {"貿", "무역할 무"},
        {"易", "바꿀 역, 쉬울 이"}, {"憂", "근심 우"}, {"慮", "생각할 려, 배려"}, {"曖", "희미할 애"}, {"昧", "어두울 매"},
        {"弊", "폐단 폐"}, {"遵", "좇을 준, 준수"}, {"緻", "빽빽할 치, 치밀"}, {"密", "빽빽할 밀, 비밀"}
    };

    [Fact]
    public async Task BuildAndVerifyComprehensiveKanjiDataset_ContainsOver1000Items()
    {
        var targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BenkyoKanji", "Data", "kanji_dataset.json");
        
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "BenkyoKanji-Builder/1.0");

        var kanjiItems = new List<KanjiItem>();
        var seen = new HashSet<string>();

        // 1. Attempt to fetch KANJIDIC open dataset from GitHub
        try
        {
            var url = "https://raw.githubusercontent.com/davidluzgouveia/kanji-data/master/kanji.json";
            var json = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var k = prop.Name;
                var el = prop.Value;

                int level = 1;
                if (el.TryGetProperty("jlpt_new", out var jn) && jn.ValueKind == JsonValueKind.Number)
                {
                    level = jn.GetInt32();
                }
                else if (el.TryGetProperty("jlpt", out var j) && j.ValueKind == JsonValueKind.Number)
                {
                    level = j.GetInt32();
                }
                else if (el.TryGetProperty("grade", out var g) && g.ValueKind == JsonValueKind.Number)
                {
                    int grade = g.GetInt32();
                    if (grade <= 2) level = 5;
                    else if (grade <= 4) level = 4;
                    else if (grade <= 6) level = 3;
                    else if (grade <= 8) level = 2;
                    else level = 1;
                }

                if (level < 1 || level > 5) level = 1;

                var onList = new List<string>();
                if (el.TryGetProperty("readings_on", out var onEl))
                {
                    foreach (var o in onEl.EnumerateArray()) onList.Add(o.GetString() ?? "");
                }

                var kunList = new List<string>();
                if (el.TryGetProperty("readings_kun", out var kunEl))
                {
                    foreach (var ku in kunEl.EnumerateArray()) kunList.Add(ku.GetString() ?? "");
                }

                var meanList = new List<string>();
                if (el.TryGetProperty("meanings", out var meanEl))
                {
                    foreach (var m in meanEl.EnumerateArray()) meanList.Add(m.GetString() ?? "");
                }

                int strokes = 1;
                if (el.TryGetProperty("strokes", out var sEl)) strokes = sEl.GetInt32();

                string radical = "";
                if (el.TryGetProperty("radical", out var rEl)) radical = rEl.GetString() ?? "";

                string meaningEn = string.Join(", ", meanList);
                string onStr = string.Join(", ", onList.Where(x => !string.IsNullOrEmpty(x)));
                string kunStr = string.Join(", ", kunList.Where(x => !string.IsNullOrEmpty(x)));

                string meaningKo = KoreanHunEumMap.TryGetValue(k, out var km) 
                    ? km 
                    : (meanList.Count > 0 ? $"한자 ({meanList[0]})" : "한자");

                var examples = new List<KanjiExample>();
                if (onList.Count > 0 && !string.IsNullOrEmpty(onList[0]))
                {
                    examples.Add(new KanjiExample
                    {
                        Word = k,
                        Reading = onList[0],
                        Meaning = meaningKo
                    });
                }

                var item = new KanjiItem
                {
                    Id = $"k-{kanjiItems.Count + 1:D4}",
                    Kanji = k,
                    Onyomi = onStr,
                    Kunyomi = kunStr,
                    MeaningKo = meaningKo,
                    MeaningEn = meaningEn,
                    Level = (JlptLevel)level,
                    StrokeCount = strokes,
                    Radical = radical,
                    Examples = examples,
                    Tags = [$"N{level}", "상용한자", "JLPT"]
                };

                if (!seen.Contains(k))
                {
                    seen.Add(k);
                    kanjiItems.Add(item);
                }
            }
        }
        catch
        {
            // If offline, rely on existing items
        }

        // Save compiled JSON if we gathered >= 1000 items
        if (kanjiItems.Count >= 1000)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            var serialized = JsonSerializer.Serialize(kanjiItems, options);
            await File.WriteAllTextAsync(targetFile, serialized);

            var binDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(binDataDir);
            await File.WriteAllTextAsync(Path.Combine(binDataDir, "kanji_dataset.json"), serialized);
        }

        // Verify dataset on disk
        Assert.True(File.Exists(targetFile));
        var diskJson = await File.ReadAllTextAsync(targetFile);
        var loadedItems = JsonSerializer.Deserialize<List<KanjiItem>>(diskJson);
        
        Assert.NotNull(loadedItems);
        Assert.True(loadedItems.Count >= 1000, $"Expected >= 1000 items, but got {loadedItems.Count}");
    }
}
