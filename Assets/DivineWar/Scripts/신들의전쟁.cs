using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

[RequireComponent(typeof(UIDocument))]
public sealed partial class 신들의전쟁 : MonoBehaviour
{
    [SerializeField] Camera 전장카메라;
    [SerializeField] AudioSource 효과음재생기;
    [SerializeField] AudioSource 배경음악재생기, 배경음악보조재생기;
    [SerializeField] AudioClip[] 일반음악, 행군음악, 추격음악, 전투음악;
    const float 전투음악전환전최소행군음악시간 = 8f;
    배경음악재생 음악;
    전장음악상황 음악상황;
    [SerializeField] AudioClip 방생성효과음, 전장입장효과음, 닫기효과음;
    [SerializeField] AudioClip 이동클릭효과음, 유닛선택효과음, 이동명령효과음, 건설완료효과음;
    [SerializeField] AudioClip 왕공격효과음;
    [SerializeField] AudioClip[] combatClips;
    int 마지막전투소리순번;
    float 타격흔들림세기 = .13f;
    [SerializeField] AudioClip 피격효과음;
    [SerializeField] AudioClip 나무채집효과음, 돌채집효과음;
    [SerializeField] AudioClip[] 왕발소리, 왕흙발소리, 왕진흙발소리;
    [SerializeField] SpriteRenderer[] 슬롯;
    [SerializeField] Sprite[] 왕대기프레임, 왕이동프레임, 왕공격프레임;
    [SerializeField] Sprite[] 주민방향프레임, 주민동작프레임;
    [SerializeField] Sprite[] 병사동작프레임;
    [SerializeField] SpriteRenderer[] 짐승슬롯;
    [SerializeField] 시련표식[] 시련들;
    [SerializeField] 짐승그림[] 짐승그림들;
    [SerializeField] SpriteRenderer[] 모닥불슬롯;
    [SerializeField] SpriteRenderer[] 시설슬롯;
    [SerializeField] Sprite[] 시설그림;
    [SerializeField] 메뉴버튼[] 시설버튼;
    [SerializeField] Sprite[] 병과그림;
    [SerializeField] Sprite 나무울타리가로, 나무울타리세로, 돌울타리가로, 돌울타리세로;
    [SerializeField] SpriteRenderer 울타리표시프리팹;
    [SerializeField] SpriteRenderer 건설미리보기;
    [SerializeField] 자원지점[] 자원들;
    자원지점[] 전장자원들 => 지형.자원들;
    [SerializeField] Transform 장식지형;
    [SerializeField] Tilemap 바닥타일맵, 장애물타일맵, 지형타일맵;
    [SerializeField] TileBase[] 해변타일;
    [SerializeField] TileBase[] 흙타일;
    [SerializeField] SpriteRenderer 숲나무프리팹;
    [SerializeField] 전장지형.구역[] 전술지형;
    [SerializeField] Transform[] 시작지점들;

    [SerializeField] GraphicRaycaster 전장HUD;
    [SerializeField] GameObject 전투, 입장대기, 건축목록;
    [SerializeField] RectTransform 미니맵영역, 건설커서;
    [SerializeField] 메뉴버튼 건축버튼;
    [SerializeField] 메뉴버튼[] 행동버튼;
    [SerializeField] Image[] 건축미리보기;
    [SerializeField] CanvasGroup[] 스킬표시;
    [SerializeField] RawImage[] 스킬그림;
    [SerializeField] 스킬쿨다운테두리[] 스킬테두리;
    [SerializeField] Sprite 스킬설명프레임;
    [SerializeField] Sprite 주민패널그림, 주민패널구분선, 주민일반버튼, 주민강조버튼;
    [SerializeField] RectTransform 왕체력채움, 스태미나채움;
    [SerializeField] TMP_Text 자원표시, 시계, 내회관, 적회관, 왕상태, 왕직업글, 스태미나글, 알림, 도움, 돌진키표시;
    [SerializeField] TMP_Text 왕능력표시, 병력주민수;
    왕직업? 표시한왕직업;
    int 표시한유물 = -1;
    VisualElement 스킬설명상자;
    Label 스킬제목, 스킬설명글;
    Vector2 알림기준위치;
    Vector3 알림기준크기;
    int 표시한알림번호, 표시한모집초 = -1;
    float 알림등장시각, 알림종료시각;
    VisualElement 임무카드, 임무아이콘;
    Label 임무단계글, 임무제목, 임무설명;
    int 표시한임무 = -1;
    float 임무등장시각 = -1;

    const string 이동방식설정키 = "DivineWar.KeyboardMovement";
    const string 주민자동해제설정키 = "DivineWar.DeselectWorkersAfterOrder";
    const string 피해표시설정키 = "DivineWar.ShowDamageNumbers";
    const string 화면흔들림설정키 = "DivineWar.ScreenShake";
    const float 화면흔들림시간 = .16f;
    bool 피해표시;
    bool 화면흔들림;
    int 마지막적중순번;
    float 화면흔들림종료;
    Vector3 카메라흔들림편차;
    readonly List<(Vector2 위치, float 끝, string 숫자)> 피해숫자들 = new(32);
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
    int 조준스킬칸 = -1;
    KeyControl 조준스킬키;
    bool 스킬조준전장위;
    Material 하단안내재질;
    readonly 왕그림상태[] 왕그림들 = new 왕그림상태[2];
    readonly 주민그림상태[] 주민그림들 = new 주민그림상태[전장규칙.전체칸];
    readonly 병사그림상태[] 병사그림들 = new 병사그림상태[전장규칙.전체칸];
    readonly float[] 대상이전체력 = new float[전장규칙.전체칸];
    readonly int[] 짐승등장번호 = new int[전장짐승.전체칸];
    readonly float[] 짐승이전체력 = new float[전장짐승.전체칸];
    PointerEventData HUD포인터;
    Rect 미니맵범위;
    Texture2D 아군본진휘장, 적본진휘장, 범위룬, 급발진검그림, 작전상후퇴갈고리그림;
    int 표시모닥불칸 = -1, 표시모닥불번호;
    float 모닥불범위종료;
    VisualElement 뿌리, 전장표시, 지도;
    VisualElement 사망화면;
    Label 부활시간표시;
    int 표시한부활초 = -1;
    Label 병력선택안내;
    bool 드래그선택안내;
    격자길찾기 길찾기;
    전장지형 지형;
    전투메뉴 메뉴;
    주민추첨패널 주민패널;
    회관체력표시 회관체력;
    SpriteRenderer 호버윤곽, 왕목표윤곽;
    SpriteRenderer[] 유닛윤곽;
    Material 호버재질, 은신재질, 유닛선택재질, 왕목표재질, 갈고리포획재질;
    static readonly Color 갈고리포획색 = new Color32(222, 153, 247, 255);
    SpriteRenderer[] 자원그림;
    Color[] 자원원래색;
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
    int 카메라추적번호 = -1;
    int? 지도포인터;
    float 전송시간, 손님대기, 시뮬누적, 마지막상태시각, 최근상태간격 = .05f, 추정전장시간;
    readonly 보간이동[] 슬롯보간 = new 보간이동[전장규칙.전체칸];
    readonly 보간이동[] 짐승보간 = new 보간이동[전장짐승.전체칸];
    Vector2 누른위치, 현재마우스, 지도누른위치, 지도누르기전왕기준시점;
    Vector2 보낸이동방향;
    bool 보낸달리기;
    Vector2 채집전이동방향;
    float 이동방향재전송;
    Vector2 명령표시위치;
    float 명령표시시작 = float.NegativeInfinity;
    float 명령사거리반경;
    bool 지도에서명령, 지도끌기중, 지도더블클릭후보;
    const float 지도더블클릭간격 = .30f, 지도클릭여유 = 6f;
    (Vector2 패널점, Vector2 전장점, int 포인터, float 판정시각, int[] 부대)? 지도클릭대기;
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

    struct 보간이동
    {
        public Vector3 시작;
        public float 경과, 길이;
    }

    struct 왕그림상태
    {
        public bool 표시중, 채집중;
        public Vector2 위치;
        public int 방향;
        public float 이동여운, 이전공격대기, 공격여운, 동작시간;
        public Sprite[] 프레임;
        public int 이전공격순번;
    }

    struct 주민그림상태
    {
        public bool 표시중;
        public int 방향;
        public float 이동여운, 걷기거리, 튀는시간, 이전공격대기, 이전체력;
    }

    struct 병사그림상태
    {
        public int 방향, 이전공격순번;
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
        알림기준위치 = 알림.rectTransform.anchoredPosition;
        알림기준크기 = 알림.rectTransform.localScale;
        입장 = 전장입장.가져오기();
        연결 = 입장?.연결;
        멀티 = 연결 != null;
        내팀 = 입장?.내팀 ?? 0;
    }

    void OnEnable()
    {
        if (!Application.isEditor || 멀티) Application.runInBackground = true;
        if (전장입장.서버빌드)
        {
            GetComponent<UIDocument>().enabled = false;
            입장대기.SetActive(false);
            전투.SetActive(false);
            return;
        }
        뿌리 = GetComponent<UIDocument>().rootVisualElement;
        뿌리.style.unityFont = 도움.font.sourceFontFile;
        뿌리.StretchToParentSize();
        전장표시 = 뿌리.Q("draw");
        전장표시.style.unityFont = 도움.font.sourceFontFile;
        지도 = 뿌리.Q("minimap");
        if (안개 != null) 지도표시준비();
        범위룬 ??= Resources.Load<Texture2D>("range-rune");
        급발진검그림 ??= Resources.Load<Texture2D>("rapid-sword");
        작전상후퇴갈고리그림 ??= Resources.Load<Texture2D>("tactical-hook");
        아군본진휘장 ??= Resources.Load<Texture2D>("Minimap/allied-stronghold");
        적본진휘장 ??= Resources.Load<Texture2D>("Minimap/enemy-stronghold");
        사망화면 = 뿌리.Q("death-overlay");
        사망화면.style.unityFont = 도움.font.sourceFontFile;
        부활시간표시 = 사망화면.Q<Label>("respawn-countdown");
        표시한부활초 = -1;
        병력선택안내 = 뿌리.Q<Label>("army-selection-hint");
        병력선택안내.style.unityFont = 도움.font.sourceFontFile;
        표시(병력선택안내, false);
        스킬설명상자 = 뿌리.Q("battle-skill-tooltip");
        스킬설명상자.style.unityFont = 도움.font.sourceFontFile;
        스킬제목 = 스킬설명상자.Q<Label>("battle-skill-title");
        스킬설명글 = 스킬설명상자.Q<Label>("battle-skill-detail");
        왕효과표시준비();
        임무카드 = 뿌리.Q("tutorial-mission");
        임무카드.style.unityFont = 도움.font.sourceFontFile;
        임무아이콘 = 임무카드.Q("tutorial-mission-icon");
        임무단계글 = 임무카드.Q<Label>("tutorial-mission-step");
        임무제목 = 임무카드.Q<Label>("tutorial-mission-title");
        임무설명 = 임무카드.Q<Label>("tutorial-mission-hint");
        표시한임무 = -1;
        채집아이콘들 ??= Array.ConvertAll(채집아이콘이름, 이름 => Resources.Load<Sprite>("HarvestIcons/" + 이름));
        선택훈련안내준비();
        군단버튼준비();
        건설표시준비();
        마지막왕능력 = null;
        고기그림들 ??= Array.ConvertAll(전장식량.아이콘이름, 이름 => Resources.Load<Sprite>("Food/" + 이름));
        식량창 = new 전장식량창(뿌리, 도움.font.sourceFontFile, 고기그림들,
            종류 => 명령전달(명령종류.고기굽기, 값: 종류),
            종류 => 명령전달(명령종류.고기사용, 값: 종류));
        건축목록표시(false);
        if (주민패널?.유효 != true)
            주민패널 = new 주민추첨패널((RectTransform)전투.transform, 도움.font, 주민추첨요청,
                주민패널그림, 스킬설명프레임, 주민패널구분선, 주민일반버튼, 주민강조버튼);
        foreach (var 테두리 in 스킬테두리)
            테두리.문구준비(도움.font);
        키보드이동 = PlayerPrefs.GetInt(이동방식설정키) == 1;
        주민자동해제 = PlayerPrefs.GetInt(주민자동해제설정키, 1) == 1;
        피해표시 = PlayerPrefs.GetInt(피해표시설정키, 1) != 0;
        화면흔들림 = PlayerPrefs.GetInt(화면흔들림설정키, 1) != 0;
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
        if (효과음재생기 != null)
        {
            if (방생성효과음 != null) 방생성효과음.LoadAudioData();
            if (전장입장효과음 != null) 전장입장효과음.LoadAudioData();
            if (닫기효과음 != null) 닫기효과음.LoadAudioData();
            이동클릭효과음?.LoadAudioData();
            유닛선택효과음?.LoadAudioData();
            이동명령효과음?.LoadAudioData();
            건설완료효과음?.LoadAudioData();
            if (왕공격효과음 != null) 왕공격효과음.LoadAudioData();
            if (피격효과음 != null) 피격효과음.LoadAudioData();
            if (나무채집효과음 != null) 나무채집효과음.LoadAudioData();
            if (돌채집효과음 != null) 돌채집효과음.LoadAudioData();
            발소리미리읽기(combatClips);
            발소리미리읽기(왕발소리);
            발소리미리읽기(왕흙발소리);
            발소리미리읽기(왕진흙발소리);
        }
        건설소리준비();
        효과음음량갱신(전투음량설정.효과음);
        울타리슬롯준비();
        if (짐승슬롯 != null)
            foreach (var 그림 in 짐승슬롯)
                if (그림 != null) 그림.enabled = false;
        재료표시준비();
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
            갈고리포획재질 = new Material(셰이더);
            갈고리포획재질.SetColor("_OutlineColor", 갈고리포획색);
            갈고리포획재질.SetFloat("_OutlineWidth", 2f);
            유닛윤곽 = new SpriteRenderer[슬롯.Length];
        }
        SceneManager.sceneLoaded += 씬준비;
        var 메뉴씬 = SceneManager.GetSceneByName("BattleMenu");
        if (메뉴씬.isLoaded) 씬준비(메뉴씬, LoadSceneMode.Additive);
        else SceneManager.LoadSceneAsync("BattleMenu", LoadSceneMode.Additive);

