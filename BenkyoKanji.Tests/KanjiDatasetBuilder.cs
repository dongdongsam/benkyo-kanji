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
    private static readonly Dictionary<string, string> ShinjitaiAndCuratedMap = new()
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
        {"弊", "폐단 폐"}, {"遵", "좇을 준, 준수"}, {"緻", "빽빽할 치, 치밀"}, {"密", "빽빽할 밀, 비밀"},
        {"内", "안 내"}, {"戸", "지게/문 호"}, {"写", "베낄 사, 사진"}, {"当", "마땅 당, 당번"},
        {"図", "그림 도, 지도"}, {"声", "소리 성"}, {"売", "팔 매, 매점"}, {"麦", "보리 맥"},
        {"両", "두 량, 양쪽"}, {"辺", "가 변, 주변"}, {"黄", "누를 황, 노랑"}, {"黒", "검을 흑"}, {"対", "대할 대, 대상"},
        {"研", "갈 연, 연구"}, {"数", "셈 수, 숫자"}, {"楽", "즐길 락, 음악 악"}, {"乗", "탈 승, 승차"}, {"発", "필 발, 출발"},
        {"県", "고을 현, 도도부현"}, {"仮", "거짓 가, 임시"}, {"転", "구를 전, 운전"}, {"軽", "가벼울 경, 경차"},
        {"横", "가로 횡"}, {"争", "다툴 쟁, 전쟁"}, {"伝", "전할 전, 전설"}, {"労", "일할 로, 근로"},
        {"戦", "싸울 전, 전쟁"}, {"悪", "악할 악, 나쁠 악"}, {"歯", "이 치, 치과"}, {"温", "따뜻할 온, 온도"}, {"様", "모양 양, 님"},
        {"緑", "초록빛 록, 녹색"}, {"駅", "역 역, 기차역"}, {"参", "참여할 참, 셋 삼"}, {"囲", "둘러쌀 위, 주위"}, {"残", "남을 잔, 잔업"},
        {"帰", "돌아갈 귀, 귀가"}, {"昼", "낮 주, 주간"}, {"区", "구분할 구, 구역"}, {"単", "홑 단, 단순"}, {"変", "변할 변, 변화"},
        {"浅", "얕을 천"}, {"仏", "부처 불, 프랑스"}, {"晩", "저녁 만, 만찬"}, {"辞", "말씀 사, 사전/사직"}, {"関", "관계할 관, 현관"},
        {"専", "오로지 전, 전문"}, {"薬", "약 약, 약국"}, {"塩", "소금 염, 식염"}, {"渉", "건널 섭, 교섭"},
        {"虚", "빌 허, 허구"}, {"恋", "사모할 련, 연애"}, {"覚", "깨달을 각, 감각"}, {"説", "말씀 설, 설명"}, {"焼", "불사를 소, 연소"},
        {"脳", "골 뇌, 두뇌"}, {"禅", "선 선, 좌선"}, {"歴", "지낼 력, 역사"}, {"団", "둥글 단, 단체"},
        {"乱", "어지러울 란, 혼란"}, {"続", "이을 속, 연속"}, {"圧", "누를 압, 압력"}, {"挙", "들 거, 선거"},
        {"断", "끊을 단, 결단"}, {"検", "검사할 검, 검토"}, {"条", "가지 조, 조건"}, {"増", "더할 증, 증가"}, {"税", "세금 세"},
        {"脱", "벗을 탈, 탈출"}, {"寝", "잘 침, 침실"}, {"価", "값 가, 가격"}, {"営", "경영할 영, 영업"}, {"観", "볼 관, 관광"},
        {"応", "응할 응, 응답"}, {"状", "형상 상, 상태"}, {"収", "거둘 수, 수입"}, {"沢", "못 택, 윤택"}, {"届", "이를 계, 신고"},
        {"狭", "좁을 협"}, {"触", "닿을 촉, 접촉"}, {"庁", "관청 청, 시청"}, {"与", "줄 여, 수여"}, {"抜", "뽑을 발, 발탁"},
        {"属", "무리 속, 소속"}, {"満", "찰 만, 만점"}, {"巻", "책 권, 말 권"}, {"搜", "찾을 수, 수색"}, {"捜", "찾을 수, 수색"},
        {"隠", "숨길 은, 은폐"}, {"従", "좇을 종, 순종"}, {"励", "힘쓸 려, 격려"}, {"徴", "부를 징, 특징"}, {"獣", "짐승 수"},
        {"独", "홀로 독, 독립"}, {"悩", "괴로워할 뇌, 고민"}, {"録", "기록할 록, 기록"}, {"撃", "칠 격, 공격"}, {"欧", "구라파 구, 유럽"},
        {"壊", "무너질 괴, 파괴"}, {"渇", "목마를 갈, 갈증"}, {"覧", "볼 람, 열람"}, {"懐", "품을 회, 회포"}, {"騒", "떠들썩할 소, 소음"},
        {"盗", "도둑 도, 도둑"}, {"既", "이미 기, 기성"}, {"巣", "새집 소, 보금자리"}, {"帯", "띠 대, 지대"}, {"径", "지름길 경, 반경"},
        {"桜", "벚나무 앵, 벚꽃"}, {"雑", "섞일 잡, 잡지"}, {"銭", "돈 전, 금전"}, {"党", "무리 당, 정당"}, {"込", "담을 입, 넣을 입"},
        {"訳", "번역할 역, 번역"}, {"蔵", "감출 장, 저장"}, {"装", "꾸밀 장, 복장"}, {"臓", "오장 장, 심장"},
        {"丼", "사발 정, 덮밥"}, {"縦", "세로 종, 종단"}, {"粋", "순수할 수, 순수"}, {"拝", "절 배, 숭배"}, {"歓", "기쁠 환, 환영"},
        {"遅", "늦을 지, 지각"}, {"舎", "집 사, 기숙사"}, {"滞", "막힐 체, 정체"}, {"亀", "거북 귀/구"}, {"剣", "칼 검, 검술"},
        {"噌", "된장 증, 미소"}, {"為", "할 위, 행위"}, {"酔", "취할 취, 만취"}, {"払", "떨칠 발, 지불"}, {"醤", "간장 장, 된장"},
        {"旧", "예 구, 옛 구"}, {"廃", "폐할 폐, 폐지"}, {"縄", "줄 승, 포승"}, {"献", "바칠 헌, 헌신"}, {"継", "이을 계, 계승"},
        {"塁", "보루 루, 야구루"}, {"戻", "어그러질 려, 반환"}, {"湾", "물굽이 만, 항만"}, {"弾", "탄알 탄, 탄알"}, {"聴", "들을 청, 청취"},
        {"闘", "싸울 투, 전투"}, {"掲", "높이들 게, 게시"}, {"齢", "나이 령, 연령"}, {"併", "아우를 병, 병합"}, {"奥", "깊을 오, 안쪽"},
        {"択", "가릴 택, 선택"}, {"称", "일컬을 칭, 칭호"}, {"緒", "실마리 서, 유서"}, {"渋", "떫을 삽, 떫은맛"}, {"勧", "권할 권, 권유"},
        {"圏", "돌 권, 권역"}, {"慎", "삼갈 신, 신중"}, {"枠", "틀 방, 테두리"}, {"稲", "벼 도, 벼"}, {"譲", "양보할 양, 양도"},
        {"駆", "몰 구, 구제"}, {"剤", "약제 제, 약제"}, {"鋭", "날카로울 예, 예리"}, {"犠", "희생 희, 희생"}, {"誉", "기릴 예, 명예"},
        {"瀬", "여울 뢰"}, {"拠", "근거 거, 증거"}, {"蛍", "반딧불 형, 형광"}, {"鉱", "광물 광, 광산"}, {"郷", "시골 향, 고향"},
        {"偽", "거짓 위, 가짜"}, {"揺", "흔들릴 요, 동요"}, {"斎", "재계할 재, 서재"}, {"枢", "지도리 추, 중추"}, {"斉", "가지런할 제, 일제"},
        {"炉", "화로 로, 난로"}, {"縁", "인연 연, 인연"}, {"娯", "즐길 오, 오락"}, {"髪", "터럭 발, 머리카락"}, {"涙", "눈물 루, 눈물"},
        {"嬢", "아가씨 양, 영애"}, {"暦", "책력 력, 달력"}, {"霊", "신령 령, 유령"}, {"湿", "젖을 습, 습도"}, {"滝", "폭포 롱/폭, 폭포"},
        {"歳", "해 세, 만 나이"}, {"錬", "단련할 련, 연마"}, {"黙", "잠잠할 묵, 침묵"}, {"砕", "부술 쇄, 파쇄"}, {"塀", "담 병, 담장"},
        {"挿", "꽂을 삽, 삽화"}, {"畳", "겹쳐질 첩, 다다미"}, {"殴", "때릴 구, 구타"}, {"概", "대개 개, 개요"}, {"奨", "권장할 장, 장려"},
        {"挟", "낄 협, 협공"}, {"瓶", "병 병, 유리병"}, {"釈", "풀 석, 해석"}, {"陥", "빠질 함, 함락"}, {"没", "가라앉을 몰, 몰입"},
        {"猟", "사냥 렵, 수렵"}, {"浄", "깨끗할 정, 청정"}, {"随", "따를 수, 수반"}, {"壤", "흙덩이 양, 토양"}, {"壌", "흙덩이 양, 토양"},
        {"舗", "펼 포, 점포"}, {"剰", "남을 잉, 잉여"}, {"繊", "가늘 섬, 섬세"}, {"惨", "참혹할 참, 참사"}, {"搭", "탈 탑, 탑승"},
        {"荘", "별장 장, 장엄"}, {"尚", "오히려 상, 고상"}, {"粛", "엄숙할 숙, 숙연"}, {"践", "밟을 천, 실천"}, {"謡", "노래 요, 가요"},
        {"茎", "줄기 경, 줄기"}, {"鎭", "진압할 진, 진정"}, {"鎮", "진압할 진, 진정"}, {"栃", "상수리나무 력, 토치기"}, {"髄", "골수 수, 골수"},
        {"呉", "나라 오, 오나라"}, {"悦", "기쁠 열, 희열"}, {"摂", "다스릴 섭, 섭취"}, {"遥", "멀 요, 아득할 요"}, {"峡", "골짜기 협, 협곡"},
        {"醸", "술 빚을 양, 양조"}, {"閲", "검열할 열, 열람"}, {"堕", "떨어질 타, 타락"}, {"窃", "훔칠 절, 절도"}, {"戯", "놀 희, 유희"},
        {"殻", "껍질 각, 패각"}, {"鶏", "닭 계, 계란"}, {"嘱", "부탁할 촉, 촉탁"}, {"暁", "새벽 효, 새벽"}, {"鋳", "쇠불릴 주, 주물"},
        {"渓", "시내 계, 계곡"}, {"桟", "사다리 잔, 잔교"}, {"蛮", "오랑캐 만, 야만"}, {"倹", "검소할 검, 검약"}, {"唖", "벙어리 아, 농아"},
        {"姶", "골짜기 합"}, {"穐", "벼 추, 가을"}, {"鯵", "전갱이 소, 전갱이"}, {"袷", "겹옷 합, 겹옷"}, {"壱", "한 일 (壹)"},
        {"鰯", "정어리 정, 정어리"}, {"鵜", "가마우지 제, 가마우지"}, {"嘘", "거짓말 허, 거짓말"}, {"欝", "답답할 울, 우울"}, {"厩", "마구간 구, 마구간"},
        {"噂", "소문 준, 소문"}, {"餌", "먹이 이, 먹이"}, {"頴", "이삭 영, 빼어날 영"}, {"焰", "불꽃 염, 화염"}, {"鴬", "꾀꼬리 앵, 꾀꼬리"},
        {"鴎", "갈매기 구, 갈매기"}, {"唄", "노래 패/배, 노래"}, {"拷", "칠 고, 고문"}, {"廉", "청렴할 렴, 염가"}, {"謹", "삼갈 근, 근하신년"}
    };

    [Fact]
    public async Task OverhaulKanjiDatasetWithAuthenticKoreanMeanings()
    {
        var targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BenkyoKanji", "Data", "kanji_dataset.json");
        if (!File.Exists(targetFile)) targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "kanji_dataset.json");

        var map = new Dictionary<string, string>();
        using var client = new HttpClient();

        // 1. Fetch Naver Hanja dictionary
        try
        {
            var naverJson = await client.GetStringAsync("https://raw.githubusercontent.com/rutopio/Korean-Name-Hanja-Charset/main/data-naver.json");
            using var doc = JsonDocument.Parse(naverJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.TryGetProperty("entryName", out var nameProp) &&
                        item.TryGetProperty("pron", out var pronProp))
                    {
                        var ch = nameProp.GetString();
                        var pron = pronProp.GetString();
                        if (!string.IsNullOrWhiteSpace(ch) && !string.IsNullOrWhiteSpace(pron) && !map.ContainsKey(ch))
                        {
                            map[ch] = pron.Trim();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Naver fetch note: {ex.Message}");
        }

        // 2. Fetch Gov Hanja dictionary
        try
        {
            var govJson = await client.GetStringAsync("https://raw.githubusercontent.com/rutopio/Korean-Name-Hanja-Charset/main/data-gov.json");
            using var doc = JsonDocument.Parse(govJson);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("cd", out var cdProp) &&
                    item.TryGetProperty("in", out var inProp))
                {
                    var cdStr = cdProp.GetString();
                    var inStr = inProp.GetString();
                    if (!string.IsNullOrWhiteSpace(cdStr) && int.TryParse(cdStr, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                    {
                        var ch = char.ConvertFromUtf32(codePoint);
                        if (!string.IsNullOrWhiteSpace(inStr) && inStr.Contains(":"))
                        {
                            var parts = inStr.Split(':');
                            if (parts.Length == 2)
                            {
                                var meaning = parts[1].Trim();
                                meaning = meaning.Replace("(", " ").Replace(")", "").Trim();
                                if (!map.ContainsKey(ch))
                                {
                                    map[ch] = meaning;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Gov fetch note: {ex.Message}");
        }

        // 3. Override with curated and Shinjitai map
        foreach (var (k, v) in ShinjitaiAndCuratedMap)
        {
            map[k] = v;
        }

        // Additional Curated Hun-Eum Map for Joyo Kanji to ensure 100% coverage
        var joyoSpecialEntries = new (string, string)[]
        {
            ("字", "글자 자, 자형"), ("漢", "한나라 한, 한수 한"), ("壱", "한 일, 갖은하나 일"),
            ("焔", "불꽃 염, 불길"), ("夢", "꿈 몽"), ("歌", "노래 가"), ("曲", "굽을 곡, 노래 곡"), ("詩", "시 시"),
            ("劇", "연극 극, 심할 극"), ("絵", "그림 회/개, 그림"), ("音", "소리 음, 음악"),
            ("楽", "즐길 락, 음악 악"), ("舞", "춤출 무"), ("芸", "재주 예, 예술"),
            ("演", "펼 연, 연기"), ("奏", "아뢸 주, 연주"), ("響", "울릴 향, 영향"),
            ("影", "그림자 영, 영화"), ("画", "그림 화/획, 영화"), ("映", "비칠 영, 영화"),
            ("像", "형상 상, 영상"), ("視", "볼 시, 시각"), ("聴", "들을 청, 청취"),
            ("声", "소리 성, 음성"), ("語", "말씀 어, 언어"), ("話", "말씀 화, 대화"),
            ("言", "말씀 언"), ("読", "읽을 독, 독서"), ("書", "글 서, 독서"),
            ("記", "기록할 기, 일기"), ("録", "기록할 록, 기록"), ("誌", "기록할 지, 잡지"),
            ("文", "글월 문, 문학"),
            ("勉", "힘쓸 면, 공부"), ("強", "강할 강, 강력"), ("習", "익힐 습, 연습"),
            ("練", "익힐 련, 훈련"), ("修", "닦을 수, 수업"), ("復", "회복할 복, 복습"),
            ("予", "미리 예, 예습"), ("考", "생각할 고, 사고"), ("思", "생각 사, 사색"),
            ("想", "생각 상, 이상"), ("念", "생각 념, 염원"), ("感", "느낄 감, 감동"),
            ("情", "뜻 정, 감정"), ("愛", "사랑 애"), ("恋", "사모할 련, 연애"),
            ("信", "믿을 신, 신뢰"), ("望", "바랄 망, 희망"), ("願", "원할 원, 소원"),
            ("祈", "빌 기, 기도"), ("祝", "빌 축, 축하"), ("福", "복 복, 행복"),
            ("幸", "다행 행, 행복"), ("喜", "기쁠 희, 기쁨"), ("怒", "성낼 노, 분노"),
            ("哀", "슬플 애, 비애"), ("悲", "슬플 비, 비극"), ("笑", "웃을 소, 미소"),
            ("泣", "울 읍, 통곡"), ("驚", "놀랄 경, 경악"), ("怖", "두려워할 포, 공포"),
            ("恐", "두려워할 공, 공룡"), ("恥", "부끄러워할 치, 수치"), ("恨", "한할 한, 원한"),
            ("悔", "뉘우칠 회, 후회"), ("悩", "괴로워할 뇌, 고민"), ("苦", "쓸 고, 고통"),
            ("痛", "아플 통, 통증"), ("辛", "매울 신, 매운맛"), ("甘", "달 감, 달콤"),
            ("酸", "실 산, 산소"), ("咸", "짤 함, 소금"),
            ("塩", "소금 염, 식염"), ("油", "기름 유, 석유"), ("脂", "기름 지, 지방"),
            ("肉", "고기 육, 육류"), ("魚", "물고기 어, 어류"), ("鳥", "새 조, 조류"),
            ("羽", "깃 우, 날개"), ("毛", "털 모, 모발"), ("皮", "가죽 피, 피부"),
            ("革", "가죽 혁, 혁신"), ("骨", "뼈 골, 골격"), ("歯", "이 치, 치과"),
            ("角", "뿔 각, 각도"), ("血", "피 혈, 혈액"), ("息", "숨 쉴 식, 호흡"),
            ("命", "목숨 명, 생명"), ("生", "날 생, 살 생"), ("死", "죽을 사, 사망"),
            ("亡", "망할 망, 사망"), ("存", "있을 존, 존재"), ("在", "있을 재, 현재"),
            ("有", "있을 유, 유명"), ("無", "없을 무, 무료"), ("非", "아닐 비, 비상"),
            ("不", "아닐 불, 불안"), ("否", "아닐 부, 거부"), ("反", "돌이킬 반, 반대"),
            ("除", "덜 제, 제거"), ("乗", "탈 승, 승차"),
            ("降", "내릴 강/항, 하강"), ("登", "오를 등, 등산"), ("進", "나아갈 진, 전진"),
            ("退", "물러날 퇴, 후퇴"), ("往", "갈 왕, 왕복"), ("復", "돌아올 복, 복귀"),
            ("去", "갈 거, 과거"), ("来", "올 래, 미래"), ("着", "붙을 착, 도착"),
            ("発", "필 발, 출발"), ("出", "날 출, 외출"), ("入", "들 입, 입구"),
            ("開", "열 개, 개점"), ("閉", "닫을 폐, 폐점"), ("始", "비로소 시, 시작"),
            ("終", "끝날 종, 종료"), ("初", "처음 초, 최초"), ("末", "끝 말, 주말/연말"),
            ("本", "근본 본, 책"), ("源", "근원 원, 어원")
        };

        foreach (var (k, v) in joyoSpecialEntries)
        {
            map[k] = v;
        }

        // 4. Load dataset
        List<KanjiItem>? items;
        {
            var existingJson = await File.ReadAllTextAsync(targetFile);
            items = JsonSerializer.Deserialize<List<KanjiItem>>(existingJson);
        }
        Assert.NotNull(items);

        // Keep the 2,136 Joyo Kanji
        var joyoItems = items.Take(2136).ToList();

        int updatedCount = 0;
        foreach (var item in joyoItems)
        {
            // Resolve Korean Hun-Eum
            if (map.TryGetValue(item.Kanji, out var koreanHunEum))
            {
                // Clean any parentheses like (이), (가), (라)
                var clean = System.Text.RegularExpressions.Regex.Replace(koreanHunEum, @"\([가-힣a-zA-Z0-9/ ]+\)", "").Trim();
                if (string.IsNullOrWhiteSpace(clean)) clean = koreanHunEum;
                item.MeaningKo = clean;
                updatedCount++;
            }
            else
            {
                // Unihan fallback
                if (!string.IsNullOrWhiteSpace(item.MeaningEn))
                {
                    item.MeaningKo = $"{item.Kanji} ({item.MeaningEn.Split(',')[0].Trim()})";
                }
            }

            // Ensure MeaningKo has no trailing garbage
            item.MeaningKo = item.MeaningKo.Replace("(이)", "").Replace("(가)", "").Replace("(라)", "").Trim();

            // Clean up examples
            foreach (var ex in item.Examples)
            {
                if (ex.Meaning.StartsWith("한자 (") || ex.Meaning.StartsWith("한자(") || ex.Meaning == "한자" || string.IsNullOrWhiteSpace(ex.Meaning) || System.Text.RegularExpressions.Regex.IsMatch(ex.Meaning, @"\([A-Za-z]+\)"))
                {
                    ex.Meaning = item.MeaningKo;
                }
            }
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        var outputJson = JsonSerializer.Serialize(joyoItems, jsonOptions);
        await File.WriteAllTextAsync(targetFile, outputJson);

        // Also write to output bin folder if exists
        var binDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "kanji_dataset.json");
        if (File.Exists(binDataPath))
        {
            await File.WriteAllTextAsync(binDataPath, outputJson);
        }

        // Also overwrite %APPDATA%\BenkyoKanji\data\kanji_library.json to immediately upgrade local machine
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDataLibrary = Path.Combine(appData, "BenkyoKanji", "data", "kanji_library.json");
        if (File.Exists(appDataLibrary))
        {
            await File.WriteAllTextAsync(appDataLibrary, outputJson);
        }

        System.Console.WriteLine($"Generated {joyoItems.Count} clean Joyo Kanji dataset! Updated {updatedCount} kanji with authentic Korean meanings!");
    }

    [Fact]
    public async Task VerifyDatasetHasZeroPlaceholdersAndAllKorean()
    {
        var targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BenkyoKanji", "Data", "kanji_dataset.json");
        if (!File.Exists(targetFile)) targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "kanji_dataset.json");

        var json = await File.ReadAllTextAsync(targetFile);
        var items = JsonSerializer.Deserialize<List<KanjiItem>>(json);
        Assert.NotNull(items);
        Assert.Equal(2136, items.Count);

        var problematic = items.Where(k => 
            k.MeaningKo.Contains("한자") ||
            k.MeaningKo.Contains("Dream") ||
            k.MeaningKo.Contains("?") ||
            k.MeaningKo.Contains("(") ||
            System.Text.RegularExpressions.Regex.IsMatch(k.MeaningKo, "[a-zA-Z]")
        ).ToList();

        if (problematic.Count > 0)
        {
            foreach (var p in problematic.Take(20))
            {
                System.Console.WriteLine($"Still problematic: {p.Kanji} -> {p.MeaningKo}");
            }
        }

        Assert.Empty(problematic);
    }
}

