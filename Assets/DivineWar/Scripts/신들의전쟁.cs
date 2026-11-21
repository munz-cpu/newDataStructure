using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

[RequireComponent(typeof(UIDocument))]
public sealed class 신들의전쟁 : MonoBehaviour
{
    [SerializeField] Camera 전장카메라;
    [SerializeField] AudioClip 이동클릭효과음;
    [SerializeField] AudioClip 왕공격효과음;
    [SerializeField] AudioClip 피격효과음;
    [SerializeField] AudioClip 나무채집효과음, 돌채집효과음;
    [SerializeField] AudioClip[] 왕발소리, 왕흙발소리;
    [SerializeField] SpriteRenderer[] 슬롯;
    [SerializeField] Sprite[] 왕대기프레임, 왕이동프레임, 왕공격프레임;
    [SerializeField] Sprite[] 주민방향프레임, 주민동작프레임;
    [SerializeField] Sprite[] 병사동작프레임;
    [SerializeField] SpriteRenderer[] 짐승슬롯;
    [SerializeField] 짐승그림[] 짐승그림들;
    [SerializeField] SpriteRenderer[] 모닥불슬롯;
    [SerializeField] Sprite 나무울타리가로, 나무울타리세로, 돌울타리가로, 돌울타리세로;
    [SerializeField] SpriteRenderer 건설미리보기;
    [SerializeField] 자원지점[] 자원들;
    [SerializeField] Transform 장식지형;
    [SerializeField] Tilemap 바닥타일맵, 장애물타일맵, 지형타일맵;
    [SerializeField] TileBase[] 해변타일;
    [SerializeField] TileBase[] 흙타일;
    [SerializeField] 전장지형.구역[] 전술지형;
    [SerializeField] Transform[] 시작지점들;

    [SerializeField] GraphicRaycaster 전장HUD;
    [SerializeField] GameObject 전투, 입장대기, 건축목록;
    [SerializeField] RectTransform 미니맵영역, 건설커서;
    [SerializeField] 메뉴버튼 건축버튼;
    [SerializeField] 메뉴버튼[] 행동버튼;
    [SerializeField] Image[] 건축미리보기;
    [SerializeField] CanvasGroup[] 스킬표시;
    [SerializeField] 스킬쿨다운테두리[] 스킬테두리;
    [SerializeField] Sprite 스킬설명프레임;
    [SerializeField] Sprite 주민패널그림, 주민패널구분선, 주민일반버튼, 주민강조버튼;
    [SerializeField] TMP_Text 자원표시, 병력표시, 시계, 내회관, 적회관, 왕상태, 임무, 알림, 도움, 돌진키표시;

    const string 이동방식설정키 = "DivineWar.KeyboardMovement";
    const string 주민자동해제설정키 = "DivineWar.DeselectWorkersAfterOrder";
    const float 명령표시길이 = .55f;
    const float 채집등장시간 = .32f, 채집유지시간 = .42f, 채집퇴장시간 = .4f;
    const float 왕채집키거리 = 2.4f;
    const float 클릭여유 = .18f;
    static readonly string[] 채집아이콘이름 = { "wood", "stone", "copper", "iron", "mineral" };
    static readonly Color 청록 = new(.39f, .85f, .82f);
    static readonly Color 적색 = new(1, .49f, .45f);
    readonly HashSet<int> 선택부대 = new();
    readonly List<Vector2> 내왕경로 = new();
    readonly List<RaycastResult> HUD충돌 = new();
    채집그림상태[] 채집그림들;
    유닛상태창 상태창;
    int 설명중인스킬 = -1;
    Material 하단안내재질;
    readonly 왕그림상태[] 왕그림들 = new 왕그림상태[2];
    readonly 주민그림상태[] 주민그림들 = new 주민그림상태[전장규칙.전체칸];
    readonly 병사그림상태[] 병사그림들 = new 병사그림상태[전장규칙.전체칸];
    readonly float[] 대상이전체력 = new float[전장규칙.전체칸];
    readonly int[] 짐승등장번호 = new int[전장짐승.전체칸];
    PointerEventData HUD포인터;
    Rect 미니맵범위;
    VisualElement 뿌리, 전장표시, 지도;
    격자길찾기 길찾기;
    전장지형 지형;
    전투메뉴 메뉴;
    주민추첨패널 주민패널;
    회관체력표시 회관체력;
    AudioSource 효과음재생기;
    SpriteRenderer 호버윤곽, 왕목표윤곽;
    SpriteRenderer[] 유닛윤곽;
    Material 호버재질, 은신재질, 유닛선택재질, 왕목표재질;
    SpriteRenderer[] 자원그림;
    Sprite[] 채집아이콘들;
    TMP_Text[] 재료수량글;
    int[] 표시한재료수량;
    전장안개 안개;
    전장그림자 그림자들;
    전장식량창 식량창;
    Sprite[] 고기그림들;

    전장규칙 규칙;
    전장상태 상태;
    참가연결 연결;
    int 내팀, 배치 = -1;
    int 주민스탯번호 = -1;
    int? 지도포인터;
    float 전송시간, 손님대기, 시뮬누적;
    Vector2 누른위치, 현재마우스, 지도누른위치, 지도누르기전왕기준시점;
    Vector2 보낸이동방향;
    Vector2 채집전이동방향;
    float 이동방향재전송;
    Vector2 명령표시위치;
    float 명령표시시작 = float.NegativeInfinity;
    bool 지도에서명령, 지도이동클릭대기, 지도시점고정;
    bool 멀티, 전투중, 일시정지, 패널열림, 자동추적, 드래그중, 팝업재개;
    bool 키보드이동, 주민자동해제, 배치세로;
    readonly bool[] 결과확인 = new bool[2];
    bool 복귀중;
    float 결과대기;
    전장입장 입장;
    string 연결종료 = "", 팝업제목, 팝업본문;

    [Serializable]
    struct 짐승그림
    {
        public Sprite[] 방향프레임;
    }

    struct 왕그림상태
    {
        public bool 표시중, 채집중;
        public Vector2 위치;
        public int 방향;
        public float 이동여운, 이전공격대기, 공격여운, 동작시간;
        public Sprite[] 프레임;
    }

    struct 주민그림상태
    {
        public bool 표시중;
        public int 방향;
        public float 이동여운, 걷기거리, 튀는시간, 이전공격대기, 이전체력;
    }

    struct 병사그림상태
    {
        public int 방향;
        public float 대기시간, 걷기거리, 이동여운, 공격여운, 피격여운, 이전공격대기;
    }

    struct 채집그림상태
    {
        public int 횟수;
        public Image 그림;
        public Vector3 출발위치;
        public float 시작;
    }