        if (멀티 && !연결.방주인) return;
        규칙준비();
        화면전환();
    }

    void 규칙준비()
    {
        int 시드 = UnityEngine.Random.Range(1, int.MaxValue);
        지도준비(시드);
        var 위치들 = Array.ConvertAll(시작지점들, 지점 => (Vector2)지점.position);
        규칙 = new 전장규칙(전장자원들, 길찾기, 위치들, 입장?.청록직업 ?? 왕직업.지휘관,
            입장?.적색직업 ?? 왕직업.아처, 시드,
            입장?.청록장착 ?? 왕직업목록.기본장착, 입장?.적색장착 ?? 왕직업목록.기본장착,
            디버그연습전: !멀티 && !전장입장.서버빌드 && 입장?.디버그연습전 == true);
        규칙.시련배치(Array.ConvertAll(시련들 ?? Array.Empty<시련표식>(), 표식 => (Vector2)표식.transform.position));
        상태 = 규칙.상태;
    }

    void 지도준비(int 시드)
    {
        if (지형 != null) return;
        지형 = new 전장지형(바닥타일맵, 장애물타일맵, 지형타일맵, 해변타일, 흙타일, 자원들, 전술지형,
            !전장입장.서버빌드, 숲나무프리팹, 시드);
        길찾기 = new 격자길찾기(바닥타일맵, 장애물타일맵, 지형);
        if (전장입장.서버빌드) return;
        자원그림 = Array.ConvertAll(전장자원들, 지점 =>
            지점.오브젝트 != null ? 지점.오브젝트.GetComponent<SpriteRenderer>() : null);
        자원원래색 = Array.ConvertAll(자원그림, 그림 => 그림 != null ? 그림.color : Color.white);
        그림자들 = new 전장그림자(transform, 슬롯, new ArraySegment<SpriteRenderer>(자원그림, 0, 자원들.Length), 장식지형);
        안개 = new 전장안개(transform, 길찾기.전장범위, 지형);
        지도표시준비();
    }

    void 지도표시준비()
    {
        안개.보이기 = true;
        if (그림자들 != null) 그림자들.보이기 = true;
        미니맵범위 = default;
        지도.style.backgroundImage = new StyleBackground(안개.전장지도);
    }

    void 울타리슬롯준비()
    {
        Array.Resize(ref 슬롯, 전장규칙.전체칸);
        깃발슬롯준비();
        for (int i = 0; i < (시련들?.Length ?? 0); i++) 슬롯[전장규칙.시련보스번호(i)] = 시련들[i].보스그림;
        var 원본 = 슬롯[0];
        for (int i = 전장규칙.울타리시작칸; i < 전장규칙.모닥불시작칸; i++)
        {
            var 그림 = Instantiate(울타리표시프리팹, 원본.transform.parent);
            그림.name = $"울타리 {i - 전장규칙.울타리시작칸 + 1}";
            그림.gameObject.layer = 원본.gameObject.layer;
            그림.sharedMaterial = 원본.sharedMaterial;
            그림.sortingLayerID = 원본.sortingLayerID;
            슬롯[i] = 그림;
        }
        for (int 순번 = 0; 순번 < 전장규칙.모닥불한도; 순번++)
            for (int 팀 = 0; 팀 < 2; 팀++)
            {
                var 그림 = 모닥불슬롯[순번 * 2 + 팀];
                그림.enabled = false;
                슬롯[전장규칙.모닥불번호(팀, 순번)] = 그림;
            }
        for (int i = 0; i < 시설슬롯.Length; i++)
        {
            시설슬롯[i].enabled = false;
            슬롯[전장규칙.시설시작칸 + i] = 시설슬롯[i];
        }
        for (int i = 0; i < 시설버튼.Length; i++)
        {
            int 번호 = 전장규칙.브루저건설 + i;
            시설버튼[i].onClick.RemoveAllListeners();
            시설버튼[i].onClick.AddListener(() => 행동고르기(번호));
        }
    }

    void 씬준비(Scene 씬, LoadSceneMode 방식)
    {
        if (씬.name != "BattleMenu") return;
        foreach (var 오브젝트 in 씬.GetRootGameObjects())
        {
            if (!오브젝트.TryGetComponent(out 전투메뉴 전환메뉴)) continue;
            메뉴 = 전환메뉴;
            메뉴.효과음음량변경 += 효과음음량갱신;
            메뉴.재개 += 메뉴전환;
            메뉴.나가기 += 타이틀이동;
            메뉴.이동방식선택 += 이동방식변경;
            메뉴.주민자동해제전환 += 주민자동해제변경;
            메뉴.피해표시전환 += 피해표시변경;
            메뉴.피해표시갱신(피해표시);
            메뉴.화면흔들림전환 += 화면흔들림변경;
            메뉴.화면흔들림갱신(화면흔들림);
            메뉴.이동방식표시(키보드이동);
            메뉴.주민자동해제표시(주민자동해제);
            if (패널열림) 메뉴.보이기(팝업제목, 팝업본문, 팝업재개);
            else 메뉴.숨기기();
            break;
        }
    }

    void OnDisable()
    {
        음악?.정지();
        음악 = null;
        음악상황 = null;
        카메라흔들림제거();
        화면흔들림종료 = 0;
        스킬조준취소();
        if (스킬설명상자 != null) 표시(스킬설명상자, false);
        건설설명숨기기();
        if (왕효과줄 != null) 표시(왕효과줄, false);
        if (임무카드 != null) 표시(임무카드, false);
        디버그해제();
        건설소리해제();
        선택훈련안내해제();
        군단버튼해제();
        식량창?.Dispose();
        식량창 = null;
        주민패널?.닫기();
        Array.Clear(대상이전체력, 0, 대상이전체력.Length);
        Array.Clear(짐승등장번호, 0, 짐승등장번호.Length);
        Array.Clear(슬롯보간, 0, 슬롯보간.Length);
        Array.Clear(짐승보간, 0, 짐승보간.Length);
        마지막상태시각 = 0;
        최근상태간격 = .05f;
        추정전장시간 = 0;
        피해숫자들.Clear();
        if (채집그림들 != null)
        {
            foreach (var 효과 in 채집그림들)
                if (효과.그림 != null) Destroy(효과.그림.gameObject);
            채집그림들 = null;
        }
        채집전이동방향 = Vector2.zero;
        주민스탯번호 = -1;
        카메라추적번호 = -1;
        설명중인스킬 = -1;
        상태창?.숨기기();
        if (안개 != null) 안개.보이기 = false;
        if (그림자들 != null) 그림자들.보이기 = false;
        if (건설커서 != null) 망치커서표시(false);
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
            메뉴.효과음음량변경 -= 효과음음량갱신;
            메뉴.재개 -= 메뉴전환;
            메뉴.나가기 -= 타이틀이동;
            메뉴.이동방식선택 -= 이동방식변경;
            메뉴.주민자동해제전환 -= 주민자동해제변경;
            메뉴.피해표시전환 -= 피해표시변경;
            메뉴.화면흔들림전환 -= 화면흔들림변경;
            메뉴 = null;
        }
        연결?.Dispose();
        연결 = null;
    }

    void OnApplicationFocus(bool 집중)
    {
        if (!집중)
        {
            스킬조준취소();
            건설설명숨기기();
            지도누름해제();
            if (건설커서 != null) 망치커서표시(false);
        }
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
        if (갈고리포획재질 != null) Destroy(갈고리포획재질);
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
        var 시작효과음 = 멀티 ? 전장입장효과음 : 방생성효과음;
        if (효과음재생기 != null && 시작효과음 != null) 효과음재생기.PlayOneShot(시작효과음);
        회관체력 ??= new 회관체력표시((RectTransform)전투.transform, 스킬설명프레임,
            내회관, 적회관, 시계);
        전투중 = true;
        if (배경음악재생기 != null && 배경음악보조재생기 != null)
        {
            음악 = new 배경음악재생(배경음악재생기, 배경음악보조재생기);
            음악상황 = new 전장음악상황(상태, 내팀, 음악시야안);
        }
        디버그준비();
        마지막적중순번 = 상태.진영들[내팀].적중순번;
        입장대기.SetActive(false);
        전투.SetActive(true);
        if (하단안내재질 == null)
        {
            도움.outlineWidth = .2f;
            하단안내재질 = 도움.fontMaterial;
        }
        왕상태.outlineWidth = .15f;
        스태미나글.outlineWidth = .2f;
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
            if (전장입장.서버빌드)
            {
                Debug.Log("[DivineWar] MATCH_INTERRUPTED");
                타이틀이동();
            }
            else 연결끊김표시(연결?.오류 ?? "전장 연결이 끊겼습니다.");
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
        if (상태.승자 >= 0 && ((연결.방주인 || 결과확인[0]) && 결과확인[1] || (결과대기 += 초) > 15f))
        {
            if (연결.전용서버)
            {
                Debug.Log($"[DivineWar] MATCH_FINISHED winner={상태.승자}");
                타이틀이동();
            }
            else
            {
                연결.Dispose();
                연결 = null;
            }
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
            전송시간 = .04f;
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
        if (전투중 && 상태 != null)
            추정전장시간 = Mathf.Min(추정전장시간 + 초, 상태.시간 + .25f);
        string 마지막상태 = null;
        while (연결.받기(out int 팀, out string 문장))
            if (팀 == -1 && 문장.StartsWith("S", StringComparison.Ordinal)) 마지막상태 = 문장;
        if (마지막상태 != null)
        {
            try
            {
                var 받은상태 = JsonUtility.FromJson<전장상태>(마지막상태.Substring(1));
                if (받은상태?.대상들?.Length == 전장규칙.전체칸 && 받은상태.진영들?.Length == 2 && 받은상태.지도시드 > 0
                    && (상태 == null || 받은상태.지도시드 == 상태.지도시드))
                {
                    지도준비(받은상태.지도시드);
                    if (받은상태.자원잔량?.Length != 전장자원들.Length
                        || 받은상태.자원재생완료시각?.Length != 전장자원들.Length)
                    {
                        연결끊김표시("서버와 전장 지형이 일치하지 않습니다.");
                        return;
                    }
                    군단구성정리(받은상태);
                    float 지금 = Time.unscaledTime;
                    if (마지막상태시각 > 0)
                        최근상태간격 = Mathf.Lerp(최근상태간격, Mathf.Clamp(지금 - 마지막상태시각, .03f, .2f), .35f);
                    마지막상태시각 = 지금;
                    추정전장시간 = 받은상태.시간;
                    float 보간길이 = 최근상태간격 * 1.12f;
                    for (int i = 0; i < 슬롯.Length; i++)
                    {
                        var 대상 = 받은상태.대상들[i];
                        보간준비(ref 슬롯보간[i], 슬롯[i], 대상?.위치 ?? default,
                            대상 is { 생존: true, 훈련남음: <= 0 }, 보간길이);
                    }
                    if (짐승슬롯 != null && 받은상태.짐승들 != null)
                    {
                        int 수 = Mathf.Min(짐승슬롯.Length, Math.Min(짐승등장번호.Length, 받은상태.짐승들.Length));
                        for (int i = 0; i < 수; i++)
                        {
                            var 짐승 = 받은상태.짐승들[i];
                            보간준비(ref 짐승보간[i], 짐승슬롯[i], 짐승.위치,
                                짐승.등장번호 != 0 && !짐승.전리품 && 짐승등장번호[i] == 짐승.등장번호, 보간길이);
                        }
                    }
                    상태 = 받은상태;
                    for (int i = 자원들.Length; i < 전장자원들.Length; i++)
                        길찾기.숲나무반영(전장자원들[i].위치, 상태.자원잔량[i] > 0);
                    if (상태.승자 >= 0 && !결과확인[내팀]) 결과확인[내팀] = 연결.보내기("F");
                    if (!전투중) 화면전환();
                }
            }
            catch (ArgumentException) { }
        }
        if (상태?.승자 >= 0 || 연결종료.Length > 0) return;
        if (연결.오류 != null) 연결끊김표시(연결.오류);
        else if (!전투중 && (손님대기 += 초) > 12) 연결끊김표시("서버의 전장 정보를 받지 못했습니다.");
    }

    static void 보간준비(ref 보간이동 보간, SpriteRenderer 그림, Vector2 목표, bool 유효, float 길이)
    {
        if (유효 && 그림 != null && 그림.enabled && Vector2.Distance(그림.transform.position, 목표) <= 2.5f)
            보간 = new 보간이동 { 시작 = 그림.transform.position, 경과 = 0, 길이 = 길이 };
        else 보간 = default;
    }

    static Vector3 보간위치(ref 보간이동 보간, SpriteRenderer 그림, Vector3 현재, Vector3 목표, bool 즉시, float 초)
    {
        if (즉시 || !그림.enabled || Vector3.Distance(현재, 목표) > 2.5f)
        {
            보간 = default;
            return 목표;
        }
        if (보간.길이 <= 0) return Vector3.MoveTowards(현재, 목표, 초 * 6);
        보간.경과 += 초;
        return Vector3.Lerp(보간.시작, 목표, Mathf.Clamp01(보간.경과 / 보간.길이));
    }

    void 연결끊김표시(string 문장)
    {
        연결?.Dispose();
        연결 = null;
        if (상태?.승자 >= 0) return;
        연결종료 = 문장;
        팝업표시("경기 중단", 연결종료, false);
    }

    bool 명령전달(명령종류 종류, Vector2 위치 = default, int 값 = 0, int[] 부대 = null,
        bool 지도입력 = false, bool 세로 = false, int 주민모집인원 = 0, int 건물번호 = 0)
    {
        if (!전투중 || 상태.승자 >= 0 || 일시정지 || 연결종료.Length > 0 || 규칙?.디버그자동플레이 == true) return false;
        var 내용 = new 전장명령
        {
            종류 = 종류, 위치 = 위치, 값 = 값, 부대 = 부대, 세로 = 세로,
            주민모집인원 = 주민모집인원, 건물번호 = 건물번호
        };
        if (규칙 != null) 규칙.명령(내팀, 내용);
        else if (연결?.보내기("C" + JsonUtility.ToJson(내용)) != true) return false;
        if (종류 == 명령종류.부대이동) 음악상황?.이동명령기록(상태, 부대, 위치);

        if (종류 is 명령종류.이동 or 명령종류.공격 or 명령종류.채집 or 명령종류.왕채집 or 명령종류.부대이동 or 명령종류.건설 or 명령종류.사냥 or 명령종류.주민훈련)
        {
            if (이동명령효과음 != null && 종류 is 명령종류.이동 or 명령종류.부대이동)
                전장확인음재생(이동명령효과음, .36f, ref 다음이동명령음시각);
            else if (이동클릭효과음 != null && 효과음재생기 != null)
                효과음재생기.PlayOneShot(이동클릭효과음);
            명령표시위치 = 위치;
            명령표시시작 = Time.unscaledTime;
            명령사거리반경 = 사거리밖공격반경(내용);
            지도에서명령 = 지도입력;
        }

        if (주민자동해제 && 부대 != null && 종류 is 명령종류.부대이동 or 명령종류.채집)
        {
            foreach (int 번호 in 부대)
                if (상태.대상들[번호]?.종류 == 대상종류.주민) 선택부대.Remove(번호);
        }
        return true;
    }

    float 사거리밖공격반경(전장명령 명령)
    {
        if (명령.부대 is { Length: > 0 } || 명령.종류 is not (명령종류.공격 or 명령종류.사냥)) return 0;
        var 왕 = 내왕();
        if (!왕.생존) return 0;

        var 진영 = 상태.진영들[내팀];
        전장대상 표적 = null;
        Vector2 위치 = 명령.위치;
        if (명령.종류 == 명령종류.공격)
        {
            if (!전장규칙.공격가능(상태, 왕, 명령.값)) return 0;
            표적 = 상태.대상들[명령.값];
            위치 = 표적.위치;
        }
        else if (진영.직업 == 왕직업.아이번) return 0;

        float 반경 = 전장규칙.왕사거리(진영.직업, 전장규칙.왕공격최소거리(표적));
        return Vector2.Distance(왕.위치, 위치) > 반경 ? 반경 : 0;
    }

    bool 주민추첨요청(int 단계, int 인원) => 명령전달(명령종류.주민추첨, 값: 단계, 주민모집인원: 인원);

    void 주민패널열기()
    {
        if (규칙?.디버그자동플레이 == true) return;
        스킬조준취소();
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
        if (연결?.방주인 == true) 서버갱신(초);
        else 접속갱신(초);
        if (!전투중) return;

        var 키 = Keyboard.current;
        if (키 != null && 키.escapeKey.wasPressedThisFrame && !디버그도구취소())
        {
            if (효과음재생기 != null && 닫기효과음 != null) 효과음재생기.PlayOneShot(닫기효과음);
            if (조준스킬키 != null) 스킬조준취소();
            else if (주민패널.열림) 주민패널.닫기();
            else if (배치 >= 0 && !패널열림)
            {
                배치 = -1;
                망치커서표시(false);
            }
            else if (건축목록열림) 건축목록표시(false);
            else if (!패널열림 && 식량창.열림) 식량창.닫기();
            else if (!패널열림 && 선택부대.Count > 0) 선택부대.Clear();
            else 메뉴전환();
        }
        이동방향전송(이동방향읽기(키), 초, 달리기입력(키));
        if (!멀티 && 규칙 != null && !일시정지 && 상태.승자 < 0) 규칙진행(초, true);
        건설소리갱신();
        if (상태.승자 >= 0 && 연결종료.Length == 0)
        {
            string 제목 = 상태.승자 == 내팀 ? "승리" : "패배";
            if (!패널열림 || 팝업제목 != 제목)
                팝업표시(제목, 상태.승자 == 내팀 ? "적 마을회관을 무너뜨렸습니다." : "아군 마을회관이 함락됐습니다.", false);
        }

        탐사갱신();
        if (음악 != null)
        {
            var 적회관위치 = 상태.대상들[전장규칙.회관번호(1 - 내팀)].위치;
            var 상황 = 상태.승자 < 0 && 연결종료.Length == 0
                ? 음악상황.갱신(상태, 안개.탐사됨(적회관위치)) : 전장음악.일반;
            음악.갱신(상황 switch
            {
                전장음악.전투 => 전투음악,
                전장음악.추격 => 추격음악,
                전장음악.행군 => 행군음악,
                _ => 일반음악
            }, Time.unscaledDeltaTime, 상황 == 전장음악.전투 ? 행군음악 : null, 전투음악전환전최소행군음악시간);
        }
        카메라흔들림제거();
        if (!패널열림 && !주민패널.열림)
        {
            키입력읽기();
            카메라이동(초);
        }
        스킬키입력(키);
        if (!지도더블클릭후보 && 지도클릭대기 is { } 대기 && Time.unscaledTime > 대기.판정시각) 지도클릭확정();
        디버그패널표시갱신();
        장면반영(초);
        float 타격세기 = 전투소리갱신();
        카메라흔들림갱신(타격세기);
        int 호버번호 = 호버갱신();
        int 주민호버번호 = 호버번호 >= 0 && 상태.대상들[호버번호].종류 == 대상종류.주민 ? 호버번호 : -1;
        상호작용키입력(키, 주민호버번호);
        정보갱신(주민호버번호, 호버번호);
        선택훈련안내갱신();
        주민패널.갱신(상태, 내팀);
        전장표시.MarkDirtyRepaint();
        지도.MarkDirtyRepaint();
    }

    void 탐사갱신()
    {
        float 현재시간 = Time.unscaledTime;
        if (안개.갱신(상태.대상들, 내팀, 현재시간, 상태.지대들))
            for (int i = 0; i < 자원그림.Length; i++)
                if (자원그림[i] != null) 자원그림[i].forceRenderingOff = !안개.탐사됨(전장자원들[i].위치);
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
        스킬조준취소();
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
        스킬조준취소();
        키보드이동 = 새키보드이동;
        PlayerPrefs.SetInt(이동방식설정키, 키보드이동 ? 1 : 0);
        PlayerPrefs.Save();
        이동방식갱신();
    }

    void 이동방식갱신()
    {
        설명중인스킬 = -1;
        돌진키표시.text = 키보드이동 ? "F" : "W";
        디버그스킬키갱신();
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

    bool 달리기입력(Keyboard 키) => !패널열림 && !주민패널.열림 && Application.isFocused
        && 키 != null && 내왕().생존 && (키.leftShiftKey.isPressed || 키.rightShiftKey.isPressed);

    void 이동방향전송(Vector2 방향, float 초, bool 달리기 = false)
    {
        if (채집전이동방향 != Vector2.zero)
        {
            if (방향 == 채집전이동방향) return;
            채집전이동방향 = Vector2.zero;
        }
        if (방향 == 보낸이동방향 && 달리기 == 보낸달리기
            && (방향 == Vector2.zero && !달리기 || (이동방향재전송 -= 초) > 0)) return;
        if (!명령전달(명령종류.방향이동, 방향, 값: 달리기 ? 1 : 0)) return;
        보낸이동방향 = 방향;
        보낸달리기 = 달리기;
        이동방향재전송 = .12f;
    }

    public void 행동고르기(int 번호)
    {
        if (!전투중 || 패널열림 || 주민패널.열림 || 규칙?.디버그자동플레이 == true) return;
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
        if (!전투중 || 패널열림 || 규칙?.디버그자동플레이 == true) return;
        bool 열기 = !건축목록열림;
        if (!열기) 배치 = -1;
        건축목록표시(열기);
    }

    void 건축목록표시(bool 열기)
    {
        건축목록.SetActive(열기);
        건축버튼.선택표시(열기);
        if (!열기) 건설설명숨기기();
    }

    Sprite 건물그림(int 종류) => 종류 switch
    {
        전장규칙.나무울타리건설 => 울타리그림(대상종류.나무울타리, 배치세로),
        전장규칙.돌울타리건설 => 울타리그림(대상종류.돌울타리, 배치세로),
        전장규칙.모닥불건설 => 슬롯[전장규칙.모닥불번호(내팀)].sprite,
        전장규칙.깃발건설 => 깃발그림[내팀 * 8],
        >= 전장규칙.브루저건설 and <= 전장규칙.점프건설 => 시설그림[(종류 - 전장규칙.브루저건설) * 16 + 내팀 * 8],
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
        if (마우스.rightButton.wasPressedThisFrame && 전장가리킴(현재마우스))
        {
            if (배치 < 0 && 키?.ctrlKey.isPressed == true && 디버그도구 == 디버그위치도구.없음)
            {
                var (짐승, 번호, _) = 전장클릭대상(위치);
                if (짐승 < 0 && 번호 >= 0 && 전장규칙.철거가능(상태.대상들[번호], 내팀))
                {
                    var 건물 = 상태.대상들[번호];
                    명령전달(명령종류.건물철거, 건물.위치, 번호, 건물번호: 건물.건물번호);
                }
            }
            else 우클릭명령(위치);
        }
        if (키 != null)
        {
            if (병과선택단축키(키)) return;
            if (키.bKey.wasPressedThisFrame) 행동고르기(전장규칙.깃발건설);
            if (키.tabKey.wasPressedThisFrame) 병력선택(true);
            if (키.spaceKey.wasPressedThisFrame) 시점복귀();
            if (키.digit1Key.wasPressedThisFrame || 키.numpad1Key.wasPressedThisFrame) 군단선택(1);
            if (키.digit2Key.wasPressedThisFrame || 키.numpad2Key.wasPressedThisFrame) 군단선택(2);
            if (키.digit3Key.wasPressedThisFrame || 키.numpad3Key.wasPressedThisFrame) 군단선택(3);
            if (키.digit4Key.wasPressedThisFrame || 키.numpad4Key.wasPressedThisFrame) 군단선택(4);
            if (키.minusKey.wasPressedThisFrame) 행동고르기(전장규칙.지뢰건설);
            if (키.equalsKey.wasPressedThisFrame) 행동고르기(전장규칙.점프건설);
            if (키.iKey.wasPressedThisFrame) 식량창.전환();
            if (키.vKey.wasPressedThisFrame && 전장규칙.울타리건설인가(배치)) 배치세로 = !배치세로;
        }
    }

    void 스킬키입력(Keyboard 키)
    {
        var 마우스 = Mouse.current;
        if (키 == null || 마우스 == null || !Application.isFocused || 패널열림 || 주민패널.열림
            || 일시정지 || 상태.승자 >= 0 || !내왕().생존 || 키.escapeKey.wasPressedThisFrame
            || 디버그도구 != 디버그위치도구.없음 || 규칙?.디버그자동플레이 == true
            || (조준스킬키 != null && 조준스킬키.device != 키))
        {
            스킬조준취소();
            return;
        }

        var 진영 = 상태.진영들[내팀];
        if (조준스킬키 == null)
        {
            for (int 칸 = 0; 칸 < 왕직업목록.장착칸수; 칸++)
            {
                var 후보키 = 칸 switch { 0 => 키.qKey, 1 => 키보드이동 ? 키.fKey : 키.wKey, 2 => 키.eKey, _ => 키.rKey };
                if (!후보키.wasPressedThisFrame) continue;
                if (왕직업목록.장착가능(진영.직업)
                    && (진영.쿨다운[칸] > 0 || !직접시전기술(왕직업목록.장착스킬(진영, 칸)))) continue;
                조준스킬칸 = 칸;
                조준스킬키 = 후보키;
                break;
            }
        }

        if (조준스킬키 == null) return;
        Vector2 화면 = 마우스.position.ReadValue();
        var 방식 = 스킬조준방식(진영, 조준스킬칸);
        스킬조준전장위 = 방식 == 조준방식.자신 || 전장가리킴(화면);
        if (조준스킬키.wasReleasedThisFrame)
        {
            int 칸 = 조준스킬칸;
            bool 시전 = 스킬조준전장위;
            스킬조준취소();
            if (시전) 스킬입력(칸, 전장좌표(화면));
        }
        else if (!조준스킬키.isPressed) 스킬조준취소();
    }

    void 스킬조준취소()
    {
        조준스킬키 = null;
        조준스킬칸 = -1;
        스킬조준전장위 = false;
    }

    void 스킬입력(int 칸, Vector2 위치)
    {
        var 진영 = 상태.진영들[내팀];
        int[] 부대 = null;
        if (왕직업목록.장착가능(진영.직업) && 왕직업목록.장착스킬(진영, 칸) == 장착기술.집합)
        {
            부대 = new int[선택부대.Count];
            선택부대.CopyTo(부대);
        }
        명령전달(명령종류.스킬, 위치, 칸, 부대);
    }

    enum 조준방식 { 자신, 위치, 방향, 병사 }

    static bool 직접시전기술(장착기술 기술) => 왕직업목록.정보(기술).쿨다운 > 0 && 기술 != 장착기술.복수오라;

    static 조준방식 스킬조준방식(진영상태 진영, int 칸)
    {
        if (!왕직업목록.장착가능(진영.직업))
        {
            if (!왕직업목록.신규(진영.직업)) return 칸 == 2 ? 조준방식.자신 : 조준방식.위치;
            return 왕직업목록.스킬(진영.직업, 칸).사거리 > 0 ? 조준방식.위치 : 조준방식.자신;
        }

        return 왕직업목록.장착스킬(진영, 칸) switch
        {
            장착기술.집합 or 장착기술.잠복 => 조준방식.위치,
            장착기술.내가지킨다 or 장착기술.동료의복수 => 조준방식.병사,
            장착기술.회전베기 or 장착기술.앞구르기 or 장착기술.급발진 or 장착기술.꿰뚫기
                or 장착기술.납치 or 장착기술.출혈화살 or 장착기술.관절빼서공격하기 => 조준방식.방향,
            _ => 조준방식.자신
        };
    }

    void 상호작용키입력(Keyboard 키, int 주민호버번호)
    {
        if (주민스탯번호 != 주민호버번호 && !(상태창?.버튼가리킴(현재마우스) ?? false)) 주민스탯번호 = -1;
        if (키 == null || !키.gKey.wasPressedThisFrame || 패널열림 || 주민패널.열림
            || 배치 >= 0 || !Application.isFocused || Mouse.current == null) return;

        int 시련번호 = 가까운봉인시련();
        if (시련번호 >= 0)
        {
            명령전달(명령종류.시련활성화, 상태.시련들[시련번호].위치, 시련번호);
            return;
        }
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
        if (!전장가리킴(현재마우스)) return;
        if (입력.button == 0)
        {
            드래그중 = true;
            누른위치 = 현재마우스;
            전장표시.CapturePointer(입력.pointerId);
        }
    }

    void 우클릭명령(Vector2 위치, bool 지도입력 = false)
    {
        if (디버그도구취소()) return;
        if (규칙?.디버그자동플레이 == true) return;
        if (배치 >= 0)
        {
            배치 = -1;
            망치커서표시(false);
            return;
        }
        if (!지도입력 && (상호작용(0) || 상호작용(클릭여유))) return;
        if (!키보드이동) 명령전달(명령종류.이동, 위치, 지도입력: 지도입력);

        bool 상호작용(float 여유) => 시련클릭(위치) || 사냥명령(짐승가리킴(위치, 여유, 사냥대상만: true))
            || 훈련소사용(클릭대상(위치, 여유))
            || 모닥불사용(클릭대상(위치, 여유), 이동허용: true) || 왕채집명령(채집대상(위치, 여유));
    }

    bool 왕채집명령(int 번호)
    {
        if (번호 < 0 || 상태.자원잔량[번호] <= 0 || !내왕().생존) return false;
        if (!명령전달(명령종류.왕채집, 전장자원들[번호].위치, 번호)) return false;
        if (보낸이동방향 != Vector2.zero) 채집전이동방향 = 보낸이동방향;
        보낸이동방향 = Vector2.zero;
        return true;
    }

    bool 왕채집접근가능(int 번호, float 거리제곱) => !전장자원들[번호].숲나무
        || 거리제곱 <= 전장규칙.채집거리 * 전장규칙.채집거리
        || 길찾기.채집인접칸있음(전장자원들[번호].위치);

    void 가까운자원왕채집()
    {
        Vector2 왕위치 = 내왕().위치;
        int 가장가까움 = -1;
        float 거리제곱 = 왕채집키거리 * 왕채집키거리;
        for (int i = 0; i < 전장자원들.Length; i++)
        {
            if (상태.자원잔량[i] <= 0) continue;
            Vector2 위치 = 전장자원들[i].위치;
            float 후보 = (왕위치 - 위치).sqrMagnitude;
            if (후보 >= 거리제곱 || !안개.탐사됨(위치)) continue;
            if (!왕채집접근가능(i, 후보)) continue;
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
        if (디버그전장클릭(위치)) return;
        if (배치 >= 0)
        {
            if (!안개.탐사됨(바닥타일맵.GetCellCenterWorld(바닥타일맵.WorldToCell(위치)))) return;
            bool 건설가능 = 전장규칙.건설가능(상태, 길찾기, 전장자원들, 내팀, 배치, 위치, out _);
            if (명령전달(명령종류.건설, 위치, 배치, 세로: 배치세로) && 건설가능) 건설소리시작();
            망치내려찍기();
            if (!전장규칙.울타리건설인가(배치)) 배치 = -1;
            return;
        }
        클릭(위치, Keyboard.current?.ctrlKey.isPressed == true);
    }

    void 전장캡처해제(PointerCaptureOutEvent 입력) => 드래그중 = false;

    void 범위선택(Rect 범위)
    {
        선택부대.Clear();
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || 대상.연습허수아비 || 대상.팀 != 내팀 || 대상.건물 || 대상.종류 == 대상종류.왕) continue;
            if (범위.Contains(전장카메라.WorldToScreenPoint(대상.위치 + Vector2.up * .35f))) 선택부대.Add(i);
        }
        드래그선택안내 = 선택부대.Count > 1;
        선택효과음재생();
    }

    bool 내병력인가(int 번호) => (uint)번호 < (uint)상태.대상들.Length
        && 상태.대상들[번호] is { 생존: true, 종류: 대상종류.병사, 연습허수아비: false } 대상 && 대상.팀 == 내팀;

    void 병력선택(bool 전체)
    {
        if (전체)
        {
            선택부대.Clear();
            for (int i = 0; i < 상태.대상들.Length; i++)
                if (내병력인가(i)) 선택부대.Add(i);
        }
        else 선택부대.RemoveWhere(번호 => !내병력인가(번호));
        선택효과음재생();
        드래그선택안내 = false;
        드래그중 = false;
        배치 = -1;
        건축목록표시(false);
    }

    void 병력선택안내갱신()
    {
        bool 병력있음 = false, 주민있음 = false;
        if (드래그선택안내 && 선택부대.Count > 1 && 배치 < 0 && !드래그중
            && !패널열림 && !주민패널.열림 && !일시정지 && 상태.승자 < 0)
            foreach (int 번호 in 선택부대)
            {
                if (내병력인가(번호)) 병력있음 = true;
                else if (상태.대상들[번호] is { 생존: true, 종류: 대상종류.주민 } 대상 && 대상.팀 == 내팀)
                    주민있음 = true;
            }
        표시(병력선택안내, 병력있음 && 주민있음);
    }

    void 클릭(Vector2 위치, bool 선택전환)
    {
        if (!선택전환 && 시련클릭(위치)) return;
        var (짐승번호, 대상번호, 자원) = 전장클릭대상(위치, 사냥대상만: true);
        if (선택전환)
        {
            if (짐승번호 < 0 && 대상번호 >= 0
                && 상태.대상들[대상번호] is { 연습허수아비: false, 종류: 대상종류.병사 or 대상종류.주민 } 아군
                && 아군.팀 == 내팀)
                유닛클릭선택(대상번호, true);
            return;
        }
        if (사냥명령(짐승번호)) return;
        if (대상번호 >= 0)
        {
            var 대상 = 상태.대상들[대상번호];
            if (대상.연습허수아비 && 대상.팀 == 내팀) return;
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
            if (내왕().치유화살남음 > 0 && 대상 != 내왕() && !대상.건물 && 대상.훈련남음 <= 0)
            {
                명령전달(명령종류.공격, 위치, 대상번호);
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
                유닛클릭선택(대상번호, false);
                return;
            }
        }
        if (선택부대.Count == 0)
        {
            if (자원 >= 0)
            {
                float 거리제곱 = (내왕().위치 - (Vector2)전장자원들[자원].위치).sqrMagnitude;
                if (거리제곱 < 왕채집키거리 * 왕채집키거리 && 왕채집접근가능(자원, 거리제곱))
                    왕채집명령(자원);
            }
            return;
        }

        선택부대명령(위치, 자원);
    }

    void 유닛클릭선택(int 번호, bool 선택전환)
    {
        if (선택전환 && 선택부대.Remove(번호)) return;
        if (!선택전환) 선택부대.Clear();
        선택부대.Add(번호);
        선택효과음재생();
    }

    (int 짐승, int 대상, int 자원) 전장클릭대상(Vector2 위치, bool 사냥대상만 = false)
    {
        var (직접, 근처, 적) = 클릭후보(위치, 클릭여유);
        int 짐승 = 짐승가리킴(위치, 0, 사냥대상만);
        if (짐승 >= 0 || (직접 >= 0 && !상태.대상들[직접].건물))
            return (짐승, 직접, -1);
        if (적 >= 0) return (-1, 적, -1);
        int 자원 = 채집대상(위치, 0);
        if (직접 >= 0 || 자원 >= 0) return (-1, 직접, 자원);
        return (짐승가리킴(위치, 클릭여유, 사냥대상만), 근처, 채집대상(위치, 클릭여유));
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
        if (번호 < 0 || 상태.대상들[번호] is not { 생존: true } 건물
            || 건물.팀 != 내팀 || (건물.종류 != 대상종류.훈련소 && !전장규칙.전문훈련소(건물))) return false;
        var 부대 = new List<int>();
        foreach (int 선택 in 선택부대)
            if (전장규칙.훈련가능유닛(내팀, 건물, 상태.대상들[선택])) 부대.Add(선택);
        부대.Sort();
        명령전달(명령종류.주민훈련, 건물.위치, 번호, 부대.ToArray());
        return true;
    }

    bool 모닥불사용(int 번호, bool 이동허용 = false)
    {
        if ((uint)번호 >= (uint)상태.대상들.Length) return false;
        var 모닥불 = 상태.대상들[번호];
        if (모닥불 is not { 종류: 대상종류.모닥불, 생존: true, 완공대기: <= 0 } || 모닥불.팀 != 내팀) return false;
        표시모닥불칸 = 번호;
        표시모닥불번호 = 모닥불.건물번호;
        모닥불범위종료 = Time.unscaledTime + 4f;
        식량창.열기();
        if (이동허용 && (내왕().위치 - 모닥불.위치).sqrMagnitude > 전장식량.조리거리 * 전장식량.조리거리)
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

    int 클릭대상(Vector2 위치, float 여유, bool 아군훈련소만 = false) => 클릭후보(위치, 여유, 아군훈련소만).근처;

    (int 직접, int 근처, int 적) 클릭후보(Vector2 위치, float 여유, bool 아군훈련소만 = false)
    {
        int 결과 = -1, 앞 = int.MinValue;
        int 직접 = -1, 직접앞 = int.MinValue;
        int 적유닛 = -1, 적유닛앞 = int.MinValue;
        float 허용거리 = 여유 * 여유;
        float 거리 = 허용거리, 적유닛거리 = .3f * .3f;
        float 탐색거리 = Mathf.Max(허용거리, 적유닛거리);
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            if (대상 == null || !대상.생존 || 그림 == null || !그림.enabled) continue;
            if (아군훈련소만 && (대상.팀 != 내팀 || 대상.종류 != 대상종류.훈련소 && !전장규칙.전문훈련소(대상))) continue;
            Vector2 간격 = 위치 - (Vector2)그림.transform.position;
            bool 모닥불 = 대상.종류 == 대상종류.모닥불;
            float 반너비 = 대상.울타리 ? (대상.세로 ? .42f : .57f) : 모닥불 ? .6f : 대상.건물 ? 1f : .42f;
            float 높이 = 대상.울타리 ? (대상.세로 ? 1.16f : .82f)
                : 모닥불 ? 1f : 대상.건물 ? 1.9f : 대상.종류 == 대상종류.주민 ? 1.18f : 1f;
            if (대상.해골병사) { 반너비 *= 1.45f; 높이 *= 1.45f; }
            float 바닥 = 대상.울타리 && 대상.세로 ? -.55f : -.25f;
            float 후보 = 클릭거리제곱(간격, 반너비, 바닥, 높이);
            if (후보 > 탐색거리 || !대상표시(대상)) continue;
            int 순서 = 그림.sortingOrder;
            if (후보 == 0 && 순서 > 직접앞)
            {
                직접 = i;
                직접앞 = 순서;
            }
            if (후보 <= 허용거리 && (후보 < 거리 || (후보 == 거리 && 순서 > 앞)))
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
        return (직접, 결과, 적유닛);
    }

    int 채집대상(Vector2 위치, float 여유)
    {
        int 결과 = -1;
        float 거리 = float.PositiveInfinity;
        for (int i = 0; i < 전장자원들.Length; i++)
        {
            if (상태.자원잔량[i] <= 0 || !안개.탐사됨(전장자원들[i].위치)) continue;
            float 후보 = Vector2.Distance(위치, (Vector2)전장자원들[i].위치 + Vector2.up * .5f);
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
        return 번호;
    }

    void 왕목표윤곽갱신()
    {
        if (왕목표윤곽 == null) return;
        var 왕 = 내왕();
        SpriteRenderer 원본 = null;
        if (왕.생존 && 상태.승자 < 0)
        {
            if (왕.채집터 >= 0 && 왕.채집터 < 자원그림.Length) 원본 = 자원그림[왕.채집터];
            else if (왕.표적 >= 0 && 왕.표적 < 슬롯.Length && 상태.대상들[왕.표적] is { 생존: true } 대상
                && 대상.갈고리포획종료 <= 상태.시간)
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
        if (!지도포인터.HasValue)
        {
            카메라추적번호 = -1;
            자동추적 = true;
        }
    }

    void 유닛추적전환(int 번호)
    {
        if (!전투중 || 패널열림 || 주민패널.열림 || 배치 >= 0 || !유닛추적가능(번호)) return;
        bool 해제 = 자동추적 && 카메라추적번호 == 번호;
        카메라추적번호 = 해제 ? -1 : 번호;
        자동추적 = !해제;
    }

    bool 유닛추적가능(int 번호) => 번호 >= 0 && 번호 < 상태.대상들.Length
        && 상태.대상들[번호] is { 생존: true, 종류: 대상종류.주민 or 대상종류.병사 } 대상
        && 대상표시(대상);

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
        if (방향 != Vector2.zero || 카메라추적번호 >= 0 && !유닛추적가능(카메라추적번호))
        {
            자동추적 = false;
            카메라추적번호 = -1;
        }
        int 추적대상 = 카메라추적번호 >= 0 ? 카메라추적번호 : 전장규칙.왕번호(내팀);
        var 추적위치 = 슬롯[추적대상] != null && 슬롯[추적대상].enabled
            ? (Vector2)슬롯[추적대상].transform.position
            : 상태.대상들[추적대상].위치;
        var 목표 = 자동추적 ? 추적위치 : (Vector2)현재 + 방향.normalized * (11 * 초);
        전장카메라.transform.position = 자동추적
            ? Vector3.Lerp(현재, 카메라한계(목표), 1 - Mathf.Exp(-9 * 초))
            : 카메라한계(목표);
    }

    bool 대상표시(전장대상 대상) =>
        대상.연습허수아비 || (안개.탐사됨(대상.위치) || 정찰범위(대상.위치)) && 직업대상보임(대상);

    bool 음악시야안(전장대상 대상)
    {
        var 위치 = 전장카메라.WorldToViewportPoint(대상.위치);
        return 위치.z > 0 && 위치.x >= 0 && 위치.x <= 1 && 위치.y >= 0 && 위치.y <= 1
            && (대상.팀 == 내팀 || 미니맵대상표시(대상));
    }

    bool 미니맵대상표시(전장대상 대상)
    {
        if (!대상표시(대상)) return false;
        if (대상.팀 == 내팀 || 정찰범위(대상.위치)) return true;
        // 탐사 기록과 별개로 현재 아군 시야 안의 적만 표시한다.
        foreach (var 아군 in 상태.대상들)
            if (아군 is { 생존: true, 울타리: false } && 아군.팀 == 내팀
                && (아군.위치 - 대상.위치).sqrMagnitude <= 전장안개.시야반경 * 전장안개.시야반경)
                return true;
        return false;
    }

    bool 정찰범위(Vector2 위치) => 전장규칙.정찰범위(상태, 내팀, 위치);

    bool 직업대상보임(전장대상 대상) => 전장규칙.전투대상보임(상태, 지형, 내팀, 대상);

    void 피해표시변경()
    {
        피해표시 = !피해표시;
        피해숫자들.Clear();
        PlayerPrefs.SetInt(피해표시설정키, 피해표시 ? 1 : 0);
        PlayerPrefs.Save();
        메뉴.피해표시갱신(피해표시);
    }

    void 화면흔들림변경()
    {
        화면흔들림 = !화면흔들림;
        화면흔들림종료 = 0;
        카메라흔들림제거();
        PlayerPrefs.SetInt(화면흔들림설정키, 화면흔들림 ? 1 : 0);
        PlayerPrefs.Save();
        메뉴.화면흔들림갱신(화면흔들림);
    }

    void 카메라흔들림제거()
    {
        if (카메라흔들림편차 == Vector3.zero) return;
        if (전장카메라 != null) 전장카메라.transform.position -= 카메라흔들림편차;
        카메라흔들림편차 = Vector3.zero;
    }

    void 카메라흔들림갱신(float 타격세기)
    {
        int 적중순번 = 상태.진영들[내팀].적중순번;
        bool 적중 = 적중순번 != 마지막적중순번;
        마지막적중순번 = 적중순번;
        if (!화면흔들림 || 패널열림 || 지도포인터.HasValue)
        {
            화면흔들림종료 = 0;
            return;
        }

        float 지금 = Time.unscaledTime;
        if (적중 || 타격세기 > 0)
        {
            타격흔들림세기 = Mathf.Max(.13f, 타격세기);
            화면흔들림종료 = 지금 + 화면흔들림시간;
        }
        float 남음 = (화면흔들림종료 - 지금) / 화면흔들림시간;
        if (남음 <= 0) return;
        float 세기 = 타격흔들림세기 * 남음 * 남음;
        카메라흔들림편차 = new Vector3(Mathf.Cos(지금 * 133), Mathf.Sin(지금 * 107) * .65f, 0) * 세기;
        전장카메라.transform.position += 카메라흔들림편차;
    }

    float 전투소리갱신()
    {
        int 최신순번 = 마지막전투소리순번;
        float 타격세기 = 0;
        foreach (var 효과 in 상태.지대들)
        {
            if (효과.소리순번 <= 마지막전투소리순번) continue;
            최신순번 = Mathf.Max(최신순번, 효과.소리순번);
            if (일시정지) continue;
            if (효과.팀 == 내팀)
                타격세기 = Mathf.Max(타격세기, 효과.소리 switch
                {
                    전투소리.후퇴베기 => .28f,
                    전투소리.관절 when 효과.적중수 > 0 => Mathf.Min(.32f, .22f + .025f * 효과.적중수),
                    전투소리.강타 => .23f,
                    전투소리.암살자평타 => .19f,
                    _ => 0
                });
            else if (!안개.탐사됨(효과.위치) || !슬롯[전장규칙.왕번호(효과.팀)].enabled) continue;
            int 번호 = (int)효과.소리;
            var 소리 = 효과.소리 == 전투소리.멀티샷발사 ? 왕공격효과음
                : combatClips != null && (uint)번호 < combatClips.Length ? combatClips[번호] : null;
            if (효과음재생기 == null || 소리 == null) continue;
            float 음량 = 효과.소리 switch
            {
                전투소리.회전적중 => .62f,
                전투소리.연속공격 => .55f,
                전투소리.멀티샷발사 => .62f,
                전투소리.암살자평타 => .92f,
                _ => .85f
            };
            효과음재생기.PlayOneShot(소리, 효과.팀 == 내팀 ? 음량 : 음량 * .62f);
        }
        마지막전투소리순번 = 최신순번;
        return 타격세기;
    }

    void 장면반영(float 초)
    {
        지형.수풀투명도갱신(전장규칙.수풀시야중심(상태, 지형, 내팀), 초);
        자원그림반영();
        짐승그림갱신(초);
        시련그림갱신();
        bool 피격음재생 = false;
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            if (그림 == null) continue;
            bool 생존 = 대상 is { 생존: true, 훈련남음: <= 0 };
            bool 주둔 = 대상 is { 타워주둔: true };
            float 체력 = 주둔 ? 0 : 대상?.체력 ?? 0;
            float 감소 = 대상이전체력[i] - 체력;
            bool 피격 = 대상 != null && !주둔 && 감소 > 0;
            대상이전체력[i] = 체력;
            bool 보임 = (생존 || 피격) && 대상표시(대상);
            if (피격 && 보임 && (!피격음재생 || 피해표시))
            {
                var 화면위치 = 전장카메라.WorldToViewportPoint(대상.위치);
                if (화면위치.z > 0 && 화면위치.x >= 0 && 화면위치.x <= 1 && 화면위치.y >= 0 && 화면위치.y <= 1)
                {
                    피격음재생 = true;
                    if (피해표시) 피해숫자추가(대상.위치 + Vector2.up * (대상.건물 ? 2.2f : 1.45f), 감소);
                }
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
            그림.transform.position = 보간위치(ref 슬롯보간[i], 그림, 현재, 목표, 규칙 != null, 초);
            Vector2 화면이동 = 그림.transform.position - 현재;
            if (대상.연습허수아비)
            {
                그림.sprite = 허수아비그림;
                그림.flipX = false;
            }
            else if (대상.종류 == 대상종류.주민 && 주민방향프레임?.Length == 16)
                주민그림갱신(i, 대상, 그림, 화면이동, 일시정지 ? 0 : 초);
            else if (대상.종류 == 대상종류.병사)
                병사그림갱신(i, 대상, 그림, 화면이동, 피격, 일시정지 ? 0 : 초);
            else 그림.flipX = !대상.건물 && 대상.왼쪽;
            float 표시시각 = 규칙 != null ? 상태.시간 : 추정전장시간;
            if (대상.울타리) 그림.sprite = 울타리그림(대상.종류, 대상.세로);
            if (대상.종류 == 대상종류.깃발) 그림.sprite = 깃발그림[대상.팀 * 8 + (int)(표시시각 * 4) % 2];
            if (전장규칙.시설인가(대상))
            {
                int 종류 = (int)대상.종류 - (int)대상종류.브루저훈련소;
                그림.sprite = 시설그림[종류 * 16 + 대상.팀 * 8];
            }
            float 도약남음 = 규칙 != null ? 대상.도약시간 : Mathf.Max(0, 대상.도약시간 - Mathf.Max(0, 추정전장시간 - 상태.시간));
            if (도약남음 > 0)
            {
                float 진행 = 1 - 도약남음 / .5f;
                그림.transform.position = Vector2.Lerp(대상.도약출발, 대상.위치, 진행) + Vector2.up * (Mathf.Sin(진행 * Mathf.PI) * 1.6f);
            }
            if (대상.소환 != 소환종류.없음)
            {
                var 소환외형 = 왕외형목록.가져오기()?.소환외형(대상.소환);
                if (소환외형 != null)
                {
                    var 방향 = 대상.표적 >= 0 && 상태.대상들[대상.표적] is { 생존: true } 표적
                        ? 표적.위치 - 대상.위치 : 대상.시선;
                    var 프레임 = 대상.공격대기 > .4f ? 소환외형.공격 : 대상.이동중 ? 소환외형.이동 : 소환외형.대기;
                    if (프레임?.Length == 120)
                        그림.sprite = 프레임[스프라이트방향(방향) * 15 + Mathf.Min(14, (int)(표시시각 % .9f / .9f * 15))];
                }
                그림.flipX = false;
            }
            if (대상.해골병사 || 대상.종류 == 대상종류.시련보스) 시련유닛그림(대상, 그림);
            그림.enabled = true;
            그림.sortingOrder = 전장지형.깊이순서(그림.transform.position.y);
            if (대상.종류 == 대상종류.왕) 왕그림갱신(대상, 그림, 화면이동, 일시정지 ? 0 : 초);
            그림.color = 대상.효과시간 > 0 ? new Color(1, .8f, .67f)
                : 대상.완공대기 > 0 ? new Color(1, 1, 1, .57f)
                : 대상.종류 == 대상종류.왕 ? Color.Lerp(Color.white, 대상.팀 == 0 ? 청록 : 적색, .18f) : Color.white;
            bool 은신중 = 대상.팀 == 내팀 && !대상.건물 && (지형.은신지형인가(대상.위치)
                || 대상.종류 == 대상종류.왕 && 대상.은신 && 상태.진영들[대상.팀].직업 == 왕직업.암살자);
            if (은신중) 그림.color *= new Color(1, 1, 1, .42f);
            if (유닛윤곽 == null) continue;
            bool 유닛선택 = (대상.종류 is 대상종류.주민 or 대상종류.병사) && 선택부대.Contains(i);
            bool 갈고리포획 = 생존 && 대상.갈고리포획종료 > 상태.시간;
            if (갈고리포획 || 은신중 || 유닛선택)
            {
                var 재질 = 갈고리포획 ? 갈고리포획재질 : 은신중 ? 은신재질 : 유닛선택재질;
                if (유닛윤곽[i] == null) 유닛윤곽[i] = 윤곽생성("유닛 윤곽", 재질);
                else if (유닛윤곽[i].sharedMaterial != 재질) 유닛윤곽[i].sharedMaterial = 재질;
                윤곽갱신(유닛윤곽[i], 그림, 갈고리포획 || 은신중 ? 1 : -1);
            }
            else if (유닛윤곽[i] != null) 유닛윤곽[i].enabled = false;
        }
        while (피해숫자들.Count > 0 && 피해숫자들[0].끝 <= 상태.시간) 피해숫자들.RemoveAt(0);
        if (피격음재생 && !일시정지 && 피격효과음 != null && 효과음재생기 != null)
            효과음재생기.PlayOneShot(피격효과음);
        왕목표윤곽갱신();
        그림자들?.반영(상태.대상들, 안개);
        건설미리보기갱신();
    }

    void 피해숫자추가(Vector2 위치, float 피해)
    {
        if (피해숫자들.Count == 32) 피해숫자들.RemoveAt(0);
        피해숫자들.Add((위치, 상태.시간 + .7f, Mathf.CeilToInt(피해).ToString()));
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

            float 감소 = 짐승이전체력[i] - 짐승.체력;
            if (피해표시 && 감소 > 0 && 그림.isVisible && 짐승등장번호[i] == 짐승.등장번호)
                피해숫자추가(짐승.위치 + Vector2.up, 감소);
            짐승이전체력[i] = 짐승.체력;
            if (짐승.전리품)
            {
                int 고기 = 짐승.종류 == 전장짐승.돼지 ? (int)고기종류.날돼지고기 : (int)고기종류.날닭고기;
                그림.transform.position = 짐승.위치 + Vector2.up * .19f;
                그림.sprite = 고기그림들[고기];
                그림.sortingOrder = 전장지형.깊이순서(짐승.위치.y) + 2;
                그림.enabled = true;
                continue;
            }

            var 방향프레임 = 짐승그림들[짐승.종류].방향프레임;
            if (방향프레임 == null || (uint)짐승.방향 >= (uint)방향프레임.Length)
            {
                그림.enabled = false;
                continue;
            }
            그림.transform.position = 보간위치(ref 짐승보간[i], 그림, 그림.transform.position,
                짐승.위치, 규칙 != null || 짐승등장번호[i] != 짐승.등장번호, 초);
            짐승등장번호[i] = 짐승.등장번호;
            그림.sprite = 방향프레임[짐승.방향];
            그림.sortingOrder = 전장지형.깊이순서(그림.transform.position.y);
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
            if (채집터 >= 0 && 채집터 < 전장자원들.Length && 주민.공격대기 > 동작.이전공격대기 + .12f)
            {
                var 채집방향 = (Vector2)전장자원들[채집터].위치 - 주민.위치;
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
        if (병사.병과 != 병과.일반 && 병과그림?.Length == 32)
        {
            var 방향 = 병사.표적 >= 0 && 상태.대상들[병사.표적] is { 생존: true } 적 ? 적.위치 - 병사.위치 : 병사.시선;
            그림.sprite = 병과그림[(병사.병과 == 병과.아처 ? 16 : 0) + 병사.팀 * 8 + 스프라이트방향(방향)];
            그림.flipX = false;
            return;
        }
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
            동작 = new 병사그림상태 { 방향 = 병사.왼쪽 ? 4 : 0, 이전공격순번 = 병사.공격순번 };
        else
        {
            동작.공격여운 = Mathf.Max(0, 동작.공격여운 - 초);
            동작.피격여운 = 피격 ? 피격길이 : Mathf.Max(0, 동작.피격여운 - 초);
            if (이동.sqrMagnitude > .000001f)
            {
                if (동작.이동여운 <= 0) 동작.걷기거리 = 0;
                동작.이동여운 = 규칙 != null ? .12f : Mathf.Max(.16f, 최근상태간격 * 1.6f);
                동작.걷기거리 = Mathf.Repeat(동작.걷기거리 + 이동.magnitude, 보폭주기거리);
                if (동작.공격여운 <= 0) 동작.방향 = 스프라이트방향(이동);
            }
            else 동작.이동여운 = Mathf.Max(0, 동작.이동여운 - 초);

            if (병사.공격순번 != 동작.이전공격순번 || 병사.공격대기 > 0 && 병사.공격대기 > 동작.이전공격대기 + .12f)
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
        동작.이전공격순번 = 병사.공격순번;
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

    void 왕그림갱신(전장대상 왕, SpriteRenderer 그림, Vector2 화면이동, float 초)
    {
        var 외형 = 왕외형목록.가져오기()?.표시외형(상태.진영들[왕.팀].직업);
        var 대기프레임 = 외형?.대기 ?? 왕대기프레임;
        var 이동프레임 = 외형?.이동 ?? 왕이동프레임;
        var 공격프레임 = 외형?.공격 ?? 왕공격프레임;
        const int 방향당프레임 = 15;
        const int 전체프레임 = 8 * 방향당프레임;
        bool 공격취소 = 왕.왕공격동작길이 <= 0;
        float 공격길이 = !공격취소 ? 왕.왕공격동작길이 : .55f;
        if (대기프레임?.Length != 전체프레임 || 이동프레임?.Length != 전체프레임
            || 공격프레임?.Length != 전체프레임) return;

        Vector2 채집방향 = Vector2.zero;
        bool 채집중 = false;
        if (!왕.이동중 && 왕.채집터 >= 0 && 왕.채집터 < 전장자원들.Length)
        {
            채집방향 = (Vector2)전장자원들[왕.채집터].위치 - 왕.위치;
            채집중 = 채집방향.sqrMagnitude <= 전장규칙.채집거리 * 전장규칙.채집거리;
        }

        bool 공격시작 = false;
        ref var 동작 = ref 왕그림들[왕.팀];
        if (동작.표시중)
        {
            var 이동 = 왕.위치 - 동작.위치;
            if (이동.sqrMagnitude <= .000001f) 이동 = 화면이동;
            동작.이동여운 = Mathf.Max(0, 동작.이동여운 - 초);
            동작.공격여운 = Mathf.Max(0, 동작.공격여운 - 초);
            if (이동.sqrMagnitude > .000001f || 왕.이동중)
            {
                // 시뮬레이션과 네트워크 위치 갱신 사이에도 달리기를 이어간다.
                동작.이동여운 = 규칙 != null ? .12f : Mathf.Max(.16f, 최근상태간격 * 1.6f);
                if (이동.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(이동);
            }
            if (왕.채집터 < 0 && !동작.채집중 && 왕.공격순번 != 동작.이전공격순번)
            {
                if (!공격취소)
                {
                    동작.공격여운 = 공격길이;
                    공격시작 = true;
                    if (왕.공격방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(왕.공격방향);
                }
            }
            if (공격취소) 동작.공격여운 = 0;
        }
        else
        {
            동작.표시중 = true;
            동작.방향 = 왕.왼쪽 ? 4 : 0;
        }
        동작.위치 = 왕.위치;
        동작.이전공격대기 = 왕.공격대기;
        동작.이전공격순번 = 왕.공격순번;

        if (채집중)
        {
            동작.공격여운 = 0;
            if (채집방향.sqrMagnitude > .000001f) 동작.방향 = 스프라이트방향(채집방향);
        }
        bool 공격중 = 동작.공격여운 > 0 && (규칙 == null || 상태.시간 < 왕.공격시각 + 공격길이);
        bool 이동중 = 동작.이동여운 > 0;
        float 표시시각 = 규칙 != null ? 상태.시간 : 추정전장시간;
        float 기술초 = 표시시각 - 왕.왕기술시각;
        float 기술길이 = 왕직업목록.기술동작길이(왕.왕기술동작);
        bool 회전중 = 왕.왕기술동작 == 장착기술.회전베기;
        var 기술프레임 = 회전중 || 기술초 < 기술길이 ? 왕외형목록.검사기술외형(왕.왕기술동작) : null;
        bool 기술중 = 기술프레임?.Length == 전체프레임;
        var 프레임 = 기술중 ? 기술프레임 : 채집중 || 공격중 ? 공격프레임 : 이동중 ? 이동프레임 : 대기프레임;
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

        if (왕.팀 == 내팀 && 프레임 == 이동프레임 && 다음시간 > 이전시간)
        {
            // 달리기 스프라이트에서 양발이 차례로 땅에 닿는 프레임이다.
            왕발소리재생(왕.위치, 이전시간, 다음시간, 반복길이, 3f / 방향당프레임);
            왕발소리재생(왕.위치, 이전시간, 다음시간, 반복길이, 10f / 방향당프레임);
        }
        동작.동작시간 = 다음시간 % 반복길이;
        동작.채집중 = 채집중;
        동작.프레임 = 프레임;
        float 진행률 = 기술중 ? (회전중 ? Mathf.Repeat(기술초 / 기술길이, 1) : Mathf.Clamp01(기술초 / 기술길이))
            : 공격중 ? 1 - 동작.공격여운 / 공격길이 : 동작.동작시간 / 반복길이;
        if (기술중) 동작.방향 = 스프라이트방향(왕.시선);
        int 번호 = Mathf.Min((int)(진행률 * 방향당프레임), 방향당프레임 - 1);
        그림.sprite = 프레임[동작.방향 * 방향당프레임 + 번호];
        그림.flipX = false;
    }

    void 채집효과음재생(int 채집터)
    {
        if (일시정지 || 효과음재생기 == null) return;
        var 소리 = 전장자원들[채집터].재화번호 == 0 ? 나무채집효과음 : 돌채집효과음;
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
        if (왕진흙발소리?.Length > 0 && 지형.습지인가(위치)) return 왕진흙발소리;
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
        if ((배치 < 0 && !망치타격중) || 패널열림 || 주민패널.열림 || !Application.isFocused || Mouse.current == null || 전장표시.panel == null)
        {
            건설미리보기.enabled = false;
            망치커서표시(false);
            return;
        }

        bool 전장위 = 전장가리킴(현재마우스);
        if (!전장위)
        {
            건설미리보기.enabled = false;
            망치커서표시(false);
            return;
        }

        망치커서갱신();
        건설미리보기.enabled = 배치 >= 0;
        if (배치 < 0) return;

        bool 가능 = 전장규칙.건설가능(상태, 길찾기, 전장자원들, 내팀, 배치, 전장좌표(현재마우스), out Vector2 위치);
        if (!안개.탐사됨(위치))
        {
            건설미리보기.enabled = false;
            return;
        }
        건설미리보기.sprite = 건물그림(배치);
        건설미리보기.transform.position = 위치;
        건설미리보기.color = 가능 ? new Color(.6f, 1, .85f, .58f) : new Color(1, .38f, .35f, .58f);
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
        군단막대갱신();
        디버그재료위치갱신();

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

        상태창 ??= new 유닛상태창((RectTransform)전투.transform, 도움.font, 주민패널그림, 유닛추적전환);
        상태창.갱신(번호, 대상, 슬롯[번호].bounds, 전장카메라, 자동추적 && 카메라추적번호 == 번호);
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
            행영역.anchoredPosition = new Vector2(재료기본여백, ((재료수량글.Length - 1) * .5f - i) * 행간격);
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

    void 사망화면갱신(bool 사망, float 남은시간)
    {
        int 초 = 사망 ? Mathf.Max(0, Mathf.CeilToInt(남은시간)) : -1;
        if (초 == 표시한부활초) return;
        if (사망) 부활시간표시.text = $"부활까지 {초}초";
        표시(사망화면, 사망);
        표시한부활초 = 초;
    }

    void 정보갱신(int 주민호버번호 = -1, int 호버번호 = -1)
    {
        건설수량갱신();
        var 팀 = 상태.진영들[내팀];
        식량창.갱신(팀, !패널열림 && !주민패널.열림);
        재료수량갱신(팀.재화);
        int 흐른초 = Mathf.FloorToInt(상태.시간);
        시계.text = $"{흐른초 / 60:00}:{흐른초 % 60:00}";
        var 아군회관 = 상태.대상들[전장규칙.회관번호(내팀)];
        var 상대회관 = 상태.대상들[전장규칙.회관번호(1 - 내팀)];
        회관체력.갱신(아군회관, 상대회관, 안개.탐사됨(상대회관.위치),
            전장규칙.회관피해감소율(상태, 내팀));
        if (병력주민수 != null)
            병력주민수.SetText("병력 {0:0}/{1:0}   주민 {2:0}/{3:0}",
                전장규칙.수량(상태, 내팀, 대상종류.병사) + 전장규칙.주둔아처수(상태, 내팀), 전장규칙.병력제한,
                전장규칙.수량(상태, 내팀, 대상종류.주민), 전장규칙.주민제한);
        var 왕 = 내왕();
        왕능력표시갱신(왕, 팀);
        float 체력비율 = 왕.생존 && 왕.최대체력 > 0 ? Mathf.Clamp01(왕.체력 / 왕.최대체력) : 0;
        왕체력채움.anchorMax = new Vector2(체력비율, 1);
        if (왕.생존) 왕상태.SetText("체력   {0:0} / {1:0}", Mathf.Ceil(왕.체력), 왕.최대체력);
        else 왕상태.SetText("왕 재등장   {0:0}초", Mathf.Max(0, Mathf.Ceil(왕.재등장)));
        float 스태미나 = 왕.생존 ? Mathf.Clamp(팀.스태미나, 0, 전장규칙.최대스태미나) : 0;
        스태미나채움.anchorMax = new Vector2(스태미나 / 전장규칙.최대스태미나, 1);
        스태미나글.SetText("스태미나   {0:0} / {1:0}   [Shift]", Mathf.Ceil(스태미나), 전장규칙.최대스태미나);
        if (표시한왕직업 != 팀.직업 || 표시한유물 != 팀.시련유물)
        {
            표시한왕직업 = 팀.직업;
            표시한유물 = 팀.시련유물;
            왕직업글.text = 팀.시련유물 > 0 ? $"{팀.직업}   ·   유물 {팀.시련유물}" : 팀.직업.ToString();
        }
        사망화면갱신(!왕.생존 && 상태.승자 < 0 && 연결종료.Length == 0 && !패널열림, 왕.재등장);
        임무카드갱신(팀);
        알림갱신(팀);
        왕효과표시갱신(팀, 왕);
        하단안내갱신(주민호버번호, 호버번호);
        if (깃발버튼 != null) 깃발버튼.선택표시(배치 == 전장규칙.깃발건설);
        for (int i = 0; i < 스킬테두리.Length; i++)
        {
            float 남은시간 = 팀.쿨다운[i];
            스킬표시[i].alpha = 남은시간 <= 0 && 왕.생존 ? 1 : .52f;
            var 기술 = 슬롯기술(팀, i);
            var 아이콘 = 왕직업목록.장착가능(팀.직업)
                ? 왕스킬아이콘목록.가져오기(왕직업목록.장착스킬(팀, i)) : null;
            스킬그림[i].enabled = 아이콘 != null;
            if (아이콘 != null && 스킬그림[i].texture != 아이콘.texture)
                스킬그림[i].texture = 아이콘.texture;
            스킬테두리[i].진행표시(남은시간, 기술.쿨다운, 왕.생존);
        }
        for (int i = 0; i < 행동버튼.Length; i++)
            if (행동버튼[i] != null) 행동버튼[i].선택표시(i == 배치);
        for (int i = 0; i < 시설버튼.Length; i++)
            시설버튼[i].선택표시(i + 전장규칙.브루저건설 == 배치);
    }

    void 임무카드갱신(진영상태 팀)
    {
        const int 튜토리얼임무수 = 4;
        if (상태.승자 >= 0)
        {
            if (표시한임무 >= 0) 표시(임무카드, false);
            표시한임무 = -1;
            return;
        }

        int 단계 = Mathf.Clamp(팀.심부름, 0, 튜토리얼임무수);
        if (표시한임무 != 단계)
        {
            var (제목, 설명, 그림) = 단계 switch
            {
                0 => ("자원 채집", "자원 우클릭 또는 가까이서 [G]", 채집아이콘들[0]),
                1 => ("제재소 건설", "건축 메뉴에서 제재소를 골라 회관 주변에 배치", 건물그림(0)),
                2 => ("병사 훈련", "주민을 선택하고 완공된 훈련소 클릭", 건물그림(1)),
                3 => ("병력 진격", "병사를 적 회관 근처로 이동", 슬롯[전장규칙.왕번호(내팀) + 전장규칙.병사시작칸].sprite),
                _ => ("적 회관 파괴", "병사를 선택하고 적 회관 클릭", 슬롯[전장규칙.회관번호(1 - 내팀)].sprite)
            };
            임무단계글.text = 단계 < 튜토리얼임무수 ? $"임무 {단계 + 1}/{튜토리얼임무수}" : "최종 목표";
            임무제목.text = 제목;
            임무설명.text = 설명;
            임무아이콘.style.backgroundImage = new StyleBackground(그림);
            표시(임무카드, true);
            표시한임무 = 단계;
            임무등장시각 = Time.unscaledTime;
        }

        if (임무등장시각 < 0) return;
        float 잔량 = 1 - Mathf.Clamp01((Time.unscaledTime - 임무등장시각) / .24f);
        임무카드.style.translate = new Translate(-14 * 잔량 * 잔량 * 잔량, 0);
        if (잔량 == 0) 임무등장시각 = -1;
    }

    void 알림갱신(진영상태 팀)
    {
        const float 등장시간 = .24f, 퇴장시간 = .48f;
        bool 메시지있음 = 팀.알림시간 > 0 && !string.IsNullOrEmpty(팀.알림);
        int 모집초 = !메시지있음 && 팀.모집종류 >= 0 ? Mathf.CeilToInt(팀.모집대기) : -1;
        if (!메시지있음 && 모집초 < 0)
        {
            if (알림.gameObject.activeSelf) 알림.gameObject.SetActive(false);
            return;
        }

        bool 새알림 = !알림.gameObject.activeSelf || (메시지있음
            ? 표시한모집초 >= 0 || 표시한알림번호 != 팀.알림번호 : 표시한모집초 < 0);
        float 지금 = Time.unscaledTime;
        float 남은시간 = 메시지있음 ? 팀.알림시간 : 팀.모집대기;
        if (새알림)
        {
            알림등장시각 = 지금;
            알림종료시각 = 지금 + 남은시간;
            알림.color = 메시지있음 && 팀.경고알림 ? new Color(1, .33f, .29f) : Color.white;
            if (메시지있음) 알림.text = 팀.알림;
        }
        if (!메시지있음 && (새알림 || 표시한모집초 != 모집초))
            알림.SetText("주민 모집 중   {0:0}초", 모집초);
        표시한알림번호 = 팀.알림번호;
        표시한모집초 = 모집초;

        float 등장 = Mathf.Clamp01((지금 - 알림등장시각) / 등장시간);
        float 이동잔량 = 1 - 등장;
        이동잔량 *= 이동잔량 * 이동잔량;
        if (멀티) 남은시간 = Mathf.Min(남은시간, 알림종료시각 - 지금);
        float 퇴장 = Mathf.SmoothStep(0, 1, 1 - 남은시간 / 퇴장시간);
        알림.alpha = Mathf.SmoothStep(0, 1, 등장) * (1 - 퇴장);
        var 영역 = 알림.rectTransform;
        영역.anchoredPosition = 알림기준위치 + Vector2.up * (-9 * 이동잔량 + 7 * 퇴장);
        영역.localScale = 알림기준크기 * (1 - .035f * 이동잔량);
        if (!알림.gameObject.activeSelf) 알림.gameObject.SetActive(true);
    }

    void 하단안내갱신(int 주민호버번호, int 호버번호)
    {
        병력선택안내갱신();
        if (패널열림 || 주민패널.열림)
        {
            설명중인스킬 = -1;
            표시(스킬설명상자, false);
            건설설명숨기기();
            도움.text = "";
            return;
        }
        스킬설명갱신();
        건설설명갱신();

        string 하단안내 = 전장규칙.울타리건설인가(배치)
            ? "좌클릭 연속 설치  ·  V 방향 전환  ·  우클릭 / Esc 취소  ·  회관 반경 8" :
            배치 >= 0 ? "좌클릭 설치  ·  우클릭 / Esc 취소  ·  회관 반경 8" :
            $"{(키보드이동 ? "WASD" : "우클릭")} 왕 이동  ·  G {(가까운봉인시련() >= 0 ? "시련 시작" : 주민호버번호 >= 0 ? "스탯 보기" : "왕 채집")}  ·  Tab 전체 병력";
        if (배치 < 0 && 선택부대.Count > 1) 하단안내 = $"{선택부대.Count}명 선택  ·  Z 주민  X 병사  C 브루저  V 아처  ·  Tab 전체 병력";
        if (배치 < 0 && 선택부대.Count == 1)
            하단안내 = "좌클릭 명령  ·  미니맵 클릭 이동  ·  Esc 선택 해제";
        if (호버번호 == 전장규칙.회관번호(내팀))
            하단안내 = $"회관 피해 감소 {전장규칙.회관피해감소율(상태, 내팀)}%  ·  완공 건물 3/6/9개에서 30/50/70%";
        else if (호버번호 >= 0 && 전장규칙.철거가능(상태.대상들[호버번호], 내팀))
            하단안내 = 상태.대상들[호버번호].종류 == 대상종류.모닥불
                ? "클릭하면 아군 병사 회복 범위 표시  ·  Ctrl + 우클릭 철거"
                : "Ctrl + 우클릭 건물 철거";
        도움.text = 배치 < 0 ? 하단안내 + "  ·  1~4 군단 선택" : 하단안내;
    }

    float 자원표시비율(int 번호)
    {
        if (상태.자원잔량[번호] > 0) return 1;
        return Mathf.Clamp01(1 - (상태.자원재생완료시각[번호] - 상태.시간) / 전장규칙.자원재생시간);
    }

    void 자원그림반영()
    {
        for (int i = 0; i < 자원그림.Length; i++)
        {
            var 그림 = 자원그림[i];
            if (그림 == null) continue;
            float 비율 = 자원표시비율(i);
            bool 표시중 = 비율 > 0;
            if (그림.enabled != 표시중) 그림.enabled = 표시중;
            if (!표시중) continue;
            var 색 = 자원원래색[i];
            색.a *= 비율;
            if (그림.color != 색) 그림.color = 색;
        }
    }

    void 자원게이지그리기(Painter2D 펜)
    {
        var 화면 = 전장표시.contentRect;
        for (int i = 0; i < 전장자원들.Length; i++)
        {
            var 그림 = 자원그림[i];
            if (그림 == null || !그림.enabled || 그림.forceRenderingOff) continue;
            var 자원 = 전장자원들[i];
            int 잔량 = 상태.자원잔량[i];
            if (잔량 >= 자원.채집한도) continue;
            var 위치 = 위치변환((Vector2)자원.위치 + Vector2.down * .13f);
            if (!화면.Contains(위치)) continue;
            float 비율 = 잔량 > 0 ? 잔량 / (float)자원.채집한도 : 자원표시비율(i);
            var 배경 = new Rect(위치.x - 13, 위치.y, 26, 5);
            네모(펜, 배경, new Color32(45, 31, 24, 240), new Color32(122, 85, 55, 255));
            Color 채움 = 잔량 > 0 ? new Color32(225, 185, 105, 255) : new Color32(140, 184, 113, 255);
            네모(펜, new Rect(배경.x + 1, 배경.y + 1, (배경.width - 2) * 비율, 배경.height - 2), 채움);
        }
    }

    void 채집그림갱신()
    {
        int[] 횟수들 = 상태.진영들[내팀].자원별채집횟수;
        if (횟수들?.Length != 전장자원들.Length) return;
        if (채집그림들 == null)
        {
            채집그림들 = new 채집그림상태[전장자원들.Length];
            for (int i = 0; i < 채집그림들.Length; i++) 채집그림들[i].횟수 = 횟수들[i];
            return;
        }

        float 지금 = Time.unscaledTime;
        var 부모 = (RectTransform)전투.transform;
        for (int i = 0; i < 채집그림들.Length; i++)
        {
            ref var 효과 = ref 채집그림들[i];
            if (횟수들[i] > 효과.횟수 && 안개.탐사됨(전장자원들[i].위치) && 자원그림[i]?.sprite != null)
            {
                var 자원 = 자원그림[i];
                var 범위 = 자원.bounds;
                효과.출발위치 = new Vector3(범위.center.x, 범위.max.y - .25f, 범위.center.z);
                효과.그림 ??= 채집이미지생성(부모, i);
                var 아이콘 = 채집아이콘들[전장자원들[i].재화번호];
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

    void 스킬설명갱신()
    {
        var 마우스 = Mouse.current;
        if (Application.isFocused && 마우스 != null && 뿌리.panel != null)
        {
            var 위치 = 마우스.position.ReadValue();
            if (왕효과설명표시(화면변환(위치))) return;
            설명중인효과 = null;
            for (int i = 0; i < 스킬테두리.Length; i++)
            {
                var 슬롯 = 스킬테두리[i].rectTransform;
                if (!슬롯.gameObject.activeInHierarchy
                    || !RectTransformUtility.RectangleContainsScreenPoint(슬롯, 위치)) continue;
                스킬설명표시(i);
                왕하단설명배치(왕패널위치().x);
                return;
            }
        }
        설명중인스킬 = -1;
        설명중인효과 = null;
        표시(스킬설명상자, false);
    }

    static 왕직업목록.기술 슬롯기술(진영상태 진영, int 번호)
    {
        if (왕직업목록.장착가능(진영.직업)) return 왕직업목록.정보(왕직업목록.장착스킬(진영, 번호));
        if (왕직업목록.신규(진영.직업)) return 왕직업목록.스킬(진영.직업, 번호);
        return new 왕직업목록.기술(전장규칙.스킬이름[번호], 전장규칙.스킬쿨다운길이(번호), 0, "");
    }

    void 스킬설명표시(int 번호)
    {
        if (설명중인스킬 == 번호) return;
        설명중인스킬 = 번호;
        string 키 = 왕직업목록.입력표시(번호, 키보드이동);
        var 진영 = 상태.진영들[내팀];
        var 직업 = 진영.직업;
        if (왕직업목록.장착가능(직업) || 왕직업목록.신규(직업))
        {
            var 기술 = 슬롯기술(진영, 번호);
            var 대기 = 기술.쿨다운 > 0 ? $"\n재사용 대기 시간 {기술.쿨다운}초" : "";
            스킬제목.text = $"{기술.이름}  [{키}]";
            스킬설명글.text = 기술.설명 + 대기;
            return;
        }
        string 효과 = 번호 switch
        {
            0 => "지정한 범위 안의 적에게 28.5의 기본 피해를 줍니다.",
            1 => "마우스 방향으로 돌진합니다. 추적자는 더 멀리 이동하고, 수호자는 3초간 방패를 얻습니다.",
            2 => "왕 주변의 아군 유닛을 회복합니다. 지능이 높을수록 더 많이 회복합니다.",
            _ => "지정한 넓은 범위 안의 적에게 66의 기본 피해를 줍니다. 지능이 높을수록 범위가 넓어집니다."
        };
        스킬제목.text = $"{전장규칙.스킬이름[번호]}  [{키}]";
        스킬설명글.text = $"{효과}\n재사용 대기 시간 {전장규칙.스킬쿨다운길이(번호)}초";
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

    void 장막그리기(Painter2D 펜, 스킬지대 장막)
    {
        var 색 = 장막.팀 == 내팀 ? new Color(.36f, .55f, .56f) : new Color(.49f, .36f, .45f);
        펜.BeginPath();
        for (int i = 0; i < 16; i++)
        {
            float 각 = i * (Mathf.PI / 8);
            var 점 = 위치변환(장막.위치 + new Vector2(Mathf.Cos(각), Mathf.Sin(각)) * 장막.반경);
            if (i == 0) 펜.MoveTo(점); else 펜.LineTo(점);
        }
        펜.ClosePath();
        펜.fillColor = new Color(색.r, 색.g, 색.b, .25f);
        펜.Fill();
        펜.strokeColor = 색;
        펜.lineWidth = 2;
        펜.Stroke();
    }

    Color 공격효과색(int 팀) => 팀 == 내팀 ? new Color(.22f, .91f, .88f) : new Color(1f, .31f, .30f);

    void 공격효과그리기(Painter2D 펜, 스킬지대 탄)
    {
        var 방향 = 탄.방향 == Vector2.zero ? Vector2.right : 탄.방향;
        var 높이 = Vector2.up * .62f;
        var 색 = 공격효과색(탄.팀);
        if (탄.종류 == 지대종류.근접타격)
        {
            if (탄.직업 == 왕직업.암살자 && 탄.대상 == 전장규칙.왕번호(탄.팀))
            {
                특수검격그리기(펜, 탄);
                return;
            }
            float 진행 = 1 - Mathf.Clamp01(탄.남은시간 / 탄.지속시간);
            var 중심 = 위치변환(탄.위치 + 높이);
            float 검격크기 = Mathf.Clamp(탄.반경 * 1.35f, 1.25f, 2.35f);
            float 반경 = Vector2.Distance(중심, 위치변환(탄.위치 + 높이 + Vector2.right * 검격크기));
            float 방향각 = Mathf.Atan2(-방향.y, 방향.x);
            float 밝기 = Mathf.Clamp01((1 - 진행) * 2.2f);
            if (탄.방향 == Vector2.zero && 탄.반경 >= 2)
            {
                파동그리기(펜, 탄.위치, 탄.반경 * Mathf.Lerp(.6f, 1, 진행), 밝기 * .48f, 색);
                return;
            }
            float 회전 = Mathf.Lerp(-.65f, .82f, 진행);
            검격궤적그리기(펜, 중심, 반경 * Mathf.Lerp(.88f, 1.12f, 진행) * (탄.강화 ? .88f : 1),
                방향각 + (탄.강화 ? -회전 : 회전), 색, 밝기);
            if (탄.적중수 > 0)
            {
                var 충돌 = 중심 + new Vector2(방향.x, -방향.y) * (반경 * .7f);
                var 불꽃색 = Color.Lerp(색, Color.white, .3f);
                불꽃색.a = Mathf.Clamp01(1 - 진행 * 1.6f);
                for (int i = -1; i <= 1; i++)
                {
                    float 각 = 방향각 + i * .63f;
                    var 광선 = new Vector2(Mathf.Cos(각), Mathf.Sin(각));
                    var 옆선 = new Vector2(-광선.y, 광선.x);
                    효과쐐기(펜, 충돌 + 광선 * (반경 * (.24f + .43f * 진행)),
                        충돌 + 광선 * (반경 * (.06f + .35f * 진행)), 옆선 * (반경 * .055f),
                        불꽃색);
                }
            }
            return;
        }
        var 앞 = 위치변환(탄.위치 + 높이);
        var 뒤 = 위치변환(탄.위치 + 높이 - 방향 * .32f);
        var 화면방향 = (앞 - 뒤).normalized;
        var 옆 = new Vector2(-화면방향.y, 화면방향.x);
        if (탄.공격종류 is 기본공격종류.화살 or 기본공격종류.모래)
        {
            펜.BeginPath(); 펜.MoveTo(뒤); 펜.LineTo(앞);
            펜.strokeColor = new Color(.08f, .13f, .16f, .9f); 펜.lineWidth = 4; 펜.Stroke();
            펜.BeginPath(); 펜.MoveTo(앞 + 화면방향 * 4); 펜.LineTo(앞 - 화면방향 * 5 + 옆 * 4);
            펜.LineTo(앞 - 화면방향 * 5 - 옆 * 4); 펜.ClosePath(); 펜.fillColor = new Color(.08f, .13f, .16f, .9f); 펜.Fill();
            펜.BeginPath(); 펜.MoveTo(뒤); 펜.LineTo(앞);
            펜.strokeColor = 색; 펜.lineWidth = 탄.공격종류 == 기본공격종류.화살 ? 1.5f : 3; 펜.Stroke();
            펜.BeginPath(); 펜.MoveTo(앞 + 화면방향 * 3); 펜.LineTo(앞 - 화면방향 * 4 + 옆 * 3);
            펜.LineTo(앞 - 화면방향 * 4 - 옆 * 3); 펜.ClosePath(); 펜.fillColor = 색; 펜.Fill();
            return;
        }
        펜.BeginPath(); 펜.MoveTo(뒤); 펜.LineTo(앞); 펜.strokeColor = new Color(색.r, 색.g, 색.b, .45f); 펜.lineWidth = 3; 펜.Stroke();
        float 크기 = 탄.공격종류 == 기본공격종류.별빛 ? 5 : 3;
        펜.BeginPath(); 펜.MoveTo(앞 - Vector2.up * 크기); 펜.LineTo(앞 + Vector2.right * 크기);
        펜.LineTo(앞 + Vector2.up * 크기); 펜.LineTo(앞 - Vector2.right * 크기); 펜.ClosePath(); 펜.fillColor = 색; 펜.Fill();
    }

    static void 검격궤적그리기(Painter2D 펜, Vector2 중심, float 반경, float 방향각, Color 색, float 밝기)
    {
        const int 면 = 8;
        Vector2 점(int 번호, float 안쪽비율)
        {
            float 진행 = (float)번호 / 면;
            float 각 = 방향각 + Mathf.Lerp(-1.28f, 1.08f, 진행);
            float 거리 = 반경 * (Mathf.Lerp(.81f, 1f, 진행) - 안쪽비율 * Mathf.Sin(진행 * Mathf.PI));
            return 중심 + new Vector2(Mathf.Cos(각), Mathf.Sin(각)) * 거리;
        }

        펜.BeginPath();
        펜.MoveTo(점(0, 0));
        for (int i = 1; i <= 면; i++) 펜.LineTo(점(i, 0));
        for (int i = 면 - 1; i > 0; i--) 펜.LineTo(점(i, .38f));
        펜.ClosePath();
        색.a = 밝기;
        펜.fillColor = 색;
        펜.Fill();
        펜.strokeColor = new Color(.11f, .09f, .12f, 밝기);
        펜.lineWidth = 2;
        펜.Stroke();

        펜.BeginPath();
        펜.MoveTo(점(0, 0));
        for (int i = 1; i <= 면; i++) 펜.LineTo(점(i, 0));
        for (int i = 면 - 1; i > 0; i--) 펜.LineTo(점(i, .16f));
        펜.ClosePath();
        펜.fillColor = Color.Lerp(색, new Color(1, 1, 1, 밝기), .42f);
        펜.Fill();
    }

    static void 효과선(Painter2D 펜, Vector2 시작, Vector2 끝, float 폭, Color 색)
    {
        펜.BeginPath();
        펜.MoveTo(시작);
        펜.LineTo(끝);
        펜.lineWidth = 폭;
        펜.strokeColor = 색;
        펜.Stroke();
    }

    static void 효과쐐기(Painter2D 펜, Vector2 끝, Vector2 밑, Vector2 옆, Color 색)
    {
        펜.BeginPath();
        펜.MoveTo(끝);
        펜.LineTo(밑 + 옆);
        펜.LineTo(밑 - 옆);
        펜.ClosePath();
        펜.fillColor = 색;
        펜.Fill();
    }

    void 특수검격그리기(Painter2D 펜, 스킬지대 타격)
    {
        bool 후퇴 = 타격.연출기술 == 장착기술.납치;
        float t = Mathf.Clamp01(1 - 타격.남은시간 / 타격.지속시간);
        float alpha = 1 - t;
        Vector2 높이 = Vector2.up * .58f;
        var 시작 = 후퇴 ? 타격.시작 : 타격.위치;
        var 중심 = 위치변환(시작 + 높이);
        float 크기 = 후퇴 ? 2.85f : 타격.반경;
        float 반경 = Vector2.Distance(중심, 위치변환(시작 + 높이 + Vector2.right * 크기));
        var 방향 = new Vector2(타격.방향.x, -타격.방향.y);
        var 측면 = new Vector2(-방향.y, 방향.x);
        bool 역방향 = 후퇴 ? 타격.적중수 % 2 == 0 : 타격.강화;
        float 각 = Mathf.Atan2(방향.y, 방향.x) + Mathf.Lerp(-.48f, .48f, t) * (역방향 ? -1 : 1);
        var 색 = new Color(.94f, .19f, .32f);
        var 어둠 = new Color(.28f, .045f, .12f, alpha);
        var 섬광 = new Color(1f, .91f, .76f, alpha);

        검격궤적그리기(펜, 중심, 반경 * 1.07f, 각 - .24f, 어둠, alpha * .7f);
        검격궤적그리기(펜, 중심, 반경, 각, 색, Mathf.Min(1, alpha * 1.55f));
        if (후퇴)
            검격궤적그리기(펜, 중심 + 방향 * 반경 * .15f, 반경 * .94f, 각 + .60f, 섬광, alpha * .88f);
        if (!후퇴 && 타격.적중수 == 0) return;

        var 충돌 = 위치변환((후퇴 ? 타격.위치 : 타격.시작) + 높이);
        var 가로 = 후퇴 ? 측면 : 방향;
        효과선(펜, 충돌 - 가로 * 반경 * (.33f + t * .35f), 충돌 + 가로 * 반경 * (.35f + t * .55f),
            Mathf.Max(1, 반경 * .085f * alpha), 섬광);
        int 조각수 = 후퇴 ? 12 : 5;
        for (int i = 0; i < 조각수; i++)
        {
            float 조각각 = 각 + i * (Mathf.PI * 2 / 조각수);
            var 밖 = new Vector2(Mathf.Cos(조각각), Mathf.Sin(조각각));
            var 옆 = new Vector2(-밖.y, 밖.x);
            float 거리 = 반경 * (.13f + t * (.42f + .12f * (i % 3)));
            효과쐐기(펜, 충돌 + 밖 * (거리 + 반경 * .19f * alpha), 충돌 + 밖 * 거리,
                옆 * 반경 * .051f * alpha, i % 3 == 0 ? 섬광 : new Color(색.r, 색.g, 색.b, alpha));
        }
        if (후퇴 && 타격.강화)
            파동그리기(펜, 타격.위치, .55f + t * 1.6f, alpha, new Color(1f, .11f, .27f));
    }

    void 수혈그리기(Painter2D 펜, 스킬지대 기운, 전장대상 왕)
    {
        float 진행 = 1 - 기운.남은시간 / 기운.지속시간;
        var 출발 = 기운.위치 + Vector2.up * .33f;
        var 도착 = 왕.위치 + Vector2.up * .68f;
        var 옆 = new Vector2(-(도착 - 출발).y, (도착 - 출발).x).normalized;
        for (int i = 0; i < 9; i++)
        {
            float t = (진행 - i * .034f) / .69f;
            if (t <= 0 || t >= 1) continue;
            float 각 = i * 2.39996f;
            var 굽힘 = 옆 * (Mathf.Cos(각) * (1.05f + i * .045f)) + Vector2.up * (1.3f + .55f * Mathf.Sin(각));
            Vector2 궤적(float x) => Vector2.Lerp(출발, 도착, x * x) + 굽힘 * Mathf.Sin(x * Mathf.PI);
            var 앞 = 위치변환(궤적(t));
            var 뒤 = 위치변환(궤적(Mathf.Max(0, t - .13f)));
            var 방향 = (앞 - 뒤).normalized;
            var 너비 = new Vector2(-방향.y, 방향.x) * (i % 2 == 0 ? 4.5f : 3f);
            효과쐐기(펜, 뒤, 앞, 너비, new Color(.5f, .025f, .09f, .8f));
            효과쐐기(펜, 앞 + 방향 * 5, 앞 - 방향 * 5, 너비 * .78f, new Color(1, .20f + .06f * (i % 3), .32f, .95f));
        }
        float 맥동 = Mathf.Max(0, 1 - Mathf.Abs(진행 - .78f) * 8);
        if (맥동 > 0) 파동그리기(펜, 왕.위치, .55f + (1 - 맥동) * .36f, 맥동, new Color(.98f, .17f, .33f));
    }

    void 왕지속효과그리기(Painter2D 펜)
    {
        for (int 팀 = 0; 팀 < 상태.진영들.Length; 팀++)
        {
            int 번호 = 전장규칙.왕번호(팀);
            var 왕 = 상태.대상들[번호];
            if (!슬롯[번호].enabled || 왕 is not { 생존: true }) continue;
            var 진영 = 상태.진영들[팀];
            if (진영.직업 != 왕직업.암살자 || !왕.은신) continue;

            var 색 = new Color(.46f, .72f, .37f);
            float 시간 = 상태.시간;
            for (int i = 0; i < 8; i++)
            {
                float 각 = i * .7854f + 시간 * .55f;
                float t = Mathf.Repeat(시간 * .9f + i * .32f, 1);
                var 밖 = new Vector2(Mathf.Cos(각), Mathf.Sin(각) * .48f);
                var 점 = 왕.위치 + 밖 * (.72f + t * .23f) + Vector2.up * (t * .65f);
                var 화면점 = 위치변환(점);
                var 끝 = 위치변환(점 + new Vector2(Mathf.Cos(각) * .18f, .19f + .10f * (i % 2)));
                var 너비 = new Vector2(-(끝 - 화면점).y, (끝 - 화면점).x).normalized * 4;
                색.a = Mathf.Sin(t * Mathf.PI) * .72f;
                효과쐐기(펜, 끝, 화면점, 너비, 색);
            }
        }
    }

    bool 기술효과그리기(MeshGenerationContext 그림, 스킬지대 지대)
    {
        var 펜 = 그림.painter2D;
        var 기술 = 지대.연출기술;
        int 시전자번호 = 전장규칙.왕번호(지대.팀);
        if (지대.종류 == 지대종류.전투소리) return true;
        if (지대.종류 == 지대종류.납치연장)
        {
            if (지대.팀 == 내팀 || 슬롯[시전자번호].enabled) 갈고리연장그리기(펜, 지대);
            return true;
        }
        if (지대.종류 == 지대종류.수혈)
        {
            if (슬롯[시전자번호].enabled && 상태.대상들[시전자번호] is { 생존: true } 왕)
                수혈그리기(펜, 지대, 왕);
            return true;
        }
        if (지대.종류 == 지대종류.납치날림)
        {
            if ((uint)지대.대상 < 슬롯.Length && 슬롯[지대.대상].enabled)
            {
                var 시작 = 위치변환(지대.시작 + Vector2.up * .55f);
                var 끝 = 위치변환(지대.위치 + Vector2.up * .55f);
                효과선(펜, 시작, 끝, 4, new Color(.98f, .28f, .39f, .63f));
            }
            return true;
        }
        if (지대.종류 is not (지대종류.돌진궤적 or 지대종류.스킬충격 or 지대종류.납치당김)
            && (지대.종류 != 지대종류.투사체 || 기술 is not (장착기술.납치 or 장착기술.급발진 or 장착기술.출혈화살)))
            return false;

        var 색 = 공격효과색(지대.팀);
        bool 시전자보임 = 슬롯[전장규칙.왕번호(지대.팀)].enabled;
        if (지대.팀 != 내팀 && 지형.은신지형인가(지대.위치) && !시전자보임) return true;

        if (지대.종류 == 지대종류.스킬충격)
        {
            if (기술 == 장착기술.관절빼서공격하기)
            {
                관절베기그리기(펜, 지대);
                return true;
            }
            if (기술 == 장착기술.납치)
            {
                if (시전자보임) 특수검격그리기(펜, 지대);
                return true;
            }
            float 진행 = Mathf.Clamp01(1 - 지대.남은시간 / 지대.지속시간);
            var 중심 = 위치변환(지대.위치 + Vector2.up * .25f);
            float 반경 = Vector2.Distance(위치변환(지대.위치), 위치변환(지대.위치 + Vector2.right * 지대.반경));
            색.a = (1 - 진행) * .95f;
            for (int i = 0; i < 8; i++)
            {
                float 각 = i * Mathf.PI / 4;
                var 방향 = new Vector2(Mathf.Cos(각), Mathf.Sin(각));
                var 옆 = new Vector2(-방향.y, 방향.x);
                효과쐐기(펜, 중심 + 방향 * 반경 * (.65f + 진행), 중심 + 방향 * 반경 * 진행,
                    옆 * 반경 * .12f, 색);
            }
            파동그리기(펜, 지대.위치, 지대.반경 * (.6f + 진행), 지대.남은시간, 색);
            return true;
        }

        if (지대.종류 == 지대종류.돌진궤적)
        {
            if (!시전자보임 || !안개.탐사됨(지대.시작)) return true;
            색.a = Mathf.Clamp01(지대.남은시간 / 전장규칙.돌진잔상시간);
            var 시작 = 위치변환(지대.시작 + Vector2.up * .2f);
            var 끝 = 위치변환(지대.위치 + Vector2.up * .2f);
            if (기술 == 장착기술.회전베기)
            {
                var 중심 = 위치변환(지대.위치 + Vector2.up * .62f);
                float 반경 = Vector2.Distance(중심, 위치변환(지대.위치 + new Vector2(1.55f, .62f)));
                float 각 = (지대.지속시간 - 지대.남은시간) * 24;
                검격궤적그리기(펜, 중심, 반경, 각, 색, 색.a);
                검격궤적그리기(펜, 중심, 반경 * .9f, 각 + Mathf.PI, 색, 색.a * .78f);
                if (지대.강화)
                    검격궤적그리기(펜, 중심, 반경 * 1.1f, 각 - .65f, 색, 색.a * .42f);
            }
            var 방향 = (끝 - 시작).normalized;
            var 옆 = new Vector2(-방향.y, 방향.x);
            float 길이 = Vector2.Distance(시작, 끝);
            if (길이 < 1) return true;
            var 먼지 = new Color(.85f, .68f, .46f, 색.a * .65f);
            for (int i = 0; i < 7; i++)
            {
                float 비율 = (i + .5f) / 7;
                var 점 = Vector2.Lerp(시작, 끝, 비율);
                var 날개 = 옆 * (i % 2 == 0 ? 1 : -1);
                효과쐐기(펜, 점 - 방향 * Mathf.Min(길이 * .18f, 21) + 날개 * 13,
                    점 + 날개 * 4, 날개 * 3, 먼지);
            }
            효과쐐기(펜, 시작, 끝, 옆 * 5, new Color(색.r, 색.g, 색.b, 색.a * .4f));
            효과선(펜, Vector2.Lerp(시작, 끝, .3f), 끝, 2, 색);
            return true;
        }

        bool 당김 = 지대.종류 == 지대종류.납치당김;
        var 높이 = Vector2.up * .64f;
        var 머리 = 위치변환(지대.위치 + 높이);
        var 발사자 = 상태.대상들[전장규칙.왕번호(지대.팀)];
        if (기술 == 장착기술.납치 && 당김 && !슬롯[지대.대상].enabled) return true;
        var 꼬리위치 = 기술 == 장착기술.납치 && 시전자보임 ? 발사자.위치 : 지대.위치 - 지대.방향 * .68f;
        var 꼬리 = 위치변환(꼬리위치 + 높이);
        var 진행방향 = (머리 - 꼬리).normalized;
        if (진행방향 == Vector2.zero)
            진행방향 = (위치변환(지대.위치 + 높이 + 지대.방향) - 머리).normalized;
        if (진행방향 == Vector2.zero) 진행방향 = Vector2.right;
        var 측면 = new Vector2(-진행방향.y, 진행방향.x);
        float 배율 = Vector2.Distance(머리, 위치변환(지대.위치 + 높이 + Vector2.right));
        if (기술 == 장착기술.급발진 && 급발진검그림 != null)
        {
            var 손잡이 = 머리 - 진행방향 * (1.05f * 배율);
            효과쐐기(펜, 손잡이 - 진행방향 * (.68f * 배율), 손잡이 + 진행방향 * (.22f * 배율),
                측면 * (.13f * 배율), new Color(.99f, .59f, .24f, .62f));
            표식그리기(그림, 머리 - 진행방향 * (.48f * 배율), new Vector2(32, 12) * (배율 / 48f),
                급발진검그림, Color.white, 진행방향);
            return true;
        }

        var 사슬끝 = 기술 == 장착기술.납치 && 작전상후퇴갈고리그림 != null
            ? 머리 - 진행방향 * Mathf.Min(.62f * 배율, Vector2.Distance(꼬리, 머리)) : 머리;
        효과선(펜, 꼬리, 사슬끝, 6, new Color(.13f, .09f, .17f, .95f));
        효과선(펜, 꼬리, 사슬끝, 2.5f, 색);
        if (기술 == 장착기술.납치)
        {
            if (!당김)
            {
                float 기본거리 = 왕직업목록.정보(장착기술.납치).사거리;
                if (지대.이동거리 > 기본거리)
                {
                    var 경계 = 위치변환(지대.시작 + 지대.방향 * 기본거리 + 높이);
                    if (Vector2.Dot(사슬끝 - 경계, 진행방향) > 0)
                    {
                        효과선(펜, 경계, 사슬끝, 8, new Color(.22f, .08f, .29f, .94f));
                        효과선(펜, 경계, 사슬끝, 4, 갈고리포획색);
                        효과선(펜, 경계, 사슬끝, 1, Color.white);
                    }
                    색 = 갈고리포획색;
                }
            }
            if (!당김 && 지대.강화)
            {
                var 섬광 = new Color(1, .78f, .58f, .86f);
                효과쐐기(펜, 머리 - 진행방향 * 34, 머리, 측면 * 7, 섬광);
                효과선(펜, 머리 - 진행방향 * 38 + 측면 * 8, 머리 - 진행방향 * 5 + 측면 * 8, 2, 섬광);
            }
            float 길이 = Vector2.Distance(꼬리, 사슬끝);
            for (float 거리 = 8; 거리 + 5 < 길이; 거리 += 14)
            {
                var 점 = 꼬리 + 진행방향 * 거리;
                효과선(펜, 점 - 측면 * 2, 점 + 진행방향 * 4 + 측면 * 2, 2, Color.Lerp(색, Color.white, .35f));
            }
            if (당김)
                파동그리기(펜, 지대.위치, .55f, .35f, 색);
            if (작전상후퇴갈고리그림 != null)
                표식그리기(그림, 머리 - 진행방향 * (.31f * 배율), new Vector2(24, 15) * (배율 / 48f),
                    작전상후퇴갈고리그림, Color.white, 진행방향);
            else
            {
                효과선(펜, 머리 - 측면 * 6 - 진행방향 * 6, 머리, 3, 색);
                효과선(펜, 머리 + 측면 * 6 - 진행방향 * 6, 머리, 3, 색);
            }
        }
        else 효과쐐기(펜, 머리 + 진행방향 * 8, 머리 - 진행방향 * 7, 측면 * 5, 색);
        return true;
    }

    void 갈고리연장그리기(Painter2D 펜, 스킬지대 연장)
    {
        float 진행 = Mathf.Clamp01(1 - 연장.남은시간 / 연장.지속시간);
        var 시작위치 = 연장.위치 + Vector2.up * .64f;
        var 시작 = 위치변환(시작위치);
        var 끝 = 위치변환(시작위치 + 연장.방향 * 연장.반경 * Mathf.Lerp(.32f, 1, 진행));
        var 앞 = (끝 - 시작).normalized;
        var 옆 = new Vector2(-앞.y, 앞.x);
        var 색 = 갈고리포획색;
        색.a = 1 - 진행;
        var 밝은색 = new Color(1f, .94f, .8f, 색.a);
        효과선(펜, 시작, 끝, 7, new Color(.28f, .1f, .37f, 색.a));
        효과선(펜, 시작, 끝, 3, 색);
        효과선(펜, 끝 - 앞 * 10 + 옆 * 6, 끝, 2, 밝은색);
        효과선(펜, 끝 - 앞 * 10 - 옆 * 6, 끝, 2, 밝은색);
        효과쐐기(펜, 시작 + 옆 * (5 + 진행 * 10), 시작, 앞 * 3, 밝은색);
        효과쐐기(펜, 시작 - 옆 * (5 + 진행 * 10), 시작, 앞 * 3, 밝은색);
    }

    void 전장그리기(MeshGenerationContext 그림)
    {
        if (상태 == null || !전투중) return;
        var 펜 = 그림.painter2D;
        자원게이지그리기(펜);
        왕지속효과그리기(펜);
        var 왕 = 내왕();
        if (조준스킬칸 >= 0 && 왕 is { 생존: true }) 스킬조준그리기(그림, 왕);
        if (!패널열림 && Time.unscaledTime < 모닥불범위종료 && 표시모닥불칸 >= 0
            && 상태.대상들[표시모닥불칸] is { 생존: true, 완공대기: <= 0 } 표시모닥불
            && 표시모닥불.건물번호 == 표시모닥불번호)
            범위룬그리기(그림, 표시모닥불.위치, 전장규칙.모닥불회복반경, new Color(.58f, .96f, .51f), .10f);
        float 명령경과 = Time.unscaledTime - 명령표시시작;
        if (왕 is { 생존: true } && !패널열림 && 명령사거리반경 > 0 && 명령경과 < 명령표시길이)
        {
            float 불투명도 = 1 - Mathf.InverseLerp(.25f, 명령표시길이, 명령경과);
            범위룬그리기(그림, 왕.위치, 명령사거리반경, new Color(1, 1, 1, .72f * 불투명도), .06f * 불투명도);
        }
        if (배치 >= 0) { 건설영역그리기(펜); 건설격자(펜); }
        var 수풀시야중심 = 전장규칙.수풀시야중심(상태, 지형, 내팀);
        foreach (var 지대 in 상태.지대들)
        {
            if (!안개.탐사됨(지대.위치) && !정찰범위(지대.위치)) continue;
            if (기술효과그리기(그림, 지대)) continue;
            if (지대.종류 is 지대종류.기본투사체 or 지대종류.근접타격)
            { 공격효과그리기(펜, 지대); continue; }
            if (지대.종류 == 지대종류.장막)
            {
                장막그리기(펜, 지대);
                continue;
            }
            if (지대.종류 == 지대종류.폭탄화살)
            {
                파동그리기(펜, 지대.위치, .45f, .35f, 공격효과색(지대.팀));
                continue;
            }
            if (지대.종류 == 지대종류.수풀)
            {
                float 알파 = 1;
                if (수풀시야중심 is Vector2 중심)
                {
                    float 거리 = Vector2.Distance(중심, 지대.위치);
                    float 겹침 = Mathf.InverseLerp(지대.반경 + 전장규칙.수풀시야반경, 지대.반경, 거리);
                    알파 = Mathf.Lerp(1, 전장지형.열린수풀알파, 겹침);
                }
                파동그리기(펜, 지대.위치, 지대.반경, .35f, new Color(.3f, .65f, .2f), 알파);
                continue;
            }
            var 색 = 공격효과색(지대.팀);
            if (지대.종류 == 지대종류.벽 || 지대.종류 == 지대종류.불길)
            {
                float 폭 = 지대.종류 == 지대종류.벽 ? 3 : 4;
                펜.BeginPath(); 펜.MoveTo(위치변환(지대.위치 - 지대.방향 * 폭)); 펜.LineTo(위치변환(지대.위치 + 지대.방향 * 폭));
                펜.strokeColor = 색; 펜.lineWidth = 8; 펜.Stroke();
            }
            else 범위룬그리기(그림, 지대.위치, 지대.반경, new Color(색.r, 색.g, 색.b, .72f), .06f);
        }
        for (int 팀 = 0; 팀 < 상태.진영들.Length; 팀++)
        {
            var 진영 = 상태.진영들[팀];
            if (진영.음식회복시간 > 0 && 안개.탐사됨(진영.음식회복위치))
                파동그리기(펜, 진영.음식회복위치, 전장식량.회복반경, 진영.음식회복시간, new Color(.57f, .92f, .46f));
            if (진영.스킬표시시간 <= 0 || !안개.탐사됨(진영.스킬위치)) continue;
            if (팀 != 내팀 && 진영.스킬왕위치표시 && 지형.은신지형인가(진영.스킬위치)) continue;
            float 알파 = Mathf.Clamp01(진영.스킬표시시간 / .24f);
            var 색 = 공격효과색(팀);
            범위룬그리기(그림, 진영.스킬위치, 진영.스킬반경, new Color(색.r, 색.g, 색.b, 알파), .10f * 알파);
        }
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || !슬롯[i].enabled) continue;
            bool 선택 = 선택부대.Contains(i);
            var 위치 = (Vector2)슬롯[i].transform.position;
            var 색 = 대상.팀 == 0 ? 청록 : 적색;
            if (대상.종류 == 대상종류.회관 && 전장규칙.회관피해감소율(상태, 대상.팀) > 0)
                회관보호막그리기(그림, 위치, 대상.효과시간 > 0);
            else if (대상.종류 == 대상종류.주민)
            {
                int 순번 = 전장규칙.주민순번(대상.팀, i);
                float 남음 = 상태.진영들[대상.팀].우수주민등장종료[순번] - 상태.시간;
                if (남음 > 0) 우수주민등장그리기(그림, 위치, 남음);
            }
            bool 선택윤곽 = 유닛윤곽?[i] != null && 유닛윤곽[i].enabled && 유닛윤곽[i].sharedMaterial == 유닛선택재질;
            if (선택 && !선택윤곽)
            {
                var 점 = 위치변환(위치 - Vector2.up * .1f);
                if (전장표시.contentRect.Contains(점))
                    네모(펜, new Rect(점.x - 12, 점.y - 3, 24, 6), new Color(.21f, .83f, .72f, .32f), 청록);
            }
            if (!선택 && !대상.건물 && !대상.연습허수아비 && 대상.종류 != 대상종류.왕 && 대상.체력 >= 대상.최대체력) continue;
            bool 모닥불 = 대상.종류 == 대상종류.모닥불;
            float 높이 = 대상.울타리 ? (대상.세로 ? 1.22f : .88f)
                : 모닥불 ? 1.17f : 대상.건물 ? 2.12f : (대상.종류 is 대상종류.주민 or 대상종류.병사) ? 1.35f : 1.15f;
            if (대상.해골병사) 높이 *= 1.45f;
            var 위 = 위치변환(위치 + Vector2.up * 높이);
            if (대상.기절 > 0 || 대상.속박 > 0 || 대상.침묵 > 0 || 대상.공포 > 0 || 대상.매혹 > 0)
                파동그리기(펜, 위치 + Vector2.up * (높이 + .15f), .22f, .3f,
                    대상.기절 > 0 ? Color.yellow : 대상.매혹 > 0 ? Color.magenta : 대상.속박 > 0 ? Color.green : new Color(.7f, .5f, 1));
            if (대상.보호량 > 0 || 대상.가드 > 0 || 대상.불사 > 0)
                파동그리기(펜, 위치, .7f, .35f, 대상.불사 > 0 ? new Color(1, .8f, .1f) : Color.cyan);
            if (!전장표시.contentRect.Contains(위)) continue;
            float 폭 = 대상.건물 && !대상.울타리 && !모닥불 ? 49 : 27;
            네모(펜, new Rect(위.x - 폭 / 2, 위.y, 폭, 5), new Color(.06f, .11f, .13f, .9f));
            bool 건설중 = 대상.완공대기 > 0;
            float 비율 = Mathf.Clamp01(건설중
                ? 1 - 대상.완공대기 / 전장규칙.건설시간(대상)
                : 대상.체력 / 대상.최대체력);
            if (비율 > 0) 네모(펜, new Rect(위.x - 폭 / 2 + 1, 위.y + 1, (폭 - 2) * 비율, 3), 건설중 ? new Color(.9f, .72f, .38f) : 색);
            if (건설중)
            {
                string 시간 = Mathf.CeilToInt(대상.완공대기) + "s";
                var 글위치 = 위 - new Vector2(시간.Length * 4, 22);
                그림.DrawText(시간, 글위치 + Vector2.one, 14, new Color(.08f, .05f, .03f));
                그림.DrawText(시간, 글위치, 14, new Color(1, .86f, .54f));
            }
        }
        foreach (var 피해 in 피해숫자들)
        {
            float 남음 = Mathf.Clamp01((피해.끝 - 상태.시간) / .7f);
            var 점 = 위치변환(피해.위치) - new Vector2(5, 16 * (1 - 남음));
            if (!전장표시.contentRect.Contains(점)) continue;
            float 알파 = Mathf.Min(1, 남음 * 3);
            그림.DrawText(피해.숫자, 점 + Vector2.one, 16, new Color(.1f, .06f, .04f, 알파));
            그림.DrawText(피해.숫자, 점, 16, new Color(1, .86f, .54f, 알파));
        }
        if (드래그중 && 배치 < 0 && Vector2.Distance(누른위치, 현재마우스) > 9)
            네모(펜, 사각형(화면변환(누른위치), 화면변환(현재마우스)), new Color(1f, 1f, 1f, .13f), Color.white);
        if (!패널열림 && Time.unscaledTime - 명령표시시작 < 명령표시길이)
            명령화살표그리기(펜, 위치변환(명령표시위치), 1);
    }

    void 관절베기그리기(Painter2D 펜, 스킬지대 검격)
    {
        float 진행 = Mathf.Clamp01(1 - 검격.남은시간 / 검격.지속시간);
        float 알파 = 1 - 진행;
        var 금색 = new Color(1, .63f, .19f, 알파);
        var 섬광 = new Color(1, .95f, .74f, 알파);
        var 원점 = 검격.위치 + Vector2.up * .55f;
        if (검격.대상 >= 0)
        {
            var 점 = 위치변환(원점);
            var 끝 = 위치변환(원점 + 검격.방향);
            var 앞 = (끝 - 점).normalized;
            var 옆 = new Vector2(-앞.y, 앞.x);
            for (int i = -2; i <= 2; i++)
            {
                var 날 = (앞 + 옆 * i * .53f).normalized;
                효과쐐기(펜, 점 + 날 * (14 + 진행 * 24), 점 + 날 * (진행 * 12),
                    new Vector2(-날.y, 날.x) * (4 * 알파), i % 2 == 0 ? 섬광 : 금색);
            }
            return;
        }

        float 반경 = 검격.반경 * Mathf.Lerp(.88f, 1, Mathf.InverseLerp(0, .3f, 진행));
        float 반각 = 전장규칙.관절베기반각;
        부채그리기(펜, 원점, 검격.방향, 반각, 반경 * .52f, 반경,
            new Color(.54f, .23f, .06f, 알파 * .77f), 금색, 2, 날끝: true);
        부채그리기(펜, 원점, 검격.방향, 반각, 반경 * .76f, 반경,
            금색, 섬광, 1.5f, 날끝: true);
        부채그리기(펜, 원점, 검격.방향, 반각 * .96f, 반경 * .94f, 반경,
            섬광, Color.clear, 0, 날끝: true);

        float 방향각 = Mathf.Atan2(검격.방향.y, 검격.방향.x);
        for (int i = -2; i <= 2; i++)
        {
            float 각 = 방향각 + i * 반각 * .36f * Mathf.Deg2Rad;
            var 밖 = new Vector2(Mathf.Cos(각), Mathf.Sin(각));
            var 자취 = 밖 * (반경 * (1 + 진행 * .17f));
            var 옆 = new Vector2(-밖.y, 밖.x);
            var 시작 = 위치변환(원점 + 자취 - 옆 * (.16f + 진행 * .35f));
            var 끝 = 위치변환(원점 + 자취 + 옆 * .14f);
            효과선(펜, 시작, 끝, 1.5f, new Color(1, .79f, .33f, 알파 * .75f));
        }
    }

    void 부채그리기(Painter2D 펜, Vector2 원점, Vector2 방향, float 반각, float 안쪽, float 바깥,
        Color 채움, Color 테두리, float 폭, bool 날끝 = false)
    {
        const int 면수 = 12;
        float 중심각 = Mathf.Atan2(방향.y, 방향.x);
        Vector2 점(int 면, float 거리)
        {
            float 각 = 중심각 + Mathf.Lerp(-반각, 반각, (float)면 / 면수) * Mathf.Deg2Rad;
            return 위치변환(원점 + new Vector2(Mathf.Cos(각), Mathf.Sin(각)) * 거리);
        }

        펜.BeginPath();
        펜.MoveTo(점(0, 바깥));
        for (int i = 1; i <= 면수; i++) 펜.LineTo(점(i, 바깥));
        if (안쪽 <= 0) 펜.LineTo(위치변환(원점));
        else
            for (int i = 면수; i >= 0; i--)
            {
                float 거리 = 날끝 ? Mathf.Lerp(바깥, 안쪽, Mathf.Sin(Mathf.PI * i / 면수)) : 안쪽;
                펜.LineTo(점(i, 거리));
            }
        펜.ClosePath();
        펜.fillColor = 채움;
        펜.Fill();
        if (폭 <= 0) return;
        펜.strokeColor = 테두리;
        펜.lineWidth = 폭;
        펜.Stroke();
    }

    void 스킬조준그리기(MeshGenerationContext 그림, 전장대상 왕)
    {
        var 펜 = 그림.painter2D;
        int 칸 = 조준스킬칸;
        var 진영 = 상태.진영들[내팀];
        var 방식 = 스킬조준방식(진영, 칸);
        bool 장착식 = 왕직업목록.장착가능(진영.직업);
        var 기술 = 장착식 ? 왕직업목록.장착스킬(진영, 칸) : default;
        float 사거리 = 슬롯기술(진영, 칸).사거리;
        if (!장착식 && !왕직업목록.신규(진영.직업))
            사거리 = 칸 switch { 0 => 5, 1 => 진영.직업 == 왕직업.추적자 ? 6 : 4, 2 => 4, _ => 7 };

        if (방식 == 조준방식.자신)
        {
            범위룬그리기(그림, 왕.위치, 사거리 > 0 ? 사거리 : .55f, 청록, .12f);
            return;
        }

        if (방식 != 조준방식.방향)
            범위룬그리기(그림, 왕.위치, 사거리, new Color(청록.r, 청록.g, 청록.b, .72f), .04f);
        if (!스킬조준전장위) return;

        var 목표 = 길찾기.전장안쪽(전장좌표(현재마우스));
        if (방식 == 조준방식.방향)
        {
            var 방향 = (목표 - 왕.위치).normalized;
            if (방향 == Vector2.zero) 방향 = 왕.시선;
            if (기술 == 장착기술.관절빼서공격하기)
            {
                부채그리기(펜, 왕.위치, 방향, 전장규칙.관절베기반각, 0, 사거리,
                    new Color(청록.r, 청록.g, 청록.b, .18f), 청록, 1.5f);
                return;
            }
            float 반폭 = 기술 switch
            {
                장착기술.회전베기 or 장착기술.꿰뚫기 => 전장규칙.돌진타격반경,
                장착기술.앞구르기 => 0,
                _ => 전장규칙.장착투사체반경
            };
            조준방향그리기(펜, 왕.위치, 방향, 사거리, 반폭);
            if (기술 == 장착기술.납치) 조준방향그리기(펜, 왕.위치, -방향, 전장규칙.납치후퇴거리, 0);
            return;
        }

        bool 유효 = (목표 - 왕.위치).sqrMagnitude <= 사거리 * 사거리;
        if (방식 == 조준방식.병사)
        {
            var 병사 = 전장규칙.가까운아군병사(상태.대상들, 왕, 목표, 사거리,
                기술 == 장착기술.동료의복수 ? 전장규칙.복수조준반경 : float.PositiveInfinity);
            if (병사 != null) 목표 = 병사.위치;
            유효 = 병사 != null;
        }
        else if (장착식 && 기술 == 장착기술.집합) 유효 &= 길찾기.통행가능(목표);

        float 반경 = 장착식 && 기술 == 장착기술.잠복 ? 전장규칙.장막반경 : .38f;
        범위룬그리기(그림, 목표, 반경, 유효 ? 청록 : 적색, .18f);
    }

    void 조준방향그리기(Painter2D 펜, Vector2 원점, Vector2 방향, float 거리, float 반폭)
    {
        var 시작 = 위치변환(원점);
        var 끝 = 위치변환(원점 + 방향 * 거리);
        var 진행 = (끝 - 시작).normalized;
        var 측면 = new Vector2(-진행.y, 진행.x);
        if (반폭 > 0)
        {
            float 폭 = Vector2.Distance(시작, 위치변환(원점 + 방향 * 반폭)) * 2;
            펜.lineCap = LineCap.Round;
            효과선(펜, 시작, 끝, 폭, new Color(청록.r, 청록.g, 청록.b, .18f));
            펜.lineCap = LineCap.Butt;
        }
        효과선(펜, 시작, 끝, 2, 청록);
        효과쐐기(펜, 끝, 끝 - 진행 * 12, 측면 * 5, 청록);
    }

    void 범위룬그리기(MeshGenerationContext 그림, Vector2 위치, float 반경, Color 색, float 채움)
    {
        if (반경 <= 0) return;
        var 중심 = 위치변환(위치);
        var 반폭 = new Vector2(
            Vector2.Distance(중심, 위치변환(위치 + Vector2.right * 반경)),
            Vector2.Distance(중심, 위치변환(위치 + Vector2.up * 반경)));
        if (!전장표시.contentRect.Overlaps(new Rect(중심 - 반폭, 반폭 * 2))) return;
        var 펜 = 그림.painter2D;
        if (채움 > 0 || 범위룬 == null)
        {
            펜.BeginPath();
            for (int i = 0; i < 24; i++)
            {
                float 각 = i * (2 * Mathf.PI / 24);
                var 점 = 중심 + Vector2.Scale(반폭, new Vector2(Mathf.Cos(각), Mathf.Sin(각)));
                if (i == 0) 펜.MoveTo(점);
                else 펜.LineTo(점);
            }
            펜.ClosePath();
            펜.fillColor = new Color(색.r, 색.g, 색.b, 채움);
            펜.Fill();
            if (범위룬 == null) { 펜.strokeColor = 색; 펜.lineWidth = 2; 펜.Stroke(); }
        }
        if (범위룬 != null) 표식그리기(그림, 중심, 반폭 * (80f / 78f), 범위룬, 색);
    }

    void 회관보호막그리기(MeshGenerationContext 그림, Vector2 위치, bool 피격)
    {
        float 밝기 = 피격 ? 1 : .72f + .12f * Mathf.Sin(상태.시간 * 2.4f);
        var 색 = new Color(.46f, .86f, 1f, 밝기);
        범위룬그리기(그림, 위치, 1.65f, 색, .07f);
        var 펜 = 그림.painter2D;
        펜.BeginPath();
        펜.MoveTo(위치변환(위치 + new Vector2(-1.4f, -.05f)));
        펜.LineTo(위치변환(위치 + new Vector2(-1.55f, .95f)));
        펜.LineTo(위치변환(위치 + new Vector2(-1.03f, 2.02f)));
        펜.LineTo(위치변환(위치 + new Vector2(0, 2.52f)));
        펜.LineTo(위치변환(위치 + new Vector2(1.03f, 2.02f)));
        펜.LineTo(위치변환(위치 + new Vector2(1.55f, .95f)));
        펜.LineTo(위치변환(위치 + new Vector2(1.4f, -.05f)));
        펜.ClosePath();
        펜.fillColor = new Color(색.r, 색.g, 색.b, 밝기 * .16f);
        펜.Fill();
        펜.strokeColor = 색;
        펜.lineWidth = 피격 ? 3f : 1.5f;
        펜.Stroke();
    }

    void 우수주민등장그리기(MeshGenerationContext 그림, Vector2 위치, float 남음)
    {
        float 진행 = 1 - 남음 / 전장규칙.우수주민등장시간;
        var 색 = new Color(1f, .82f, .37f, Mathf.Clamp01(남음 * 2));
        범위룬그리기(그림, 위치, Mathf.Lerp(.45f, 1.24f, 진행), 색, .10f * 색.a);
        var 펜 = 그림.painter2D;
        for (int i = 0; i < 6; i++)
        {
            float 각 = i * (2 * Mathf.PI / 6) + 진행 * 1.7f;
            var 점 = 위치변환(위치 + new Vector2(Mathf.Cos(각) * .66f, Mathf.Sin(각) * .4f + 진행 * 1.45f));
            효과쐐기(펜, 점 - Vector2.up * 5, 점 + Vector2.up * 4, Vector2.right * 2, 색);
        }
    }

    void 파동그리기(Painter2D 펜, Vector2 위치, float 효과반경, float 남은시간, Color 색, float 알파 = 1)
    {
        var 중심 = 위치변환(위치);
        float 여운 = 남은시간 / .48f;
        float 반경 = Vector2.Distance(중심, 위치변환(위치 + Vector2.right * 효과반경)) * (1.08f - 여운 * .18f);
        펜.BeginPath();
        펜.Arc(중심, 반경, Angle.Degrees(0), Angle.Degrees(360));
        펜.ClosePath();
        펜.fillColor = new Color(색.r, 색.g, 색.b, 여운 * .16f * 알파);
        펜.strokeColor = new Color(색.r, 색.g, 색.b, 여운 * .8f * 알파);
        펜.lineWidth = 2;
        펜.Fill();
        펜.Stroke();
    }

    void 건설격자(Painter2D 펜)
    {
        var 칸범위 = 바닥타일맵.cellBounds;
        for (int y = 칸범위.yMin; y < 칸범위.yMax; y++)
        for (int x = 칸범위.xMin; x < 칸범위.xMax; x++)
        {
            var 칸 = new Vector3Int(x, y, 0);
            var 위치 = 바닥타일맵.GetCellCenterWorld(칸);
            if (!전장규칙.건설영역안(상태, 내팀, 위치)) continue;
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
        for (int i = 0; i < 자원들.Length; i++)
        {
            var 지점 = 자원들[i];
            if (상태.자원잔량[i] <= 0) continue;
            if (!안개.탐사됨(지점.위치)) continue;
            var 점 = 지도위치(범위, 지점.위치);
            네모(펜, new Rect(점.x - 1, 점.y - 1, 2, 2), new Color(.61f, .62f, .43f, .83f));
        }
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 == null || !대상.생존 || 대상.종류 == 대상종류.회관 || !미니맵대상표시(대상)) continue;
            var 점 = 지도위치(범위, 대상.위치);
            float 폭 = 대상.건물 || 대상.종류 == 대상종류.왕 ? 4 : 2;
            네모(펜, new Rect(점.x - 폭 / 2, 점.y - 폭 / 2, 폭, 폭), 대상.팀 == 0 ? 청록 : 적색);
        }
        foreach (var 시련 in 상태.시련들)
        {
            if (!안개.탐사됨(시련.위치) || 시련.단계 == 시련단계.완료) continue;
            var 점 = 지도위치(범위, 시련.위치);
            var 색 = 시련.단계 == 시련단계.전투 ? new Color(1, .4f, .2f) : new Color(.92f, .78f, .42f);
            네모(펜, new Rect(점.x - 2.5f, 점.y - 2.5f, 5, 5), 색);
        }
        var 중심 = (Vector2)(전장카메라.transform.position - 카메라흔들림편차);
        float 세로 = 전장카메라.orthographicSize, 가로 = 세로 * 전장카메라.aspect;
        var 왼쪽위 = 지도위치(범위, 중심 + new Vector2(-가로, 세로));
        var 오른쪽아래 = 지도위치(범위, 중심 + new Vector2(가로, -세로));
        네모(펜, 사각형(왼쪽위, 오른쪽아래), default, new Color(.92f, .9f, .74f, .64f));
        if (!패널열림 && 지도에서명령)
            명령화살표그리기(펜, 지도위치(범위, 명령표시위치), .6f);
        for (int 팀 = 0; 팀 < 상태.진영들.Length; 팀++)
        {
            var 대상 = 상태.대상들[전장규칙.회관번호(팀)];
            if (대상 == null || !대상.생존 || !미니맵대상표시(대상)) continue;
            본진휘장그리기(그림, 지도위치(범위, 대상.위치), 대상.팀 == 내팀 ? 아군본진휘장 : 적본진휘장);
        }
        for (int i = 0; i < 군단수; i++)
            if (군단인원[i] > 0 && 군단휘장들[i] != null)
                표식그리기(그림, 지도위치(범위, 군단중심[i]), new Vector2(8.5f, 10), 군단휘장들[i], Color.white);
    }

    static void 본진휘장그리기(MeshGenerationContext 그림, Vector2 중심, Texture2D 휘장)
    {
        if (휘장 != null) 표식그리기(그림, 중심, new Vector2(9, 9), 휘장, Color.white);
    }

    static void 표식그리기(MeshGenerationContext 그림, Vector2 중심, Vector2 반폭, Texture2D 표식, Color32 색,
        Vector2? 방향 = null)
    {
        var 망 = 그림.Allocate(4, 6, 표식);
        var uv = 망.uvRegion;
        var 진행 = 방향 ?? Vector2.right;
        var 가로 = 진행 * 반폭.x;
        var 세로 = new Vector2(-진행.y, 진행.x) * 반폭.y;
        void 꼭짓점(Vector2 위치, float u, float v) =>
            망.SetNextVertex(new Vertex { position = new Vector3(위치.x, 위치.y, Vertex.nearZ), tint = 색, uv = new Vector2(u, v) });
        꼭짓점(중심 - 가로 - 세로, uv.xMin, uv.yMax);
        꼭짓점(중심 + 가로 - 세로, uv.xMax, uv.yMax);
        꼭짓점(중심 + 가로 + 세로, uv.xMax, uv.yMin);
        꼭짓점(중심 - 가로 + 세로, uv.xMin, uv.yMin);
        망.SetNextIndex(0); 망.SetNextIndex(1); 망.SetNextIndex(2);
        망.SetNextIndex(2); 망.SetNextIndex(3); 망.SetNextIndex(0);
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
        if (디버그지도클릭(지도전장좌표(입력.localPosition))) return;
        지도누른위치 = 입력.localPosition;
        지도더블클릭후보 = 지도클릭대기 is { } 대기
            && 대기.포인터 == 입력.pointerId && Time.unscaledTime <= 대기.판정시각
            && (대기.패널점 - 지도누른위치).sqrMagnitude <= 지도클릭여유 * 지도클릭여유;
        if (!지도더블클릭후보) 지도클릭확정();
        지도누르기전왕기준시점 = (Vector2)(전장카메라.transform.position - 카메라흔들림편차) - 내왕().위치;
        지도포인터 = 입력.pointerId;
        지도.CapturePointer(입력.pointerId);
    }

    bool 지도드래그됨(Vector2 점) => (점 - 지도누른위치).sqrMagnitude > 지도클릭여유 * 지도클릭여유;

    void 지도끌기(PointerMoveEvent 입력)
    {
        if (지도포인터 != 입력.pointerId) return;
        if ((입력.pressedButtons & 1) == 0)
        {
            지도누름해제();
            return;
        }
        if (!지도끌기중 && !지도드래그됨(입력.localPosition)) return;
        if (지도더블클릭후보)
        {
            지도더블클릭후보 = false;
            지도클릭확정();
        }
        지도끌기중 = true;
        지도시점이동(입력.localPosition);
    }

    void 지도떼기(PointerUpEvent 입력)
    {
        if (지도포인터 != 입력.pointerId || 입력.button != 0) return;
        bool 클릭 = !지도끌기중 && !지도드래그됨(입력.localPosition)
            && 지도.contentRect.Contains(입력.localPosition)
            && 전투중 && !패널열림 && !주민패널.열림;
        bool 더블클릭 = 클릭 && 지도더블클릭후보;
        if (더블클릭) 지도클릭대기 = null;
        지도누름해제();
        if (!클릭) return;
        if (더블클릭)
        {
            자동추적 = false;
            지도시점이동(입력.localPosition);
            return;
        }

        int[] 부대 = null;
        if (배치 < 0 && 선택부대.Count > 0)
        {
            부대 = new int[선택부대.Count];
            선택부대.CopyTo(부대);
        }
        지도클릭대기 = (입력.localPosition, 지도전장좌표(입력.localPosition), 입력.pointerId,
            Time.unscaledTime + 지도더블클릭간격, 부대);
    }

    void 지도클릭확정()
    {
        if (지도클릭대기 is not { } 대기) return;
        지도클릭대기 = null;
        if (대기.부대 == null || !전투중 || 패널열림 || 주민패널.열림 || 배치 >= 0) return;
        명령전달(명령종류.부대이동, 대기.전장점, 부대: 대기.부대, 지도입력: true);
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
        if (지도끌기중)
            지도시점설정(내왕().위치 + 지도누르기전왕기준시점);
        지도끌기중 = false;
        지도더블클릭후보 = false;
        if (지도.HasPointerCapture(포인터)) 지도.ReleasePointer(포인터);
    }

    void 지도시점이동(Vector2 점) => 지도시점설정(지도전장좌표(점));

    void 지도시점설정(Vector2 위치)
    {
        카메라흔들림제거();
        전장카메라.transform.position = 카메라한계(위치);
    }

    Vector2 지도전장좌표(Vector2 점)
    {
        var 범위 = 지도.contentRect;
        float 가로 = Mathf.InverseLerp(범위.xMin, 범위.xMax, 점.x);
        float 세로 = Mathf.InverseLerp(범위.yMin, 범위.yMax, 점.y);
        var 전장 = 길찾기.전장범위;
        return new Vector2(Mathf.Lerp(전장.xMin, 전장.xMax, 가로), Mathf.Lerp(전장.yMax, 전장.yMin, 세로));
    }
}