    void Awake()
    {
        입장 = 전장입장.가져오기();
        연결 = 입장?.연결;
        멀티 = 연결 != null;
        내팀 = 입장?.내팀 ?? 0;
    }

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        지형 ??= new 전장지형(바닥타일맵, 장애물타일맵, 지형타일맵, 해변타일, 흙타일, 자원들, 전술지형, !전장입장.서버빌드);
        길찾기 ??= new 격자길찾기(바닥타일맵, 장애물타일맵, 지형);
        if (전장입장.서버빌드)
        {
            GetComponent<UIDocument>().enabled = false;
            입장대기.SetActive(false);
            전투.SetActive(false);
            return;
        }
        뿌리 = GetComponent<UIDocument>().rootVisualElement;
        뿌리.StretchToParentSize();
        전장표시 = 뿌리.Q("draw");
        지도 = 뿌리.Q("minimap");
        고기그림들 ??= Array.ConvertAll(전장식량.아이콘이름, 이름 => Resources.Load<Sprite>("Food/" + 이름));
        식량창 = new 전장식량창(뿌리, 도움.font.sourceFontFile, 고기그림들,
            종류 => 명령전달(명령종류.고기굽기, 값: 종류),
            종류 => 명령전달(명령종류.고기사용, 값: 종류));
        미니맵범위 = default;
        안개 ??= new 전장안개(transform, 길찾기.전장범위, 지형);
        안개.보이기 = true;
        if (그림자들 != null) 그림자들.보이기 = true;
        지도.style.backgroundColor = new StyleColor(new Color32(52, 78, 68, 255));
        지도.style.backgroundImage = new StyleBackground(안개.전장지도);
        건축목록표시(false);
        if (주민패널?.유효 != true)
            주민패널 = new 주민추첨패널((RectTransform)전투.transform, 도움.font, 주민추첨요청,
                주민패널그림, 스킬설명프레임, 주민패널구분선, 주민일반버튼, 주민강조버튼);
        foreach (var 테두리 in 스킬테두리)
            테두리.문구준비(도움.font);
        키보드이동 = PlayerPrefs.GetInt(이동방식설정키) == 1;
        주민자동해제 = PlayerPrefs.GetInt(주민자동해제설정키, 1) == 1;
        이동방식갱신();
        전장표시.generateVisualContent += 전장그리기;
        전장표시.RegisterCallback<PointerDownEvent>(전장누르기);
        전장표시.RegisterCallback<PointerUpEvent>(전장떼기);
        전장표시.RegisterCallback<PointerCaptureOutEvent>(전장캡처해제);
        지도.generateVisualContent += 지도그리기;
        지도.RegisterCallback<PointerDownEvent>(지도누르기);
        지도.RegisterCallback<PointerMoveEvent>(지도끌기);
        지도.RegisterCallback<PointerUpEvent>(지도떼기);
        지도.RegisterCallback<PointerCancelEvent>(지도취소);
        지도.RegisterCallback<PointerCaptureOutEvent>(지도캡처해제);
        입장대기.SetActive(true);
        전투.SetActive(false);
        표시(지도, false);
        표시(전장표시, false);
    }

    void Start()
    {
        if (전장입장.서버빌드)
        {
            if (연결?.전용서버 != true) { 타이틀이동(); return; }
            규칙준비();
            Debug.Log("[DivineWar] MATCH_STARTED");
            return;
        }
        if (이동클릭효과음 != null || 왕공격효과음 != null || 피격효과음 != null || 나무채집효과음 != null || 돌채집효과음 != null
            || 왕발소리?.Length > 0 || 왕흙발소리?.Length > 0)
        {
            효과음재생기 = gameObject.AddComponent<AudioSource>();
            효과음재생기.playOnAwake = false;
            효과음재생기.spatialBlend = 0f;
            if (이동클릭효과음 != null) 이동클릭효과음.LoadAudioData();
            if (왕공격효과음 != null) 왕공격효과음.LoadAudioData();
            if (피격효과음 != null) 피격효과음.LoadAudioData();
            if (나무채집효과음 != null) 나무채집효과음.LoadAudioData();
            if (돌채집효과음 != null) 돌채집효과음.LoadAudioData();
            발소리미리읽기(왕발소리);
            발소리미리읽기(왕흙발소리);
        }
        울타리슬롯준비();
        if (짐승슬롯 != null)
            foreach (var 그림 in 짐승슬롯)
                if (그림 != null) 그림.enabled = false;
        자원그림 = Array.ConvertAll(자원들, 지점 => 지점.오브젝트.GetComponent<SpriteRenderer>());
        채집아이콘들 = Array.ConvertAll(채집아이콘이름, 이름 => Resources.Load<Sprite>("HarvestIcons/" + 이름));
        재료표시준비();
        그림자들 = new 전장그림자(transform, 슬롯, 자원그림, 장식지형);
        var 셰이더 = Resources.Load<Shader>("전장 호버 윤곽");
        if (셰이더 != null)
        {
            호버재질 = new Material(셰이더);
            호버윤곽 = 윤곽생성("호버 윤곽", 호버재질);
            은신재질 = new Material(셰이더);
            은신재질.SetFloat("_OutlineWidth", 2f);
            은신재질.SetFloat("_FillOpacity", .38f);
            왕목표재질 = new Material(셰이더);
            왕목표재질.SetColor("_OutlineColor", new Color32(77, 255, 225, 255));
            왕목표윤곽 = 윤곽생성("왕 목표 윤곽", 왕목표재질);
            유닛선택재질 = new Material(셰이더);
            유닛선택재질.SetColor("_OutlineColor", Color.red);
            유닛윤곽 = new SpriteRenderer[슬롯.Length];
        }
        SceneManager.sceneLoaded += 씬준비;
        var 메뉴씬 = SceneManager.GetSceneByName("BattleMenu");
        if (메뉴씬.isLoaded) 씬준비(메뉴씬, LoadSceneMode.Additive);
        else SceneManager.LoadSceneAsync("BattleMenu", LoadSceneMode.Additive);

        if (멀티) return;
        규칙준비();
        화면전환();
    }

    void 규칙준비()
    {
        var 위치들 = Array.ConvertAll(시작지점들, 지점 => (Vector2)지점.position);
        규칙 = new 전장규칙(자원들, 길찾기, 위치들, 입장?.청록직업 ?? 왕직업.수호자,
            입장?.적색직업 ?? 왕직업.추적자, UnityEngine.Random.Range(0, int.MaxValue));
        상태 = 규칙.상태;
    }

    void 울타리슬롯준비()
    {
        Array.Resize(ref 슬롯, 전장규칙.전체칸);
        var 원본 = 슬롯[0];
        var 부모 = new GameObject("울타리").transform;
        부모.SetParent(원본.transform.parent, false);
        for (int i = 전장규칙.울타리시작칸; i < 전장규칙.모닥불시작칸; i++)
        {
            var 오브젝트 = new GameObject($"울타리 {i - 전장규칙.울타리시작칸 + 1}");
            오브젝트.layer = 원본.gameObject.layer;
            오브젝트.transform.SetParent(부모, false);
            var 그림 = 오브젝트.AddComponent<SpriteRenderer>();
            그림.sharedMaterial = 원본.sharedMaterial;
            그림.sortingLayerID = 원본.sortingLayerID;
            그림.enabled = false;
            슬롯[i] = 그림;
        }
        for (int 팀 = 0; 팀 < 2; 팀++)
        {
            var 그림 = 모닥불슬롯[팀];
            그림.enabled = false;
            슬롯[전장규칙.모닥불번호(팀)] = 그림;
        }
    }

    void 씬준비(Scene 씬, LoadSceneMode 방식)
    {
        if (씬.name != "BattleMenu") return;
        foreach (var 오브젝트 in 씬.GetRootGameObjects())
        {
            if (!오브젝트.TryGetComponent(out 전투메뉴 전환메뉴)) continue;
            메뉴 = 전환메뉴;
            메뉴.재개 += 메뉴전환;
            메뉴.나가기 += 타이틀이동;
            메뉴.이동방식선택 += 이동방식변경;
            메뉴.주민자동해제전환 += 주민자동해제변경;
            메뉴.이동방식표시(키보드이동);
            메뉴.주민자동해제표시(주민자동해제);
            if (패널열림) 메뉴.보이기(팝업제목, 팝업본문, 팝업재개);
            else 메뉴.숨기기();
            break;
        }
    }

    void OnDisable()
    {
        식량창?.Dispose();
        식량창 = null;
        주민패널?.닫기();
        Array.Clear(대상이전체력, 0, 대상이전체력.Length);
        if (채집그림들 != null)
        {
            foreach (var 효과 in 채집그림들)
                if (효과.그림 != null) Destroy(효과.그림.gameObject);
            채집그림들 = null;
        }
        채집전이동방향 = Vector2.zero;
        주민스탯번호 = -1;
        설명중인스킬 = -1;
        상태창?.숨기기();
        if (안개 != null) 안개.보이기 = false;
        if (그림자들 != null) 그림자들.보이기 = false;
        if (건설커서 != null) 건설커서.gameObject.SetActive(false);
        if (전장표시 != null)
        {
            전장표시.generateVisualContent -= 전장그리기;
            전장표시.UnregisterCallback<PointerDownEvent>(전장누르기);
            전장표시.UnregisterCallback<PointerUpEvent>(전장떼기);
            전장표시.UnregisterCallback<PointerCaptureOutEvent>(전장캡처해제);
        }
        if (지도 != null)
        {
            지도누름해제();
            지도.generateVisualContent -= 지도그리기;
            지도.UnregisterCallback<PointerDownEvent>(지도누르기);
            지도.UnregisterCallback<PointerMoveEvent>(지도끌기);
            지도.UnregisterCallback<PointerUpEvent>(지도떼기);
            지도.UnregisterCallback<PointerCancelEvent>(지도취소);
            지도.UnregisterCallback<PointerCaptureOutEvent>(지도캡처해제);
        }
        if (호버윤곽 != null) 호버윤곽.enabled = false;
        if (왕목표윤곽 != null) 왕목표윤곽.enabled = false;
        if (유닛윤곽 != null)
            foreach (var 윤곽 in 유닛윤곽)
                if (윤곽 != null) 윤곽.enabled = false;
        SceneManager.sceneLoaded -= 씬준비;
        if (메뉴 != null)
        {
            메뉴.숨기기();
            메뉴.재개 -= 메뉴전환;
            메뉴.나가기 -= 타이틀이동;
            메뉴.이동방식선택 -= 이동방식변경;
            메뉴.주민자동해제전환 -= 주민자동해제변경;
            메뉴 = null;
        }
        연결?.Dispose();
        연결 = null;
    }

    void OnApplicationFocus(bool 집중)
    {
        if (!집중) 지도누름해제();
    }

    void OnDestroy()
    {
        if (하단안내재질 != null) Destroy(하단안내재질);
        if (상태창?.오브젝트 != null) Destroy(상태창.오브젝트);
        안개?.Dispose();
        지형?.Dispose();
        그림자들?.Dispose();
        if (호버윤곽 != null) Destroy(호버윤곽.gameObject);
        if (호버재질 != null) Destroy(호버재질);
        if (은신재질 != null) Destroy(은신재질);
        if (왕목표윤곽 != null) Destroy(왕목표윤곽.gameObject);
        if (왕목표재질 != null) Destroy(왕목표재질);
        if (유닛윤곽 != null)
            foreach (var 윤곽 in 유닛윤곽)
                if (윤곽 != null) Destroy(윤곽.gameObject);
        if (유닛선택재질 != null) Destroy(유닛선택재질);
    }

    static void 표시(VisualElement 요소, bool 켜짐) => 요소.style.display = 켜짐 ? DisplayStyle.Flex : DisplayStyle.None;
    void 타이틀이동()
    {
        if (복귀중) return;
        복귀중 = true;
        SceneManager.LoadScene("Title");
    }

    void 화면전환()
    {
        회관체력 ??= new 회관체력표시((RectTransform)전투.transform, 스킬설명프레임,
            내회관, 적회관, 시계, 내팀 == 0 ? 청록 : 적색, 내팀 == 0 ? 적색 : 청록);
        전투중 = true;
        입장대기.SetActive(false);
        전투.SetActive(true);
        if (하단안내재질 == null)
        {
            도움.outlineWidth = .2f;
            하단안내재질 = 도움.fontMaterial;
        }
        표시(전장표시, true);
        표시(지도, true);
        for (int i = 0; i < 2; i++)
            건축미리보기[i].sprite = 건물그림(i);
        전장카메라.orthographicSize = 7.8f;
        시점복귀();
        전장카메라.transform.position = 카메라한계(내왕().위치);
        탐사갱신();
        정보갱신();
        채집그림갱신();
        장면반영(1);
    }

    void 서버갱신(float 초)
    {
        if (복귀중) return;
        if (연결?.오류 != null || 연결 == null)
        {
            Debug.Log("[DivineWar] MATCH_INTERRUPTED");
            타이틀이동();
            return;
        }
        for (int i = 0; i < 96 && 연결.받기(out int 팀, out string 문장); i++)
        {
            if (팀 is < 0 or > 1) continue;
            if (문장 == "F" && 상태.승자 >= 0) 결과확인[팀] = true;
            else if (문장.StartsWith("C", StringComparison.Ordinal))
            {
                try { 규칙.명령(팀, JsonUtility.FromJson<전장명령>(문장.Substring(1))); }
                catch (ArgumentException) { }
            }
        }
        bool 진행중 = 상태.승자 < 0;
        if (진행중) 규칙진행(초, false);
        if (상태.승자 >= 0 && (결과확인[0] && 결과확인[1] || (결과대기 += 초) > 15f))
        {
            Debug.Log($"[DivineWar] MATCH_FINISHED winner={상태.승자}");
            타이틀이동();
            return;
        }
        전송시간 -= 초;
        if (연결.상태전송가능 && (전송시간 <= 0 || 진행중 && 상태.승자 >= 0))
        {
            상태.청록왕경로 ??= new List<Vector2>();
            상태.적색왕경로 ??= new List<Vector2>();
            규칙.왕이동경로복사(0, 상태.청록왕경로);
            규칙.왕이동경로복사(1, 상태.적색왕경로);
            연결.상태보내기("S" + JsonUtility.ToJson(상태));
            전송시간 = .083f;
        }
    }

    void 규칙진행(float 초, bool 연습전)
    {
        시뮬누적 += 초;
        while (시뮬누적 >= .04f)
        {
            규칙.진행(.04f, 연습전);
            시뮬누적 -= .04f;
        }
    }

    void 접속갱신(float 초)
    {
        if (연결 == null) return;
        string 마지막상태 = null;
        while (연결.받기(out int 팀, out string 문장))
            if (팀 == -1 && 문장.StartsWith("S", StringComparison.Ordinal)) 마지막상태 = 문장;
        if (마지막상태 != null)
        {
            try
            {
                var 받은상태 = JsonUtility.FromJson<전장상태>(마지막상태.Substring(1));
                if (받은상태?.대상들?.Length == 전장규칙.전체칸 && 받은상태.진영들?.Length == 2)
                {
                    상태 = 받은상태;
                    if (상태.승자 >= 0 && !결과확인[내팀]) 결과확인[내팀] = 연결.보내기("F");
                    if (!전투중) 화면전환();
                }
            }
            catch (ArgumentException) { }
        }
        if (상태?.승자 >= 0 || 연결종료.Length > 0) return;
        if (연결.오류 != null) 연결종료 = 연결.오류;
        else if (!전투중 && (손님대기 += 초) > 12) 연결종료 = "서버의 전장 정보를 받지 못했습니다.";
        if (연결종료.Length > 0)
        {
            연결.Dispose();
            연결 = null;
            팝업표시("경기 중단", 연결종료, false);
        }
    }

    bool 명령전달(명령종류 종류, Vector2 위치 = default, int 값 = 0, int[] 부대 = null,
        bool 지도입력 = false, bool 세로 = false, int 주민모집인원 = 0)
    {
        if (!전투중 || 상태.승자 >= 0 || 일시정지 || 연결종료.Length > 0) return false;
        var 내용 = new 전장명령 { 종류 = 종류, 위치 = 위치, 값 = 값, 부대 = 부대, 세로 = 세로, 주민모집인원 = 주민모집인원 };
        if (규칙 != null) 규칙.명령(내팀, 내용);
        else if (연결?.보내기("C" + JsonUtility.ToJson(내용)) != true) return false;

        if (종류 is 명령종류.이동 or 명령종류.공격 or 명령종류.채집 or 명령종류.왕채집 or 명령종류.부대이동 or 명령종류.건설 or 명령종류.사냥 or 명령종류.주민훈련)
        {
            if (이동클릭효과음 != null && 효과음재생기 != null)
                효과음재생기.PlayOneShot(이동클릭효과음);
            명령표시위치 = 위치;
            명령표시시작 = Time.unscaledTime;
            지도에서명령 = 지도입력;
        }

        if (주민자동해제 && 부대 != null && 종류 is 명령종류.부대이동 or 명령종류.채집)
        {
            foreach (int 번호 in 부대)
                if (상태.대상들[번호]?.종류 == 대상종류.주민) 선택부대.Remove(번호);
        }
        return true;
    }

    bool 주민추첨요청(int 단계, int 인원) => 명령전달(명령종류.주민추첨, 값: 단계, 주민모집인원: 인원);

    void 주민패널열기()
    {
        이동방향전송(Vector2.zero, 0);
        지도누름해제();
        건축목록표시(false);
        배치 = -1;
        주민패널.열기(상태, 내팀);
    }

    void Update()
    {
        float 초 = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (전장입장.서버빌드) { 서버갱신(초); return; }
        접속갱신(초);
        if (!전투중) return;

        var 키 = Keyboard.current;
        if (키 != null && 키.escapeKey.wasPressedThisFrame)
        {
            if (주민패널.열림) 주민패널.닫기();
            else if (배치 >= 0 && !패널열림) 배치 = -1;
            else if (건축목록열림) 건축목록표시(false);
            else if (!패널열림 && 식량창.열림) 식량창.닫기();
            else if (!패널열림 && 선택부대.Count > 0) 선택부대.Clear();
            else 메뉴전환();
        }
        이동방향전송(이동방향읽기(키), 초);
        if (규칙 != null && !일시정지 && 상태.승자 < 0) 규칙진행(초, true);
        if (상태.승자 >= 0 && 연결종료.Length == 0)
        {
            string 제목 = 상태.승자 == 내팀 ? "승리" : "패배";
            if (!패널열림 || 팝업제목 != 제목)
                팝업표시(제목, 상태.승자 == 내팀 ? "적 마을회관을 무너뜨렸습니다." : "아군 마을회관이 함락됐습니다.", false);
        }

        탐사갱신();
        if (!패널열림 && !주민패널.열림)
        {
            키입력읽기();
            카메라이동(초);
        }
        장면반영(초);
        int 주민호버번호 = 호버갱신();
        주민스탯입력(키, 주민호버번호);
        정보갱신(주민호버번호);
        주민패널.갱신(상태, 내팀);
        전장표시.MarkDirtyRepaint();
        지도.MarkDirtyRepaint();
    }

    void 탐사갱신()
    {
        float 현재시간 = Time.unscaledTime;
        if (안개.갱신(상태.대상들, 내팀, 현재시간))
            for (int i = 0; i < 자원그림.Length; i++)
                if (자원그림[i] != null) 자원그림[i].forceRenderingOff = !안개.탐사됨(자원들[i].오브젝트.position);
        if (안개.페이드갱신(현재시간) && 지도.panel != null)
            RuntimePanelUtils.SetTextureDirty(지도.panel, 안개.전장지도);
    }

    전장대상 내왕() => 상태.대상들[전장규칙.왕번호(내팀)];

    public void 메뉴전환()
    {
        if (!전투중 || 상태.승자 >= 0 || 연결종료.Length > 0) return;
        if (패널열림)
        {
            패널열림 = 일시정지 = false;
            메뉴?.숨기기();
        }
        else 팝업표시(멀티 ? "게임 메뉴" : "일시정지", 멀티 ? "멀티플레이 경기는 계속 진행됩니다." : "전투가 일시정지됐습니다.", true);
    }

    void 팝업표시(string 제목, string 문장, bool 재개)
    {
        주민패널.닫기();
        이동방향전송(Vector2.zero, 0);
        패널열림 = true;
        일시정지 = !멀티;
        팝업제목 = 제목;
        팝업본문 = 문장;
        팝업재개 = 재개;
        드래그중 = false;
        지도누름해제();
        배치 = -1;
        건축목록표시(false);
        if (호버윤곽 != null) 호버윤곽.enabled = false;
        메뉴?.보이기(제목, 문장, 재개);
    }

    void 이동방식변경(bool 새키보드이동)
    {
        if (키보드이동 == 새키보드이동) return;
        키보드이동 = 새키보드이동;
        PlayerPrefs.SetInt(이동방식설정키, 키보드이동 ? 1 : 0);
        PlayerPrefs.Save();
        이동방식갱신();
    }

    void 이동방식갱신()
    {
        돌진키표시.text = 키보드이동 ? "F" : "W";
        메뉴?.이동방식표시(키보드이동);
    }

    void 주민자동해제변경()
    {
        주민자동해제 = !주민자동해제;
        PlayerPrefs.SetInt(주민자동해제설정키, 주민자동해제 ? 1 : 0);
        PlayerPrefs.Save();
        메뉴.주민자동해제표시(주민자동해제);
    }

    Vector2 이동방향읽기(Keyboard 키)
    {
        if (!키보드이동 || 패널열림 || 주민패널.열림 || !Application.isFocused || 키 == null || !내왕().생존)
            return Vector2.zero;
        return new Vector2((키.dKey.isPressed ? 1 : 0) - (키.aKey.isPressed ? 1 : 0),
            (키.wKey.isPressed ? 1 : 0) - (키.sKey.isPressed ? 1 : 0));
    }

    void 이동방향전송(Vector2 방향, float 초)
    {
        if (채집전이동방향 != Vector2.zero)
        {
            if (방향 == 채집전이동방향) return;
            채집전이동방향 = Vector2.zero;
        }
        if (방향 == 보낸이동방향 && (방향 == Vector2.zero || (이동방향재전송 -= 초) > 0)) return;
        명령전달(명령종류.방향이동, 방향);
        보낸이동방향 = 방향;
        이동방향재전송 = .12f;
    }

    public void 행동고르기(int 번호)
    {
        if (!전투중 || 패널열림 || 주민패널.열림) return;
        if (전장규칙.건설종류인가(번호))
        {
            배치 = 배치 == 번호 ? -1 : 번호;
            배치세로 = false;
        }
        else if (번호 == 2) 주민패널열기();
        else if (번호 == 3)
        {
            int 훈련소 = -1;
            float 거리 = float.PositiveInfinity;
            for (int i = 전장규칙.왕번호(내팀) + 4; i <= 전장규칙.왕번호(내팀) + 5; i++)
                if (상태.대상들[i] is { 생존: true, 완공대기: <= 0 } 건물)
                {
                    float 후보 = (건물.위치 - 내왕().위치).sqrMagnitude;
                    if (후보 < 거리) { 거리 = 후보; 훈련소 = i; }
                }
            if (훈련소 >= 0) 훈련소사용(훈련소);
            else 명령전달(명령종류.모집);
        }
    }

    bool 건축목록열림 => 건축목록.activeSelf;

    public void 건축목록전환()
    {
        if (!전투중 || 패널열림) return;
        bool 열기 = !건축목록열림;
        if (!열기) 배치 = -1;
        건축목록표시(열기);
    }

    void 건축목록표시(bool 열기)
    {
        건축목록.SetActive(열기);
        건축버튼.선택표시(열기);
    }

    Sprite 건물그림(int 종류) => 종류 switch
    {
        전장규칙.나무울타리건설 => 울타리그림(대상종류.나무울타리, 배치세로),
        전장규칙.돌울타리건설 => 울타리그림(대상종류.돌울타리, 배치세로),
        전장규칙.모닥불건설 => 슬롯[전장규칙.모닥불번호(내팀)].sprite,
        _ => 슬롯[전장규칙.왕번호(내팀) + 2 + 종류 * 2].sprite
    };

    Sprite 울타리그림(대상종류 종류, bool 세로) => 종류 == 대상종류.나무울타리
        ? (세로 ? 나무울타리세로 : 나무울타리가로)
        : (세로 ? 돌울타리세로 : 돌울타리가로);

    Vector2 전장좌표(Vector2 화면) =>
        전장카메라.ScreenToWorldPoint(new Vector3(화면.x, 화면.y, 10));

    void 키입력읽기()
    {
        var 마우스 = Mouse.current;
        var 키 = Keyboard.current;
        if (마우스 == null || !Application.isFocused) return;
        현재마우스 = 마우스.position.ReadValue();
        Vector2 위치 = 전장좌표(현재마우스);
        if (마우스.rightButton.wasPressedThisFrame && 전장가리킴(현재마우스)) 우클릭명령(위치);
        if (키 != null)
        {
            if (키.spaceKey.wasPressedThisFrame) 시점복귀();
            if (키.digit1Key.wasPressedThisFrame) 행동고르기(0);
            if (키.digit2Key.wasPressedThisFrame) 행동고르기(1);
            if (키.digit3Key.wasPressedThisFrame) 행동고르기(2);
            if (키.digit4Key.wasPressedThisFrame) 행동고르기(3);
            if (키.digit5Key.wasPressedThisFrame) 행동고르기(전장규칙.나무울타리건설);
            if (키.digit6Key.wasPressedThisFrame) 행동고르기(전장규칙.돌울타리건설);
            if (키.digit7Key.wasPressedThisFrame) 행동고르기(전장규칙.모닥불건설);
            if (키.iKey.wasPressedThisFrame) 식량창.전환();
            if (키.vKey.wasPressedThisFrame && 전장규칙.울타리건설인가(배치)) 배치세로 = !배치세로;
            if (키.qKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 0);
            if ((키보드이동 ? 키.fKey : 키.wKey).wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 1);
            if (키.eKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 2);
            if (키.rKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 3);
        }
    }

    void 주민스탯입력(Keyboard 키, int 주민호버번호)
    {
        if (주민스탯번호 != 주민호버번호) 주민스탯번호 = -1;
        if (키 == null || !키.gKey.wasPressedThisFrame || 패널열림 || 주민패널.열림
            || 배치 >= 0 || !Application.isFocused || Mouse.current == null) return;

        if (주민호버번호 >= 0) 주민스탯번호 = 주민호버번호;
        else 가까운자원왕채집();
    }

    Vector2 화면좌표(Vector2 패널좌표)
    {
        var 범위 = 전장표시.contentRect;
        return new Vector2((패널좌표.x - 범위.x) * Screen.width / 범위.width,
            (범위.yMax - 패널좌표.y) * Screen.height / 범위.height);
    }

    void 전장누르기(PointerDownEvent 입력)
    {
        if (!전투중 || 패널열림 || 주민패널.열림 || 입력.pointerType != UnityEngine.UIElements.PointerType.mouse) return;
        현재마우스 = 화면좌표(입력.localPosition);
        if (입력.button == 0)
        {
            드래그중 = true;
            누른위치 = 현재마우스;
            전장표시.CapturePointer(입력.pointerId);
        }
    }

    void 우클릭명령(Vector2 위치, bool 지도입력 = false)
    {
        if (배치 >= 0)
        {
            배치 = -1;
            return;
        }
        if (!지도입력 && (상호작용(0) || 상호작용(클릭여유))) return;
        if (!키보드이동) 명령전달(명령종류.이동, 위치, 지도입력: 지도입력);

        bool 상호작용(float 여유) => 사냥명령(짐승가리킴(위치, 여유, 사냥대상만: true))
            || 훈련소사용(클릭대상(위치, 여유))
            || 모닥불사용(클릭대상(위치, 여유)) || 왕채집명령(채집대상(위치, 여유));
    }

    bool 왕채집명령(int 번호)
    {
        if (번호 < 0 || !내왕().생존) return false;
        if (!명령전달(명령종류.왕채집, 자원들[번호].오브젝트.position, 번호)) return false;
        if (보낸이동방향 != Vector2.zero) 채집전이동방향 = 보낸이동방향;
        보낸이동방향 = Vector2.zero;
        return true;
    }

    void 가까운자원왕채집()
    {
        Vector2 왕위치 = 내왕().위치;
        int 가장가까움 = -1;
        float 거리제곱 = 왕채집키거리 * 왕채집키거리;
        for (int i = 0; i < 자원들.Length; i++)
        {
            Vector2 위치 = 자원들[i].오브젝트.position;
            float 후보 = (왕위치 - 위치).sqrMagnitude;
            if (후보 >= 거리제곱 || !안개.탐사됨(위치)) continue;
            거리제곱 = 후보;
            가장가까움 = i;
        }
        왕채집명령(가장가까움);
    }

    void 전장떼기(PointerUpEvent 입력)
    {
        if (입력.button != 0 || !전장표시.HasPointerCapture(입력.pointerId)) return;
        bool 완료 = 드래그중 && 전투중 && !패널열림;
        드래그중 = false;
        전장표시.ReleasePointer(입력.pointerId);
        if (!완료) return;
        현재마우스 = 화면좌표(입력.localPosition);
        if (Vector2.Distance(누른위치, 현재마우스) > 9 && 배치 < 0)
        {
            범위선택(사각형(누른위치, 현재마우스));
            return;
        }
        if (!전장표시.contentRect.Contains(입력.localPosition)) return;
        if (!전장가리킴(현재마우스)) return;
        Vector2 위치 = 전장좌표(현재마우스);
        if (배치 >= 0)
        {
            if (!안개.탐사됨(바닥타일맵.GetCellCenterWorld(바닥타일맵.WorldToCell(위치)))) return;
            명령전달(명령종류.건설, 위치, 배치, 세로: 배치세로);
            if (!전장규칙.울타리건설인가(배치)) 배치 = -1;
            return;
        }
        클릭(위치);
    }

    void 전장캡처해제(PointerCaptureOutEvent 입력) => 드래그중 = false;

    void 범위선택(Rect 범위)
    {
        선택부대.Clear();
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || 대상.팀 != 내팀 || 대상.건물 || 대상.종류 == 대상종류.왕) continue;
            if (범위.Contains(전장카메라.WorldToScreenPoint(대상.위치 + Vector2.up * .35f))) 선택부대.Add(i);
        }
    }

    void 클릭(Vector2 위치)
    {
        var (짐승번호, 대상번호, 자원) = 전장클릭대상(위치, 사냥대상만: true);
        if (사냥명령(짐승번호)) return;
        if (대상번호 >= 0)
        {
            var 대상 = 상태.대상들[대상번호];
            if (모닥불사용(대상번호)) return;
            if (대상.팀 != 내팀)
            {
                int[] 부대 = null;
                if (선택부대.Count > 0)
                {
                    부대 = new int[선택부대.Count];
                    선택부대.CopyTo(부대);
                }
                명령전달(명령종류.공격, 위치, 대상번호, 부대);
                return;
            }
            if (훈련소사용(대상번호)) return;
            if (대상.종류 == 대상종류.회관)
            {
                주민패널열기();
                return;
            }
            if (대상.종류 == 대상종류.왕)
            {
                선택부대.Clear();
                return;
            }
            if (대상.종류 is 대상종류.병사 or 대상종류.주민)
            {
                선택부대.Clear();
                선택부대.Add(대상번호);
                return;
            }
        }
        if (선택부대.Count == 0)
        {
            if (대상번호 >= 0) return;
            if (!왕채집명령(자원) && !키보드이동) 명령전달(명령종류.이동, 위치);
            return;
        }

        선택부대명령(위치, 자원);
    }

    (int 짐승, int 대상, int 자원) 전장클릭대상(Vector2 위치, bool 사냥대상만 = false)
    {
        var 결과 = 찾기(0);
        return 결과 == (-1, -1, -1) ? 찾기(클릭여유) : 결과;

        (int 짐승, int 대상, int 자원) 찾기(float 여유)
        {
            return (짐승가리킴(위치, 여유, 사냥대상만), 클릭대상(위치, 여유), 채집대상(위치, 여유));
        }
    }

    static float 클릭거리제곱(Vector2 간격, float 반너비, float 바닥, float 높이)
    {
        float x = Mathf.Max(0, Mathf.Abs(간격.x) - 반너비);
        float y = 간격.y - Mathf.Clamp(간격.y, 바닥, 높이);
        return x * x + y * y;
    }

    int 짐승가리킴(Vector2 위치, float 여유, bool 사냥대상만 = false)
    {
        if (짐승슬롯 == null || 상태.짐승들 == null) return -1;
        int 수 = Mathf.Min(짐승슬롯.Length, 상태.짐승들.Length);
        int 앞 = int.MinValue;
        int 선택 = -1;
        float 거리제곱 = 여유 * 여유;
        for (int i = 0; i < 수; i++)
        {
            var 그림 = 짐승슬롯[i];
            var 짐승 = 상태.짐승들[i];
            if (짐승.등장번호 == 0 || 짐승.전리품 || (사냥대상만 && !짐승.사냥가능)) continue;
            if (그림 == null || !그림.enabled || 그림.sprite == null) continue;

            var 간격 = 위치 - (Vector2)그림.transform.position;
            float 반너비 = 짐승.사냥가능 ? (짐승.종류 == 전장짐승.돼지 ? .48f : .32f) : .7f;
            float 후보 = 클릭거리제곱(간격, 반너비, 짐승.사냥가능 ? -.16f : -.32f, 짐승.사냥가능 ? .57f : 1.2f);
            if (후보 > 거리제곱 || (후보 == 거리제곱 && 그림.sortingOrder <= 앞)) continue;

            거리제곱 = 후보;
            앞 = 그림.sortingOrder;
            선택 = i;
        }
        return 선택;
    }

    bool 사냥명령(int 번호)
    {
        if (번호 < 0) return false;
        var 선택 = 상태.짐승들[번호];
        int[] 부대 = null;
        if (선택부대.Count > 0)
        {
            부대 = new int[선택부대.Count];
            선택부대.CopyTo(부대);
        }
        return 명령전달(명령종류.사냥, 선택.위치, 선택.등장번호, 부대);
    }

    bool 훈련소사용(int 번호)
    {
        if (번호 < 0 || 상태.대상들[번호] is not { 생존: true, 종류: 대상종류.훈련소 } 건물
            || 건물.팀 != 내팀) return false;
        var 부대 = new List<int>();
        foreach (int 선택 in 선택부대)
            if (상태.대상들[선택] is { 생존: true, 종류: 대상종류.주민 }) 부대.Add(선택);
        부대.Sort();
        명령전달(명령종류.주민훈련, 건물.위치, 번호, 부대.ToArray());
        return true;
    }

    bool 모닥불사용(int 번호)
    {
        if (번호 != 전장규칙.모닥불번호(내팀)) return false;
        var 모닥불 = 상태.대상들[번호];
        if (모닥불 is not { 생존: true, 완공대기: <= 0 }) return false;
        식량창.열기();
        if ((내왕().위치 - 모닥불.위치).sqrMagnitude > 전장식량.조리거리 * 전장식량.조리거리)
            명령전달(명령종류.이동, 모닥불.위치);
        return true;
    }

    void 선택부대명령(Vector2 위치, int 자원 = -1, bool 지도입력 = false)
    {
        if (선택부대.Count == 0) return;
        var 부대 = new int[선택부대.Count];
        선택부대.CopyTo(부대);
        if (자원 >= 0 && 선택주민있음())
            명령전달(명령종류.채집, 위치, 자원, 부대);
        else 명령전달(명령종류.부대이동, 위치, 부대: 부대, 지도입력: 지도입력);
    }

    int 클릭대상(Vector2 위치, float 여유)
    {
        int 결과 = -1, 앞 = int.MinValue;
        int 적유닛 = -1, 적유닛앞 = int.MinValue;
        float 허용거리 = 여유 * 여유;
        float 거리 = 허용거리, 적유닛거리 = 허용거리;
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            if (대상 == null || !대상.생존 || !그림.enabled || !대상표시(대상)) continue;
            Vector2 간격 = 위치 - (Vector2)그림.transform.position;
            bool 모닥불 = 대상.종류 == 대상종류.모닥불;
            float 반너비 = 대상.울타리 ? (대상.세로 ? .42f : .57f) : 모닥불 ? .6f : 대상.건물 ? 1f : .42f;
            float 높이 = 대상.울타리 ? (대상.세로 ? 1.16f : .82f)
                : 모닥불 ? 1f : 대상.건물 ? 1.9f : 대상.종류 == 대상종류.주민 ? 1.18f : 1f;
            float 바닥 = 대상.울타리 && 대상.세로 ? -.55f : -.25f;
            float 후보 = 클릭거리제곱(간격, 반너비, 바닥, 높이);
            if (후보 > 허용거리) continue;
            int 순서 = 그림.sortingOrder;
            if (후보 < 거리 || (후보 == 거리 && 순서 > 앞))
            {
                거리 = 후보;
                앞 = 순서;
                결과 = i;
            }
            if (대상.팀 != 내팀 && !대상.건물 && (후보 < 적유닛거리 || (후보 == 적유닛거리 && 순서 > 적유닛앞)))
            {
                적유닛거리 = 후보;
                적유닛앞 = 순서;
                적유닛 = i;
            }
        }
        if (적유닛 >= 0 && 적유닛거리 == 거리 && 결과 >= 0 && 상태.대상들[결과].종류 == 대상종류.회관) return 적유닛;
        return 결과;
    }

    int 채집대상(Vector2 위치, float 여유)
    {
        int 결과 = -1;
        float 거리 = float.PositiveInfinity;
        for (int i = 0; i < 자원들.Length; i++)
        {
            if (!안개.탐사됨(자원들[i].오브젝트.position)) continue;
            float 후보 = Vector2.Distance(위치, (Vector2)자원들[i].오브젝트.position + Vector2.up * .5f);
            if (후보 >= 거리) continue;
            if (후보 >= 1.42f + 여유)
            {
                var 그림 = 자원그림[i];
                if (그림 == null) continue;
                var 영역 = 그림.bounds;
                var 간격 = 위치 - (Vector2)영역.center;
                if (클릭거리제곱(간격, 영역.extents.x, -영역.extents.y, 영역.extents.y) > 여유 * 여유) continue;
            }
            거리 = 후보;
            결과 = i;
        }
        return 결과;
    }

    bool 선택주민있음()
    {
        foreach (int 번호 in 선택부대)
            if (상태.대상들[번호] is { 생존: true, 종류: 대상종류.주민 }) return true;
        return false;
    }

    bool 전장호버중() => !패널열림 && 배치 < 0 && Application.isFocused && Mouse.current != null
        && (!드래그중 || Vector2.Distance(누른위치, 현재마우스) <= 9) && 전장가리킴(현재마우스);

    SpriteRenderer 호버대상(int 번호, int 자원)
    {
        if (번호 >= 0)
        {
            var 대상 = 상태.대상들[번호];
            bool 조작가능 = 대상.팀 == 내팀
                ? 대상.종류 is 대상종류.병사 or 대상종류.주민 or 대상종류.회관
                : 내왕().생존;
            if (조작가능)
                return 유닛윤곽?[번호] != null && 유닛윤곽[번호].enabled ? null : 슬롯[번호];
            if (대상.팀 != 내팀) return null;
        }

        bool 채집가능 = 선택부대.Count == 0 ? 내왕().생존 : 선택주민있음();
        return 채집가능 && 자원 >= 0 ? 자원그림[자원] : null;
    }

    int 호버갱신()
    {
        int 번호 = -1;
        SpriteRenderer 원본 = null;
        if (전장호버중())
        {
            var 위치 = 전장좌표(현재마우스);
            var (짐승, 대상, 자원) = 전장클릭대상(위치);
            if (짐승 < 0) 번호 = 대상;
            원본 = 짐승 >= 0 ? 짐승슬롯[짐승] : 호버대상(번호, 자원);
        }
        if (호버윤곽 != null)
        {
            if (원본 != null && 왕목표윤곽 != null && 왕목표윤곽.enabled && 왕목표윤곽.transform.parent == 원본.transform)
                원본 = null;
            윤곽갱신(호버윤곽, 원본);
        }
        return 번호 >= 0 && 상태.대상들[번호].종류 == 대상종류.주민 ? 번호 : -1;
    }

    void 왕목표윤곽갱신()
    {
        if (왕목표윤곽 == null) return;
        var 왕 = 내왕();
        SpriteRenderer 원본 = null;
        if (왕.생존 && 상태.승자 < 0)
        {
            if (왕.채집터 >= 0 && 왕.채집터 < 자원그림.Length) 원본 = 자원그림[왕.채집터];
            else if (왕.표적 >= 0 && 왕.표적 < 슬롯.Length && 상태.대상들[왕.표적] is { 생존: true })
                원본 = 슬롯[왕.표적];
        }
        윤곽갱신(왕목표윤곽, 원본 != null && !원본.forceRenderingOff ? 원본 : null);
    }

    SpriteRenderer 윤곽생성(string 이름, Material 재질)
    {
        var 오브젝트 = new GameObject(이름);
        오브젝트.transform.SetParent(transform, false);
        var 윤곽 = 오브젝트.AddComponent<SpriteRenderer>();
        윤곽.sharedMaterial = 재질;
        윤곽.enabled = false;
        return 윤곽;
    }

    static void 윤곽갱신(SpriteRenderer 윤곽, SpriteRenderer 원본, int 정렬간격 = -1)
    {
        윤곽.enabled = 원본 != null && 원본.enabled && 원본.sprite != null;
        if (!윤곽.enabled) return;

        if (윤곽.transform.parent != 원본.transform)
        {
            윤곽.transform.SetParent(원본.transform, false);
            윤곽.gameObject.layer = 원본.gameObject.layer;
        }
        윤곽.sprite = 원본.sprite;
        윤곽.flipX = 원본.flipX;
        윤곽.flipY = 원본.flipY;
        윤곽.sortingLayerID = 원본.sortingLayerID;
        윤곽.sortingOrder = 원본.sortingOrder + 정렬간격;
    }

    public void 시점복귀()
    {
        if (!지도포인터.HasValue) 자동추적 = true;
    }

    Vector3 카메라한계(Vector2 중심)
    {
        float 높이 = 전장카메라.orthographicSize;
        float 너비 = 높이 * 전장카메라.aspect;
        var 범위 = 길찾기.전장범위;
        float 가로한계 = Mathf.Max(0, 범위.width * .5f - 너비), 세로한계 = Mathf.Max(0, 범위.height * .5f - 높이);
        var 차이 = 중심 - 범위.center;
        return new Vector3(범위.center.x + Mathf.Clamp(차이.x, -가로한계, 가로한계),
            범위.center.y + Mathf.Clamp(차이.y, -세로한계, 세로한계), -10);
    }

    void 카메라이동(float 초)
    {
        if (지도포인터.HasValue) return;
        var 현재 = 전장카메라.transform.position;
        Vector2 방향 = Vector2.zero;
        if (Application.isFocused && Mouse.current != null && 현재마우스.x > -48 && 현재마우스.x < Screen.width + 48
            && 현재마우스.y > -48 && 현재마우스.y < Screen.height + 48)
        {
            const int 여백 = 12;
            방향.x = 현재마우스.x <= 여백 ? -1 : 현재마우스.x >= Screen.width - 여백 ? 1 : 0;
            방향.y = 현재마우스.y <= 여백 ? -1 : 현재마우스.y >= Screen.height - 여백 ? 1 : 0;
        }
        if (방향 != Vector2.zero) 자동추적 = false;
        var 목표 = 자동추적 ? 내왕().위치 : (Vector2)현재 + 방향.normalized * (11 * 초);
        전장카메라.transform.position = 자동추적
            ? Vector3.Lerp(현재, 카메라한계(목표), 1 - Mathf.Exp(-9 * 초))
            : 카메라한계(목표);
    }

    bool 대상표시(전장대상 대상) =>
        안개.탐사됨(대상.위치) && 지형.대상보임(대상, 내팀);

    void 장면반영(float 초)
    {
        짐승그림갱신(초);
        bool 피격음재생 = false;
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            bool 생존 = 대상 is { 생존: true };
            float 체력 = 대상?.체력 ?? 0;
            bool 피격 = 대상 != null && 체력 < 대상이전체력[i];
            대상이전체력[i] = 체력;
            bool 보임 = (생존 || 피격) && 대상표시(대상);
            if (!피격음재생 && 피격 && 보임)
            {
                var 화면위치 = 전장카메라.WorldToViewportPoint(대상.위치);
                피격음재생 = 화면위치.z > 0 && 화면위치.x >= 0 && 화면위치.x <= 1
                    && 화면위치.y >= 0 && 화면위치.y <= 1;
            }
            if (!생존 || !보임)
            {
                if (대상?.종류 == 대상종류.왕) 왕그림들[대상.팀] = default;
                그림.enabled = false;
                if (!생존) 선택부대.Remove(i);
                if (유닛윤곽?[i] != null) 유닛윤곽[i].enabled = false;
                continue;
            }
            Vector3 현재 = 그림.transform.position;
            Vector3 목표 = 대상.위치;
            그림.transform.position = 규칙 != null || !그림.enabled || Vector3.Distance(현재, 목표) > 2
                ? 목표 : Vector3.MoveTowards(현재, 목표, 초 * 6);
            if (대상.종류 == 대상종류.주민 && 주민방향프레임?.Length == 16)
                주민그림갱신(i, 대상, 그림, 그림.transform.position - 현재, 일시정지 ? 0 : 초);
            else if (대상.종류 == 대상종류.병사)
                병사그림갱신(i, 대상, 그림, 그림.transform.position - 현재, 피격, 일시정지 ? 0 : 초);
            else 그림.flipX = !대상.건물 && 대상.왼쪽;
            if (대상.울타리) 그림.sprite = 울타리그림(대상.종류, 대상.세로);
            그림.enabled = true;
            그림.sortingOrder = (int)(-그림.transform.position.y * 80) + 1600;
            if (대상.종류 == 대상종류.왕) 왕그림갱신(대상, 그림, 일시정지 ? 0 : 초);
            그림.color = 대상.효과시간 > 0 ? new Color(1, .8f, .67f)
                : 대상.완공대기 > 0 ? new Color(1, 1, 1, .57f)
                : 대상.종류 == 대상종류.왕 ? Color.Lerp(Color.white, 대상.팀 == 0 ? 청록 : 적색, .68f) : Color.white;
            bool 은신중 = 대상.팀 == 내팀 && !대상.건물 && 지형.은신지형인가(대상.위치);
            if (은신중) 그림.color *= new Color(1, 1, 1, .42f);
            if (유닛윤곽 == null) continue;
            bool 유닛선택 = (대상.종류 is 대상종류.주민 or 대상종류.병사) && 선택부대.Contains(i);
            if (은신중 || 유닛선택)
            {
                var 재질 = 은신중 ? 은신재질 : 유닛선택재질;
                if (유닛윤곽[i] == null) 유닛윤곽[i] = 윤곽생성("유닛 윤곽", 재질);
                else if (유닛윤곽[i].sharedMaterial != 재질) 유닛윤곽[i].sharedMaterial = 재질;
                윤곽갱신(유닛윤곽[i], 그림, 은신중 ? 1 : -1);
            }
            else if (유닛윤곽[i] != null) 유닛윤곽[i].enabled = false;
        }
        if (피격음재생 && !일시정지 && 피격효과음 != null && 효과음재생기 != null)
            효과음재생기.PlayOneShot(피격효과음);
        왕목표윤곽갱신();
        그림자들?.반영(상태.대상들, 안개);
        건설미리보기갱신();
    }

    void 짐승그림갱신(float 초)
    {
        if (짐승슬롯 == null || 짐승그림들 == null) return;
        var 짐승들 = 상태.짐승들;
        int 수 = Mathf.Min(짐승슬롯.Length, 짐승등장번호.Length);
        for (int i = 0; i < 수; i++)
        {
            var 그림 = 짐승슬롯[i];
            if (그림 == null) continue;
            var 짐승 = 짐승들 != null && i < 짐승들.Length ? 짐승들[i] : default;
            if (짐승.등장번호 == 0 || (uint)짐승.종류 >= (uint)짐승그림들.Length || !안개.탐사됨(짐승.위치))
            {
                그림.enabled = false;
                continue;
            }

            if (짐승.전리품)
            {
                int 고기 = 짐승.종류 == 전장짐승.돼지 ? (int)고기종류.날돼지고기 : (int)고기종류.날닭고기;
                그림.transform.position = 짐승.위치 + Vector2.up * .19f;
                그림.sprite = 고기그림들[고기];
                그림.sortingOrder = (int)(-짐승.위치.y * 80) + 1602;
                그림.enabled = true;
                continue;
            }

            var 방향프레임 = 짐승그림들[짐승.종류].방향프레임;
            if (방향프레임 == null || (uint)짐승.방향 >= (uint)방향프레임.Length)
            {
                그림.enabled = false;
                continue;
            }
            그림.transform.position = 규칙 != null || !그림.enabled || 짐승등장번호[i] != 짐승.등장번호
                ? 짐승.위치 : Vector2.MoveTowards(그림.transform.position, 짐승.위치, 초 * 6f);
            짐승등장번호[i] = 짐승.등장번호;
            그림.sprite = 방향프레임[짐승.방향];
            그림.sortingOrder = (int)(-그림.transform.position.y * 80) + 1600;
            그림.enabled = true;
        }
    }

    void 주민그림갱신(int 슬롯번호, 전장대상 주민, SpriteRenderer 그림, Vector2 이동, float 초)
    {
        const int 걷기프레임수 = 6;
        const int 방향당프레임 = 걷기프레임수 + 3;
        const float 보폭주기거리 = 1.20f;
        const float 튀는길이 = .30f;

        ref var 동작 = ref 주민그림들[슬롯번호];
        if (!그림.enabled || !동작.표시중)
            동작 = new 주민그림상태 { 표시중 = true, 방향 = 2 };
        else
        {
            동작.튀는시간 = Mathf.Max(0, 동작.튀는시간 - 초);
            if (이동.sqrMagnitude > .000001f)
            {
                if (동작.이동여운 <= 0) 동작.걷기거리 = 0;
                동작.이동여운 = .12f;
                동작.걷기거리 = Mathf.Repeat(동작.걷기거리 + 이동.magnitude, 보폭주기거리);
                동작.방향 = 스프라이트방향(이동);
            }
            else 동작.이동여운 = Mathf.Max(0, 동작.이동여운 - 초);

            int 채집터 = 주민.채집터;
            if (채집터 >= 0 && 채집터 < 자원들.Length && 주민.공격대기 > 동작.이전공격대기 + .12f)
            {
                var 채집방향 = (Vector2)자원들[채집터].오브젝트.position - 주민.위치;
                if (채집방향.sqrMagnitude <= 전장규칙.채집거리 * 전장규칙.채집거리)
                {
                    if (채집방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(채집방향);
                    동작.튀는시간 = 튀는길이;
                    if (주민.팀 == 내팀) 채집효과음재생(채집터);
                }
            }
            if (주민.체력 < 동작.이전체력) 동작.튀는시간 = 튀는길이;
        }
        동작.이전공격대기 = 주민.공격대기;
        동작.이전체력 = 주민.체력;

        int 방향번호 = 주민.팀 * 8 + 동작.방향;
        var 프레임 = 주민방향프레임[방향번호];
        if (주민동작프레임?.Length == 16 * 방향당프레임)
        {
            int 시작 = 방향번호 * 방향당프레임;
            if (동작.튀는시간 > 0)
            {
                int 단계 = Mathf.Min(4, (int)((1 - 동작.튀는시간 / 튀는길이) * 5));
                프레임 = 주민동작프레임[시작 + 걷기프레임수 + 2 - Mathf.Abs(2 - 단계)];
            }
            else if (동작.이동여운 > 0)
            {
                int 단계 = Mathf.Min(걷기프레임수 - 1, (int)(동작.걷기거리 / 보폭주기거리 * 걷기프레임수));
                프레임 = 주민동작프레임[시작 + 단계];
            }
        }
        if (프레임 != 그림.sprite) 그림.sprite = 프레임;
        그림.flipX = false;
    }

    void 병사그림갱신(int 슬롯번호, 전장대상 병사, SpriteRenderer 그림, Vector2 이동, bool 피격, float 초)
    {
        const int 대기프레임 = 4;
        const int 걷기프레임 = 6;
        const int 공격프레임 = 6;
        const int 피격프레임 = 3;
        const int 방향당프레임 = 대기프레임 + 걷기프레임 + 공격프레임 + 피격프레임;
        const float 대기길이 = 1.2f;
        const float 보폭주기거리 = 1.20f;
        const float 공격길이 = .48f;
        const float 피격길이 = .24f;
        if (병사동작프레임?.Length != 16 * 방향당프레임) return;

        ref var 동작 = ref 병사그림들[슬롯번호];
        if (!그림.enabled)
            동작 = new 병사그림상태 { 방향 = 병사.왼쪽 ? 4 : 0 };
        else
        {
            동작.공격여운 = Mathf.Max(0, 동작.공격여운 - 초);
            동작.피격여운 = 피격 ? 피격길이 : Mathf.Max(0, 동작.피격여운 - 초);
            if (이동.sqrMagnitude > .000001f)
            {
                if (동작.이동여운 <= 0) 동작.걷기거리 = 0;
                동작.이동여운 = .12f;
                동작.걷기거리 = Mathf.Repeat(동작.걷기거리 + 이동.magnitude, 보폭주기거리);
                if (동작.공격여운 <= 0) 동작.방향 = 스프라이트방향(이동);
            }
            else 동작.이동여운 = Mathf.Max(0, 동작.이동여운 - 초);

            if (병사.공격대기 > 0 && 병사.공격대기 > 동작.이전공격대기 + .12f)
            {
                동작.공격여운 = 공격길이;
                if ((uint)병사.표적 < (uint)상태.대상들.Length && 상태.대상들[병사.표적] is { } 적)
                {
                    var 공격방향 = 적.위치 - 병사.위치;
                    if (공격방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(공격방향);
                }
            }
        }
        동작.이전공격대기 = 병사.공격대기;
        동작.대기시간 = Mathf.Repeat(동작.대기시간 + 초, 대기길이);

        int 번호;
        if (동작.공격여운 > 0)
            번호 = 대기프레임 + 걷기프레임
                + Mathf.Min(공격프레임 - 1, (int)((1 - 동작.공격여운 / 공격길이) * 공격프레임));
        else if (동작.피격여운 > 0)
            번호 = 대기프레임 + 걷기프레임 + 공격프레임
                + Mathf.Min(피격프레임 - 1, (int)((1 - 동작.피격여운 / 피격길이) * 피격프레임));
        else if (동작.이동여운 > 0)
            번호 = 대기프레임 + Mathf.Min(걷기프레임 - 1, (int)(동작.걷기거리 / 보폭주기거리 * 걷기프레임));
        else
            번호 = Mathf.Min(대기프레임 - 1, (int)(동작.대기시간 / 대기길이 * 대기프레임));

        int 방향번호 = 병사.팀 * 8 + 동작.방향;
        var 프레임 = 병사동작프레임[방향번호 * 방향당프레임 + 번호];
        if (프레임 != 그림.sprite) 그림.sprite = 프레임;
        그림.flipX = false;
    }

    void 왕그림갱신(전장대상 왕, SpriteRenderer 그림, float 초)
    {
        const int 방향당프레임 = 15;
        const int 전체프레임 = 8 * 방향당프레임;
        const float 공격길이 = .55f;
        if (왕대기프레임?.Length != 전체프레임 || 왕이동프레임?.Length != 전체프레임
            || 왕공격프레임?.Length != 전체프레임) return;

        Vector2 채집방향 = Vector2.zero;
        bool 채집중 = false;
        if (!왕.이동중 && 왕.채집터 >= 0 && 왕.채집터 < 자원들.Length)
        {
            채집방향 = (Vector2)자원들[왕.채집터].오브젝트.position - 왕.위치;
            채집중 = 채집방향.sqrMagnitude <= 전장규칙.채집거리 * 전장규칙.채집거리;
        }

        bool 공격시작 = false;
        ref var 동작 = ref 왕그림들[왕.팀];
        if (동작.표시중)
        {
            var 이동 = 왕.위치 - 동작.위치;
            동작.이동여운 = Mathf.Max(0, 동작.이동여운 - 초);
            동작.공격여운 = Mathf.Max(0, 동작.공격여운 - 초);
            if (이동.sqrMagnitude > .000001f)
            {
                // 시뮬레이션과 네트워크 위치 갱신 사이에도 달리기를 이어간다.
                동작.이동여운 = .12f;
                동작.방향 = 스프라이트방향(이동);
            }
            if (왕.채집터 < 0 && !동작.채집중 && 왕.공격대기 > 0 && 왕.공격대기 > 동작.이전공격대기)
            {
                동작.공격여운 = 공격길이;
                공격시작 = true;
                if (왕.표적 >= 0 && 상태.대상들[왕.표적] is { } 적)
                {
                    var 공격방향 = 적.위치 - 왕.위치;
                    if (공격방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(공격방향);
                }
            }
        }
        else
        {
            동작.표시중 = true;
            동작.방향 = 왕.왼쪽 ? 4 : 0;
        }
        동작.위치 = 왕.위치;
        동작.이전공격대기 = 왕.공격대기;

        if (채집중)
        {
            동작.공격여운 = 0;
            if (채집방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(채집방향);
        }
        bool 공격중 = 동작.공격여운 > 0;
        bool 이동중 = 동작.이동여운 > 0;
        var 프레임 = 채집중 || 공격중 ? 왕공격프레임 : 이동중 ? 왕이동프레임 : 왕대기프레임;
        float 반복길이 = 채집중 ? 공격길이 : 이동중 ? .6f : 1.2f;
        float 이전시간 = 동작.동작시간;
        float 다음시간 = 동작.프레임 == 프레임 && 동작.채집중 == 채집중 ? 이전시간 + 초 : 0;
        bool 채집휘두르기시작 = 채집중 && (!동작.채집중 || 다음시간 >= 반복길이);
        if (왕.팀 == 내팀 && !일시정지)
        {
            if ((공격시작 || 채집휘두르기시작) && 왕공격효과음 != null && 효과음재생기 != null)
                효과음재생기.PlayOneShot(왕공격효과음);
            if (채집휘두르기시작) 채집효과음재생(왕.채집터);
        }

        if (왕.팀 == 내팀 && 프레임 == 왕이동프레임 && 다음시간 > 이전시간)
        {
            // 달리기 스프라이트에서 양발이 차례로 땅에 닿는 프레임이다.
            왕발소리재생(왕.위치, 이전시간, 다음시간, 반복길이, 3f / 방향당프레임);
            왕발소리재생(왕.위치, 이전시간, 다음시간, 반복길이, 10f / 방향당프레임);
        }
        동작.동작시간 = 다음시간 % 반복길이;
        동작.채집중 = 채집중;
        동작.프레임 = 프레임;
        float 진행률 = 공격중 ? 1 - 동작.공격여운 / 공격길이 : 동작.동작시간 / 반복길이;
        int 번호 = Mathf.Min((int)(진행률 * 방향당프레임), 방향당프레임 - 1);
        그림.sprite = 프레임[동작.방향 * 방향당프레임 + 번호];
        그림.flipX = false;
    }

    void 채집효과음재생(int 채집터)
    {
        if (일시정지 || 효과음재생기 == null) return;
        var 소리 = 자원들[채집터].재화번호 == 0 ? 나무채집효과음 : 돌채집효과음;
        if (소리 != null) 효과음재생기.PlayOneShot(소리);
    }

    static void 발소리미리읽기(AudioClip[] 소리들)
    {
        if (소리들 == null) return;
        foreach (var 소리 in 소리들)
            if (소리 != null) 소리.LoadAudioData();
    }

    AudioClip[] 지면발소리(Vector2 위치)
    {
        if (왕흙발소리?.Length > 0 && 흙타일?.Length > 0)
        {
            var 타일 = 바닥타일맵.GetTile(바닥타일맵.WorldToCell(위치));
            if (타일 != null && Array.IndexOf(흙타일, 타일) >= 0) return 왕흙발소리;
        }
        return 왕발소리;
    }

    void 왕발소리재생(Vector2 위치, float 이전시간, float 다음시간, float 반복길이, float 착지진행률)
    {
        if (효과음재생기 == null) return;
        float 착지시간 = 반복길이 * 착지진행률;
        if (착지시간 <= 이전시간) 착지시간 += 반복길이;
        if (착지시간 > 다음시간) return;

        var 소리들 = 지면발소리(위치);
        if (소리들 == null || 소리들.Length == 0) return;
        var 소리 = 소리들[UnityEngine.Random.Range(0, 소리들.Length)];
        if (소리 != null) 효과음재생기.PlayOneShot(소리);
    }

    static int 스프라이트방향(Vector2 방향)
    {
        // 스프라이트 시트의 행은 오른쪽부터 시계 방향으로 배치되어 있다.
        return (Mathf.RoundToInt(-Mathf.Atan2(방향.y, 방향.x) * Mathf.Rad2Deg / 45f) + 8) % 8;
    }

    void 건설미리보기갱신()
    {
        if (배치 < 0 || 패널열림 || !Application.isFocused || Mouse.current == null || 전장표시.panel == null)
        {
            건설미리보기.enabled = false;
            건설커서.gameObject.SetActive(false);
            return;
        }

        건설미리보기.enabled = 전장가리킴(현재마우스);
        if (!건설미리보기.enabled)
        {
            건설커서.gameObject.SetActive(false);
            return;
        }

        bool 가능 = 전장규칙.건설가능(상태, 길찾기, 자원들, 내팀, 배치, 전장좌표(현재마우스), out Vector2 위치);
        if (!안개.탐사됨(위치))
        {
            건설미리보기.enabled = false;
            건설커서.gameObject.SetActive(false);
            return;
        }
        건설미리보기.sprite = 건물그림(배치);
        건설미리보기.transform.position = 위치;
        건설미리보기.color = 가능 ? new Color(.6f, 1, .85f, .58f) : new Color(1, .38f, .35f, .58f);
        건설커서.gameObject.SetActive(가능);
        if (!가능) return;

        var 부모 = (RectTransform)건설커서.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 현재마우스, null, out var 커서위치);
        건설커서.anchoredPosition = 커서위치 - new Vector2(부모.rect.xMin, 부모.rect.yMax) + new Vector2(12, -12);
    }

    bool 전장가리킴(Vector2 화면)
    {
        if (주민패널.열림) return false;
        if (전장표시.panel == null || 전장표시.panel.Pick(화면변환(화면)) != 전장표시) return false;
        if (EventSystem.current == null) return true;
        HUD포인터 ??= new PointerEventData(EventSystem.current);
        HUD포인터.position = 화면;
        HUD충돌.Clear();
        전장HUD.Raycast(HUD포인터, HUD충돌);
        return HUD충돌.Count == 0;
    }

    void LateUpdate()
    {
        if (!전투중) return;
        채집그림갱신();
        유닛상태창갱신();
        if (뿌리.panel == null) return;

        // 미니맵의 픽셀 영역을 씬에 배치한 RectTransform에 맞춘다.
        var 영역 = 미니맵영역.rect;
        var 시작 = 화면변환(RectTransformUtility.WorldToScreenPoint(null, 미니맵영역.TransformPoint(영역.min)));
        var 끝 = 화면변환(RectTransformUtility.WorldToScreenPoint(null, 미니맵영역.TransformPoint(영역.max)));
        var 범위 = 사각형(시작, 끝);
        if (범위 == 미니맵범위) return;
        미니맵범위 = 범위;
        지도.style.left = 범위.x;
        지도.style.top = 범위.y;
        지도.style.width = 범위.width;
        지도.style.height = 범위.height;
    }

    void 유닛상태창갱신()
    {
        int 번호 = 주민스탯번호;
        if (번호 < 0 && 선택부대.Count == 1)
            foreach (int 선택번호 in 선택부대) 번호 = 선택번호;

        if (번호 < 0 || 패널열림 || 주민패널.열림 || 배치 >= 0
            || !슬롯[번호].enabled || 상태.대상들[번호] is not { 생존: true } 대상)
        {
            상태창?.숨기기();
            return;
        }

        상태창 ??= new 유닛상태창((RectTransform)전투.transform, 도움.font, 주민패널그림);
        상태창.갱신(번호, 대상, 슬롯[번호].bounds, 전장카메라);
    }

    void 재료표시준비()
    {
        const float 행간격 = 52;
        var 부모 = (RectTransform)전투.transform;
        var 글꼴 = 자원표시.font;
        var 글색 = 자원표시.color;
        재료수량글 = new TMP_Text[채집아이콘들.Length];

        for (int i = 0; i < 재료수량글.Length; i++)
        {
            var 행영역 = new GameObject($"재료 {채집아이콘이름[i]}", typeof(RectTransform)).GetComponent<RectTransform>();
            행영역.SetParent(부모, false);
            행영역.anchorMin = 행영역.anchorMax = new Vector2(0, .5f);
            행영역.pivot = new Vector2(0, .5f);
            행영역.anchoredPosition = new Vector2(13, ((재료수량글.Length - 1) * .5f - i) * 행간격);
            행영역.sizeDelta = new Vector2(170, 48);

            var 아이콘 = new GameObject("아이콘", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            아이콘.sprite = 채집아이콘들[i];
            아이콘.preserveAspect = true;
            아이콘.raycastTarget = false;
            var 아이콘영역 = 아이콘.rectTransform;
            아이콘영역.SetParent(행영역, false);
            아이콘영역.anchorMin = 아이콘영역.anchorMax = new Vector2(0, .5f);
            아이콘영역.anchoredPosition = new Vector2(24, 0);
            아이콘영역.sizeDelta = new Vector2(40, 40);

            var 글 = i == 0 ? 자원표시 :
                new GameObject("수량", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            글.font = 글꼴;
            글.color = 글색;
            글.fontSize = 24;
            글.alignment = TextAlignmentOptions.MidlineLeft;
            글.textWrappingMode = TextWrappingModes.NoWrap;
            글.raycastTarget = false;
            var 글영역 = 글.rectTransform;
            글영역.SetParent(행영역, false);
            글영역.anchorMin = Vector2.zero;
            글영역.anchorMax = Vector2.one;
            글영역.offsetMin = new Vector2(53, 0);
            글영역.offsetMax = Vector2.zero;
            재료수량글[i] = 글;
        }
    }

    void 재료수량갱신(int[] 재화)
    {
        bool 처음표시 = 표시한재료수량 == null;
        표시한재료수량 ??= new int[재료수량글.Length];
        for (int i = 0; i < 재료수량글.Length; i++)
        {
            int 수량 = 재화[i];
            if (!처음표시 && 표시한재료수량[i] == 수량) continue;
            표시한재료수량[i] = 수량;
            재료수량글[i].text = 수량.ToString();
        }
    }

    void 정보갱신(int 주민호버번호 = -1)
    {
        var 팀 = 상태.진영들[내팀];
        식량창.갱신(팀, !패널열림 && !주민패널.열림);
        재료수량갱신(팀.재화);
        병력표시.text = $"병사 {전장규칙.수량(상태, 내팀, 대상종류.병사)}/{전장규칙.병력제한}   주민 {전장규칙.수량(상태, 내팀, 대상종류.주민)}/{전장규칙.주민제한}";
        int 흐른초 = Mathf.FloorToInt(상태.시간);
        시계.text = $"{흐른초 / 60:00}:{흐른초 % 60:00}";
        var 아군회관 = 상태.대상들[전장규칙.회관번호(내팀)];
        var 상대회관 = 상태.대상들[전장규칙.회관번호(1 - 내팀)];
        회관체력.갱신(아군회관, 상대회관, 안개.탐사됨(상대회관.위치));
        var 왕 = 내왕();
        왕상태.text = 왕.생존 ? $"{팀.직업}   {Mathf.CeilToInt(왕.체력)} / {왕.최대체력}" : $"왕 재등장   {Mathf.CeilToInt(왕.재등장)}초";
        임무.text = 팀.심부름 switch
        {
            0 => "심부름 1/4   자원 우클릭 또는 가까이서 G로 왕이 직접 채집하세요.",
            1 => "심부름 2/4   제재소를 지으세요.",
            2 => "심부름 3/4   주민을 선택하고 훈련소를 클릭해 병사로 훈련하세요.",
            3 => "심부름 4/4   적 회관 근처에 병력을 보내세요.",
            _ => "심부름 완료   적 마을회관을 무너뜨리세요."
        };
        알림.text = 팀.알림시간 > 0 ? 팀.알림 : 팀.훈련대기인원 > 0
            ? $"병사 훈련 {팀.훈련대기인원}명   {Mathf.CeilToInt(팀.훈련남은시간)}초 · 앞 주민 도착 후 진행"
            : 팀.모집종류 >= 0 ? $"주민 모집 중   {Mathf.CeilToInt(팀.모집대기)}초" : "";
        알림.gameObject.SetActive(알림.text.Length > 0);
        하단안내갱신(주민호버번호);
        for (int i = 0; i < 스킬테두리.Length; i++)
        {
            float 남은시간 = 팀.쿨다운[i];
            스킬표시[i].alpha = 남은시간 <= 0 && 왕.생존 ? 1 : .52f;
            스킬테두리[i].진행표시(남은시간, 전장규칙.스킬쿨다운길이(i), 왕.생존);
        }
        for (int i = 0; i < 행동버튼.Length; i++)
            행동버튼[i].선택표시(i == 배치);
    }

    void 하단안내갱신(int 주민호버번호)
    {
        if (패널열림 || 주민패널.열림)
        {
            설명중인스킬 = -1;
            도움.text = "";
            return;
        }
        if (스킬설명갱신()) return;

        도움.text = 전장규칙.울타리건설인가(배치)
            ? "좌클릭 연속 설치  ·  V 방향 전환  ·  우클릭 / Esc 취소  ·  회관 반경 8" :
            배치 >= 0 ? "좌클릭 설치  ·  우클릭 / Esc 취소  ·  회관 반경 8" :
            $"{(키보드이동 ? "WASD" : "바닥 클릭")} 왕 이동  ·  G {(주민호버번호 >= 0 ? "스탯 보기" : "왕 채집")}  ·  드래그 선택";
        if (배치 < 0 && 선택부대.Count > 1) 도움.text = $"{선택부대.Count}명 선택  ·  좌클릭 명령  ·  미니맵 클릭 이동";
        if (배치 < 0 && 선택부대.Count == 1)
            도움.text = "좌클릭 명령  ·  미니맵 클릭 이동  ·  Esc 선택 해제";
    }

    void 채집그림갱신()
    {
        int[] 횟수들 = 상태.진영들[내팀].자원별채집횟수;
        if (횟수들?.Length != 자원들.Length) return;
        if (채집그림들 == null)
        {
            채집그림들 = new 채집그림상태[자원들.Length];
            for (int i = 0; i < 채집그림들.Length; i++) 채집그림들[i].횟수 = 횟수들[i];
            return;
        }

        float 지금 = Time.unscaledTime;
        var 부모 = (RectTransform)전투.transform;
        for (int i = 0; i < 채집그림들.Length; i++)
        {
            ref var 효과 = ref 채집그림들[i];
            if (횟수들[i] > 효과.횟수 && 안개.탐사됨(자원들[i].오브젝트.position) && 자원그림[i]?.sprite != null)
            {
                var 자원 = 자원그림[i];
                var 범위 = 자원.bounds;
                효과.출발위치 = new Vector3(범위.center.x, 범위.max.y - .25f, 범위.center.z);
                효과.그림 ??= 채집이미지생성(부모, i);
                var 아이콘 = 채집아이콘들[자원들[i].재화번호];
                효과.그림.sprite = 아이콘 != null ? 아이콘 : 자원.sprite;
                효과.시작 = 지금;
                효과.그림.gameObject.SetActive(true);
            }
            효과.횟수 = 횟수들[i];
            if (효과.그림 == null || !효과.그림.gameObject.activeSelf) continue;

            float 경과 = 지금 - 효과.시작;
            if (경과 >= 채집등장시간 + 채집유지시간 + 채집퇴장시간)
            {
                효과.그림.gameObject.SetActive(false);
                continue;
            }

            float 남은등장 = 1 - Mathf.Clamp01(경과 / 채집등장시간);
            float 등장 = 1 - 남은등장 * 남은등장 * 남은등장;
            float 퇴장 = Mathf.SmoothStep(0, 1, (경과 - 채집등장시간 - 채집유지시간) / 채집퇴장시간);
            Vector3 지점 = 효과.출발위치 + Vector3.up * (1.05f * 등장 + .48f * 퇴장);
            Vector3 화면 = 전장카메라.WorldToScreenPoint(지점);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 화면, null, out var 위치);
            효과.그림.rectTransform.anchoredPosition = 위치;
            효과.그림.color = new Color(1, 1, 1, 등장 * (1 - 퇴장));
        }
    }

    Image 채집이미지생성(RectTransform 부모, int 번호)
    {
        var 오브젝트 = new GameObject($"채집 재료 {번호}", typeof(RectTransform), typeof(Image));
        var 그림 = 오브젝트.GetComponent<Image>();
        var 위치 = 그림.rectTransform;
        위치.SetParent(부모, false);
        위치.SetAsFirstSibling();
        위치.anchorMin = 위치.anchorMax = 부모.pivot;
        위치.sizeDelta = new Vector2(72, 72);
        그림.preserveAspect = true;
        그림.raycastTarget = false;
        return 그림;
    }

    bool 스킬설명갱신()
    {
        var 마우스 = Mouse.current;
        if (Application.isFocused && 마우스 != null)
        {
            var 위치 = 마우스.position.ReadValue();
            for (int i = 0; i < 스킬테두리.Length; i++)
            {
                var 슬롯 = 스킬테두리[i].rectTransform;
                if (!슬롯.gameObject.activeInHierarchy
                    || !RectTransformUtility.RectangleContainsScreenPoint(슬롯, 위치)) continue;
                스킬설명표시(i);
                return true;
            }
        }
        설명중인스킬 = -1;
        return false;
    }

    void 스킬설명표시(int 번호)
    {
        if (설명중인스킬 == 번호) return;
        설명중인스킬 = 번호;
        string 키 = 번호 switch { 0 => "Q", 1 => 돌진키표시.text, 2 => "E", _ => "R" };
        string 효과 = 번호 switch
        {
            0 => "지정한 범위의 적에게 기본 피해 38을 줍니다.",
            1 => "마우스 방향으로 돌진합니다. 추적자는 더 멀리 이동하고, 수호자는 3초간 방패를 얻습니다.",
            2 => "왕 주변의 아군 유닛을 회복합니다. 지능이 높을수록 더 많이 회복합니다.",
            _ => "지정한 넓은 범위의 적에게 기본 피해 88을 줍니다. 지능이 높을수록 범위가 넓어집니다."
        };
        도움.text = $"<size=20>{전장규칙.스킬이름[번호]}  [{키}]</size>\n"
            + $"{효과}\n재사용 대기 {전장규칙.스킬쿨다운길이(번호)}초";
    }

    Vector2 화면변환(Vector2 화면) => RuntimePanelUtils.ScreenToPanel(뿌리.panel, new Vector2(화면.x, Screen.height - 화면.y));
    Vector2 위치변환(Vector2 위치) => 화면변환(전장카메라.WorldToScreenPoint(위치));
    static Rect 사각형(Vector2 처음, Vector2 끝) => Rect.MinMaxRect(Mathf.Min(처음.x, 끝.x), Mathf.Min(처음.y, 끝.y),
        Mathf.Max(처음.x, 끝.x), Mathf.Max(처음.y, 끝.y));

    static void 네모(Painter2D 펜, Rect 범위, Color 채움, Color 테두리 = default)
    {
        펜.BeginPath();
        펜.MoveTo(범위.min);
        펜.LineTo(new Vector2(범위.xMax, 범위.yMin));
        펜.LineTo(범위.max);
        펜.LineTo(new Vector2(범위.xMin, 범위.yMax));
        펜.ClosePath();
        if (채움.a > 0) { 펜.fillColor = 채움; 펜.Fill(); }
        if (테두리.a > 0) { 펜.strokeColor = 테두리; 펜.lineWidth = 1; 펜.Stroke(); }
    }

    void 명령화살표그리기(Painter2D 펜, Vector2 위치, float 배율)
    {
        float 경과 = Time.unscaledTime - 명령표시시작;
        if (경과 >= 명령표시길이) return;

        float 남은낙하 = 1 - Mathf.Clamp01(경과 / .19f);
        var 끝 = 위치 - Vector2.up * ((2 + 24 * 남은낙하 * 남은낙하 * 남은낙하) * 배율);
        float 불투명도 = 1 - Mathf.InverseLerp(.3f, 명령표시길이, 경과);

        펜.BeginPath();
        펜.MoveTo(끝);
        펜.LineTo(끝 + new Vector2(-10, -10) * 배율);
        펜.LineTo(끝 + new Vector2(-4, -10) * 배율);
        펜.LineTo(끝 + new Vector2(-4, -20) * 배율);
        펜.LineTo(끝 + new Vector2(4, -20) * 배율);
        펜.LineTo(끝 + new Vector2(4, -10) * 배율);
        펜.LineTo(끝 + new Vector2(10, -10) * 배율);
        펜.ClosePath();
        펜.fillColor = new Color(1, 1, 1, 불투명도);
        펜.Fill();
        펜.strokeColor = new Color(.05f, .09f, .1f, 불투명도);
        펜.lineWidth = 2 * 배율;
        펜.Stroke();
    }

    void 전장그리기(MeshGenerationContext 그림)
    {
        if (상태 == null || !전투중) return;
        var 펜 = 그림.painter2D;
        if (배치 >= 0) 건설격자(펜);
        for (int 팀 = 0; 팀 < 상태.진영들.Length; 팀++)
        {
            var 진영 = 상태.진영들[팀];
            if (진영.음식회복시간 > 0 && 안개.탐사됨(진영.음식회복위치))
                파동그리기(펜, 진영.음식회복위치, 전장식량.회복반경, 진영.음식회복시간, new Color(.57f, .92f, .46f));
            if (진영.스킬표시시간 <= 0 || !안개.탐사됨(진영.스킬위치)) continue;
            if (팀 != 내팀 && 진영.스킬왕위치표시 && 지형.은신지형인가(진영.스킬위치)) continue;
            파동그리기(펜, 진영.스킬위치, 진영.스킬반경, 진영.스킬표시시간, 팀 == 0 ? 청록 : 적색);
        }
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || !슬롯[i].enabled) continue;
            bool 선택 = 선택부대.Contains(i);
            var 위치 = (Vector2)슬롯[i].transform.position;
            var 색 = 대상.팀 == 0 ? 청록 : 적색;
            bool 선택윤곽 = 유닛윤곽?[i] != null && 유닛윤곽[i].enabled && 유닛윤곽[i].sharedMaterial == 유닛선택재질;
            if (선택 && !선택윤곽)
            {
                var 점 = 위치변환(위치 - Vector2.up * .1f);
                if (전장표시.contentRect.Contains(점))
                    네모(펜, new Rect(점.x - 12, 점.y - 3, 24, 6), new Color(.21f, .83f, .72f, .32f), 청록);
            }
            if (!선택 && !대상.건물 && 대상.종류 != 대상종류.왕 && 대상.체력 >= 대상.최대체력) continue;
            bool 모닥불 = 대상.종류 == 대상종류.모닥불;
            float 높이 = 대상.울타리 ? (대상.세로 ? 1.22f : .88f)
                : 모닥불 ? 1.17f : 대상.건물 ? 2.12f : (대상.종류 is 대상종류.주민 or 대상종류.병사) ? 1.35f : 1.15f;
            var 위 = 위치변환(위치 + Vector2.up * 높이);
            if (!전장표시.contentRect.Contains(위)) continue;
            float 폭 = 대상.건물 && !대상.울타리 && !모닥불 ? 49 : 27;
            네모(펜, new Rect(위.x - 폭 / 2, 위.y, 폭, 5), new Color(.06f, .11f, .13f, .9f));
            float 비율 = 대상.완공대기 > 0
                ? 1 - 대상.완공대기 / 전장규칙.건설시간(대상)
                : 대상.체력 / 대상.최대체력;
            if (비율 > 0) 네모(펜, new Rect(위.x - 폭 / 2 + 1, 위.y + 1, (폭 - 2) * 비율, 3), 대상.완공대기 > 0 ? new Color(.9f, .72f, .38f) : 색);
        }
        if (드래그중 && 배치 < 0 && Vector2.Distance(누른위치, 현재마우스) > 9)
            네모(펜, 사각형(화면변환(누른위치), 화면변환(현재마우스)), new Color(1f, 1f, 1f, .13f), Color.white);
        if (!패널열림 && Time.unscaledTime - 명령표시시작 < 명령표시길이)
            명령화살표그리기(펜, 위치변환(명령표시위치), 1);
    }

    void 파동그리기(Painter2D 펜, Vector2 위치, float 효과반경, float 남은시간, Color 색)
    {
        var 중심 = 위치변환(위치);
        float 여운 = 남은시간 / .48f;
        float 반경 = Vector2.Distance(중심, 위치변환(위치 + Vector2.right * 효과반경)) * (1.08f - 여운 * .18f);
        펜.BeginPath();
        펜.Arc(중심, 반경, Angle.Degrees(0), Angle.Degrees(360));
        펜.ClosePath();
        펜.fillColor = new Color(색.r, 색.g, 색.b, 여운 * .16f);
        펜.strokeColor = new Color(색.r, 색.g, 색.b, 여운 * .8f);
        펜.lineWidth = 2;
        펜.Fill();
        펜.Stroke();
    }

    void 건설격자(Painter2D 펜)
    {
        var 회관 = 상태.대상들[전장규칙.회관번호(내팀)];
        var 중심칸 = 바닥타일맵.WorldToCell(회관.위치);
        for (int y = -8; y <= 8; y++)
        for (int x = -8; x <= 8; x++)
        {
            if (x * x + y * y > 64) continue;
            var 칸 = 중심칸 + new Vector3Int(x, y, 0);
            var 위치 = 바닥타일맵.GetCellCenterWorld(칸);
            if (!바닥타일맵.HasTile(칸) || !안개.탐사됨(위치)) continue;
            var 왼쪽아래 = (Vector2)바닥타일맵.CellToWorld(칸);
            var 오른쪽위 = (Vector2)바닥타일맵.CellToWorld(칸 + new Vector3Int(1, 1, 0));
            var 범위 = 사각형(위치변환(왼쪽아래), 위치변환(오른쪽위));
            if (!전장표시.contentRect.Overlaps(범위)) continue;
            var 색 = 길찾기.건설지형가능(위치) ? new Color(.75f, .89f, .71f, .23f) : new Color(1, .45f, .35f, .23f);
            네모(펜, 범위, default, 색);
        }
    }

    Vector2 지도위치(Rect 범위, Vector2 위치)
    {
        var 전장 = 길찾기.전장범위;
        return new Vector2(범위.x + (위치.x - 전장.xMin) / 전장.width * 범위.width,
            범위.y + (전장.yMax - 위치.y) / 전장.height * 범위.height);
    }

    void 지도경로그리기(Painter2D 펜, Rect 범위)
    {
        if (규칙 != null) 규칙.왕이동경로복사(내팀, 내왕경로);
        var 경로 = 규칙 != null ? 내왕경로 : (내팀 == 0 ? 상태.청록왕경로 : 상태.적색왕경로);
        if (경로 == null || 경로.Count == 0) return;

        var 왕그림 = 슬롯[전장규칙.왕번호(내팀)];
        펜.BeginPath();
        펜.MoveTo(지도위치(범위, 왕그림.transform.position));
        foreach (var 위치 in 경로) 펜.LineTo(지도위치(범위, 위치));
        펜.lineWidth = 3.5f;
        펜.strokeColor = new Color(.05f, .09f, .1f, .8f);
        펜.Stroke();
        펜.lineWidth = 2;
        펜.strokeColor = Color.white;
        펜.Stroke();
    }

    void 지도그리기(MeshGenerationContext 그림)
    {
        if (상태 == null) return;
        var 펜 = 그림.painter2D;
        var 범위 = 지도.contentRect;
        지도경로그리기(펜, 범위);
        foreach (var 지점 in 자원들)
        {
            if (!안개.탐사됨(지점.오브젝트.position)) continue;
            var 점 = 지도위치(범위, 지점.오브젝트.position);
            네모(펜, new Rect(점.x - 1, 점.y - 1, 2, 2), new Color(.61f, .62f, .43f, .83f));
        }
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 == null || !대상.생존 || !대상표시(대상)) continue;
            var 점 = 지도위치(범위, 대상.위치);
            float 폭 = 대상.종류 == 대상종류.회관 ? 7 : 대상.건물 || 대상.종류 == 대상종류.왕 ? 4 : 2;
            네모(펜, new Rect(점.x - 폭 / 2, 점.y - 폭 / 2, 폭, 폭), 대상.팀 == 0 ? 청록 : 적색);
        }
        var 중심 = (Vector2)전장카메라.transform.position;
        float 세로 = 전장카메라.orthographicSize, 가로 = 세로 * 전장카메라.aspect;
        var 왼쪽위 = 지도위치(범위, 중심 + new Vector2(-가로, 세로));
        var 오른쪽아래 = 지도위치(범위, 중심 + new Vector2(가로, -세로));
        네모(펜, 사각형(왼쪽위, 오른쪽아래), default, new Color(.92f, .9f, .74f, .64f));
        if (!패널열림 && 지도에서명령)
            명령화살표그리기(펜, 지도위치(범위, 명령표시위치), .6f);
    }

    void 지도누르기(PointerDownEvent 입력)
    {
        if (!전투중 || 패널열림 || 주민패널.열림 || 지도포인터.HasValue) return;
        if (!지도.contentRect.Contains(입력.localPosition)) return;
        if (입력.button == 1 && 입력.pointerType == UnityEngine.UIElements.PointerType.mouse)
        {
            우클릭명령(지도전장좌표(입력.localPosition), 지도입력: true);
            return;
        }
        if (입력.button != 0) return;
        지도누른위치 = 입력.localPosition;
        지도시점고정 = 입력.clickCount >= 2;
        지도이동클릭대기 = !지도시점고정 && 배치 < 0 && 선택부대.Count > 0;
        if (지도시점고정) 자동추적 = false;
        지도누르기전왕기준시점 = (Vector2)전장카메라.transform.position - 내왕().위치;
        지도포인터 = 입력.pointerId;
        지도.CapturePointer(입력.pointerId);
        if (!지도이동클릭대기) 지도시점이동(입력.localPosition);
    }

    bool 지도드래그됨(Vector2 점) => (점 - 지도누른위치).sqrMagnitude > 6 * 6;

    void 지도끌기(PointerMoveEvent 입력)
    {
        if (지도포인터 != 입력.pointerId) return;
        if ((입력.pressedButtons & 1) == 0)
        {
            지도누름해제();
            return;
        }
        if (지도이동클릭대기 && !지도드래그됨(입력.localPosition)) return;
        지도이동클릭대기 = false;
        지도시점이동(입력.localPosition);
    }

    void 지도떼기(PointerUpEvent 입력)
    {
        if (지도포인터 != 입력.pointerId || 입력.button != 0) return;
        bool 이동클릭 = 지도이동클릭대기 && !지도드래그됨(입력.localPosition)
            && 지도.contentRect.Contains(입력.localPosition)
            && 전투중 && !패널열림 && !주민패널.열림 && 배치 < 0;
        지도누름해제();
        if (이동클릭) 선택부대명령(지도전장좌표(입력.localPosition), 지도입력: true);
    }

    void 지도취소(PointerCancelEvent 입력)
    {
        if (지도포인터 == 입력.pointerId) 지도누름해제();
    }

    void 지도캡처해제(PointerCaptureOutEvent 입력)
    {
        if (지도포인터 == 입력.pointerId) 지도누름해제();
    }

    void 지도누름해제()
    {
        if (!지도포인터.HasValue) return;
        int 포인터 = 지도포인터.Value;
        지도포인터 = null;
        if (!지도이동클릭대기 && !지도시점고정)
            전장카메라.transform.position = 카메라한계(내왕().위치 + 지도누르기전왕기준시점);
        지도이동클릭대기 = false;
        지도시점고정 = false;
        if (지도.HasPointerCapture(포인터)) 지도.ReleasePointer(포인터);
    }

    void 지도시점이동(Vector2 점) => 전장카메라.transform.position = 카메라한계(지도전장좌표(점));

    Vector2 지도전장좌표(Vector2 점)
    {
        var 범위 = 지도.contentRect;
        float 가로 = Mathf.InverseLerp(범위.xMin, 범위.xMax, 점.x);
        float 세로 = Mathf.InverseLerp(범위.yMin, 범위.yMax, 점.y);
        var 전장 = 길찾기.전장범위;
        return new Vector2(Mathf.Lerp(전장.xMin, 전장.xMax, 가로), Mathf.Lerp(전장.yMax, 전장.yMin, 세로));
    }
}

