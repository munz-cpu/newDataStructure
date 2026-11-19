using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class 신들의전쟁 : MonoBehaviour
{
    [SerializeField] Camera 전장카메라;
    [SerializeField] SpriteRenderer[] 슬롯;
    [SerializeField] SpriteRenderer 건설미리보기;
    [SerializeField] StyleSheet 전장스타일;
    [SerializeField] 자원지점[] 자원들;
    [SerializeField] Tilemap 바닥타일맵, 장애물타일맵;
    [SerializeField] Transform[] 시작지점들;

    static readonly Color 청록 = new(.39f, .85f, .82f);
    static readonly Color 적색 = new(1, .49f, .45f);
    readonly HashSet<int> 선택부대 = new();
    readonly Button[] 행동버튼 = new Button[4], 스킬버튼 = new Button[4];
    VisualElement 뿌리, 전투, 전장표시, 지도, 입장대기;
    Label 자원표시, 병력표시, 시계, 내회관, 적회관, 왕상태, 임무, 알림, 도움;
    격자길찾기 길찾기;
    전투메뉴 메뉴;

    전장규칙 규칙;
    전장상태 상태;
    참가연결 연결;
    int 내팀, 배치 = -1, 대기공격 = -1;
    float 대기공격시간, 마지막클릭 = -10, 전송시간, 손님대기, 시뮬누적;
    Vector2 누른위치, 현재마우스, 이전클릭위치;
    bool 멀티, 전투중, 일시정지, 패널열림, 자동추적, 드래그중, 팝업재개;
    string 연결종료 = "", 팝업제목, 팝업본문;

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        뿌리 = GetComponent<UIDocument>().rootVisualElement;
        if (전장스타일 != null) 뿌리.styleSheets.Add(전장스타일);
        전투글꼴.적용(뿌리);
        전투 = 뿌리.Q("battle");
        입장대기 = 뿌리.Q("loading");
        전장표시 = 뿌리.Q("draw");
        지도 = 뿌리.Q("minimap");
        자원표시 = 뿌리.Q<Label>("resources");
        병력표시 = 뿌리.Q<Label>("population");
        시계 = 뿌리.Q<Label>("clock");
        내회관 = 뿌리.Q<Label>("own-hall");
        적회관 = 뿌리.Q<Label>("enemy-hall");
        왕상태 = 뿌리.Q<Label>("king-status");
        임무 = 뿌리.Q<Label>("quest");
        알림 = 뿌리.Q<Label>("notice");
        도움 = 뿌리.Q<Label>("help");
        for (int i = 0; i < 4; i++)
        {
            int 번호 = i;
            행동버튼[i] = 뿌리.Q<Button>("action-" + i);
            행동버튼[i].focusable = false;
            행동버튼[i].clicked += () => 행동고르기(번호);
            스킬버튼[i] = 뿌리.Q<Button>("skill-" + i);
            스킬버튼[i].SetEnabled(false);
        }
        뿌리.Q<Button>("pause").clicked += 메뉴전환;
        뿌리.Q<Button>("center").clicked += 시점복귀;
        전장표시.generateVisualContent += 전장그리기;
        전장표시.RegisterCallback<PointerDownEvent>(전장누르기);
        전장표시.RegisterCallback<PointerUpEvent>(전장떼기);
        전장표시.RegisterCallback<PointerCaptureOutEvent>(_ => 드래그중 = false);
        지도.generateVisualContent += 지도그리기;
        지도.RegisterCallback<PointerDownEvent>(지도찍기);
        표시(전투, false);
        표시(전장표시, false);
    }

    void Start()
    {
        길찾기 = new 격자길찾기(바닥타일맵, 장애물타일맵);
        SceneManager.sceneLoaded += 씬준비;
        var 메뉴씬 = SceneManager.GetSceneByName("BattleMenu");
        if (메뉴씬.isLoaded) 씬준비(메뉴씬, LoadSceneMode.Additive);
        else SceneManager.LoadSceneAsync("BattleMenu", LoadSceneMode.Additive);

        var 입장 = 전장입장.가져오기();
        연결 = 입장?.연결;
        멀티 = 연결 != null;
        내팀 = 입장?.내팀 ?? 0;
        if (내팀 == 1) return;
        var 위치들 = Array.ConvertAll(시작지점들, 지점 => (Vector2)지점.position);
        규칙 = new 전장규칙(자원들, 길찾기, 위치들, 입장?.청록직업 ?? 왕직업.수호자,
            입장?.적색직업 ?? 왕직업.추적자, UnityEngine.Random.Range(0, int.MaxValue));
        상태 = 규칙.상태;
        화면전환();
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
            if (패널열림) 메뉴.보이기(팝업제목, 팝업본문, 팝업재개);
            break;
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= 씬준비;
        if (메뉴 != null)
        {
            메뉴.재개 -= 메뉴전환;
            메뉴.나가기 -= 타이틀이동;
        }
        연결?.Dispose();
        연결 = null;
    }

    static void 표시(VisualElement 요소, bool 켜짐) => 요소.style.display = 켜짐 ? DisplayStyle.Flex : DisplayStyle.None;
    void 타이틀이동() => SceneManager.LoadScene("Title");

    void 화면전환()
    {
        전투중 = true;
        표시(입장대기, false);
        표시(전투, true);
        표시(전장표시, true);
        전장카메라.orthographicSize = 7.8f;
        시점복귀();
        전장카메라.transform.position = 카메라한계(내왕().위치);
        정보갱신();
        장면반영(1);
    }

    void 접속갱신(float 초)
    {
        if (연결 == null) return;
        string 마지막상태 = null;
        int 처리수 = 0;
        while (처리수++ < 48 && 연결.받기(out string 문장))
        {
            if (내팀 == 1 && 문장.StartsWith("S", StringComparison.Ordinal))
                마지막상태 = 문장;
            else if (내팀 == 0 && 문장.StartsWith("C", StringComparison.Ordinal))
            {
                try { 규칙.명령(1, JsonUtility.FromJson<전장명령>(문장.Substring(1))); }
                catch (ArgumentException) { }
            }
        }
        if (마지막상태 != null)
        {
            try
            {
                var 받은상태 = JsonUtility.FromJson<전장상태>(마지막상태.Substring(1));
                if (받은상태?.대상들?.Length == 전장규칙.전체칸 && 받은상태.진영들?.Length == 2)
                {
                    상태 = 받은상태;
                    if (!전투중) 화면전환();
                }
            }
            catch (ArgumentException) { }
        }
        if (상태?.승자 >= 0 || 연결종료.Length > 0) return;
        if (연결.오류 != null) 연결종료 = 연결.오류;
        else if (!전투중 && (손님대기 += 초) > 12) 연결종료 = "호스트의 전장 정보를 받지 못했습니다.";
        if (연결종료.Length > 0) 팝업표시("연결 종료", 연결종료, false);
    }

    void 명령전달(명령종류 종류, Vector2 위치 = default, int 값 = 0, int[] 부대 = null)
    {
        if (!전투중 || 상태.승자 >= 0 || 일시정지 || 연결종료.Length > 0) return;
        var 내용 = new 전장명령 { 종류 = 종류, 위치 = 위치, 값 = 값, 부대 = 부대 };
        if (규칙 != null) 규칙.명령(내팀, 내용);
        else 연결?.보내기("C" + JsonUtility.ToJson(내용));
    }

    void Update()
    {
        float 초 = Mathf.Min(Time.unscaledDeltaTime, .1f);
        접속갱신(초);
        if (!전투중) return;

        var 키 = Keyboard.current;
        if (키 != null && 키.escapeKey.wasPressedThisFrame)
        {
            if (배치 >= 0 && !패널열림) 배치 = -1;
            else 메뉴전환();
        }
        if (규칙 != null && !일시정지 && 연결종료.Length == 0 && 상태.승자 < 0)
        {
            시뮬누적 += 초;
            while (시뮬누적 >= .04f)
            {
                규칙.진행(.04f, !멀티);
                시뮬누적 -= .04f;
            }
        }
        if (멀티 && 내팀 == 0 && (전송시간 -= 초) <= 0)
        {
            연결?.상태보내기("S" + JsonUtility.ToJson(상태));
            전송시간 = .083f;
        }
        if (상태.승자 >= 0 && 연결종료.Length == 0)
        {
            string 제목 = 상태.승자 == 내팀 ? "승리" : "패배";
            if (!패널열림 || 팝업제목 != 제목)
                팝업표시(제목, 상태.승자 == 내팀 ? "적 마을회관을 무너뜨렸습니다." : "아군 마을회관이 함락됐습니다.", false);
        }

        if (!패널열림)
        {
            키입력읽기();
            카메라이동(초);
        }
        장면반영(초);
        정보갱신();
        전장표시.MarkDirtyRepaint();
        지도.MarkDirtyRepaint();
    }

    전장대상 내왕() => 상태.대상들[전장규칙.왕번호(내팀)];

    void 메뉴전환()
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
        패널열림 = true;
        일시정지 = !멀티;
        팝업제목 = 제목;
        팝업본문 = 문장;
        팝업재개 = 재개;
        드래그중 = false;
        배치 = 대기공격 = -1;
        메뉴?.보이기(제목, 문장, 재개);
    }

    void 행동고르기(int 번호)
    {
        if (패널열림) return;
        if (번호 < 2) 배치 = 배치 == 번호 ? -1 : 번호;
        else 명령전달(명령종류.모집, 값: 번호 - 2);
    }

    Vector2 전장좌표(Vector2 화면) =>
        전장카메라.ScreenToWorldPoint(new Vector3(화면.x, 화면.y, 10));

    void 키입력읽기()
    {
        var 마우스 = Mouse.current;
        var 키 = Keyboard.current;
        if (마우스 == null || !Application.isFocused) return;
        현재마우스 = 마우스.position.ReadValue();
        Vector2 위치 = 전장좌표(현재마우스);
        if (키 != null)
        {
            if (키.spaceKey.wasPressedThisFrame) 시점복귀();
            if (키.digit1Key.wasPressedThisFrame) 행동고르기(0);
            if (키.digit2Key.wasPressedThisFrame) 행동고르기(1);
            if (키.digit3Key.wasPressedThisFrame) 행동고르기(2);
            if (키.digit4Key.wasPressedThisFrame) 행동고르기(3);
            if (키.qKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 0);
            if (키.wKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 1);
            if (키.eKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 2);
            if (키.rKey.wasPressedThisFrame) 명령전달(명령종류.스킬, 위치, 3);
        }
        if (대기공격 >= 0 && Time.unscaledTime >= 대기공격시간)
        {
            명령전달(명령종류.공격, 값: 대기공격);
            대기공격 = -1;
        }
    }

    Vector2 화면좌표(Vector2 패널좌표)
    {
        var 범위 = 전장표시.contentRect;
        return new Vector2((패널좌표.x - 범위.x) * Screen.width / 범위.width,
            (범위.yMax - 패널좌표.y) * Screen.height / 범위.height);
    }

    void 전장누르기(PointerDownEvent 입력)
    {
        if (!전투중 || 패널열림 || 입력.pointerType != UnityEngine.UIElements.PointerType.mouse) return;
        현재마우스 = 화면좌표(입력.localPosition);
        if (입력.button == 1)
        {
            if (배치 >= 0) 배치 = -1;
            else 명령전달(명령종류.이동, 전장좌표(현재마우스));
        }
        else if (입력.button == 0)
        {
            드래그중 = true;
            누른위치 = 현재마우스;
            전장표시.CapturePointer(입력.pointerId);
        }
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
        Vector2 위치 = 전장좌표(현재마우스);
        if (배치 >= 0)
        {
            명령전달(명령종류.건설, 위치, 배치);
            배치 = -1;
            return;
        }
        클릭(위치);
    }

    void 범위선택(Rect 범위)
    {
        선택부대.Clear();
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || 대상.팀 != 내팀 || 대상.건물 || 대상.종류 == 대상종류.왕) continue;
            if (범위.Contains(전장카메라.WorldToScreenPoint(대상.위치 + Vector2.up * .35f))) 선택부대.Add(i);
        }
        대기공격 = -1;
        마지막클릭 = -10;
    }

    void 클릭(Vector2 위치)
    {
        bool 두번 = Time.unscaledTime - 마지막클릭 < .31f && Vector2.Distance(이전클릭위치, 현재마우스) < 30;
        마지막클릭 = Time.unscaledTime;
        이전클릭위치 = 현재마우스;
        int 대상번호 = 클릭대상(위치);
        if (두번 && 선택부대.Count > 0)
        {
            마지막클릭 = -10;
            대기공격 = -1;
            var 부대 = new int[선택부대.Count];
            선택부대.CopyTo(부대);
            int 자원 = 채집대상(위치);
            if (자원 >= 0 && Array.Exists(부대, i => 상태.대상들[i].종류 == 대상종류.주민))
                명령전달(명령종류.채집, 위치, 자원, 부대);
            else 명령전달(명령종류.부대이동, 위치, 부대: 부대);
            return;
        }
        if (대상번호 < 0) return;
        var 대상 = 상태.대상들[대상번호];
        if (대상.팀 == 내팀 && 대상.종류 is 대상종류.병사 or 대상종류.주민)
        {
            // 선택한 부대가 있으면 더블클릭 지시를 위해 구성을 유지한다.
            if (선택부대.Count == 0) 선택부대.Add(대상번호);
        }
        else if (대상.팀 != 내팀)
        {
            if (선택부대.Count == 0) 명령전달(명령종류.공격, 값: 대상번호);
            else { 대기공격 = 대상번호; 대기공격시간 = Time.unscaledTime + .32f; }
        }
    }

    int 클릭대상(Vector2 위치)
    {
        int 결과 = -1, 앞 = int.MinValue;
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            if (대상 == null || !대상.생존 || 그림.sortingOrder <= 앞) continue;
            Vector2 간격 = 위치 - (Vector2)그림.transform.position;
            float 반너비 = 대상.건물 ? 1f : .42f;
            float 높이 = 대상.건물 ? 1.9f : 1f;
            if (Mathf.Abs(간격.x) > 반너비 || 간격.y < -.25f || 간격.y > 높이) continue;
            앞 = 그림.sortingOrder;
            결과 = i;
        }
        return 결과;
    }

    int 채집대상(Vector2 위치)
    {
        int 결과 = -1;
        float 거리 = 1.42f;
        for (int i = 0; i < 자원들.Length; i++)
        {
            float 후보 = Vector2.Distance(위치, (Vector2)자원들[i].오브젝트.position + Vector2.up * .5f);
            if (후보 >= 거리) continue;
            거리 = 후보;
            결과 = i;
        }
        return 결과;
    }

    void 시점복귀() => 자동추적 = true;

    Vector3 카메라한계(Vector2 중심)
    {
        float 높이 = 전장카메라.orthographicSize;
        float 너비 = 높이 * 전장카메라.aspect;
        float 가로한계 = Mathf.Max(0, 24 - 너비), 세로한계 = Mathf.Max(0, 14 - 높이);
        return new Vector3(Mathf.Clamp(중심.x, -가로한계, 가로한계), Mathf.Clamp(중심.y, -세로한계, 세로한계), -10);
    }

    void 카메라이동(float 초)
    {
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

    void 장면반영(float 초)
    {
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            var 그림 = 슬롯[i];
            if (대상 == null || !대상.생존)
            {
                그림.enabled = false;
                선택부대.Remove(i);
                continue;
            }
            Vector3 현재 = 그림.transform.position;
            Vector3 목표 = 대상.위치;
            그림.transform.position = 규칙 != null || !그림.enabled || Vector3.Distance(현재, 목표) > 2
                ? 목표 : Vector3.MoveTowards(현재, 목표, 초 * 6);
            그림.enabled = true;
            그림.sortingOrder = (int)(-그림.transform.position.y * 80) + 1600;
            그림.flipX = !대상.건물 && 대상.왼쪽;
            그림.color = 대상.효과시간 > 0 ? new Color(1, .8f, .67f)
                : 대상.완공대기 > 0 ? new Color(1, 1, 1, .57f) : Color.white;
        }
        건설미리보기.enabled = 배치 >= 0 && !패널열림;
        if (건설미리보기.enabled)
        {
            건설미리보기.sprite = 슬롯[전장규칙.왕번호(내팀) + 2 + 배치 * 2].sprite;
            Vector2 위치 = 길찾기.중심정렬(전장좌표(현재마우스));
            건설미리보기.transform.position = 위치;
            bool 범위안 = Vector2.Distance(위치, 상태.대상들[전장규칙.회관번호(내팀)].위치) <= 8 && 길찾기.정적통행가능(위치);
            건설미리보기.color = 범위안 ? new Color(.6f, 1, .85f, .58f) : new Color(1, .38f, .35f, .58f);
        }
    }

    void 정보갱신()
    {
        var 팀 = 상태.진영들[내팀];
        var 재화 = 팀.재화;
        자원표시.text = $"나무 {재화[0]}      돌 {재화[1]}      구리 {재화[2]}      철 {재화[3]}      미네랄 {재화[4]}";
        병력표시.text = $"병사 {전장규칙.수량(상태, 내팀, 대상종류.병사)}/{전장규칙.병력제한}   주민 {전장규칙.수량(상태, 내팀, 대상종류.주민)}/{전장규칙.주민제한}";
        int 흐른초 = Mathf.FloorToInt(상태.시간);
        시계.text = $"{흐른초 / 60:00}:{흐른초 % 60:00}";
        내회관.text = $"아군 회관 {Mathf.CeilToInt(상태.대상들[전장규칙.회관번호(내팀)].체력)}";
        적회관.text = $"적 회관 {Mathf.CeilToInt(상태.대상들[전장규칙.회관번호(1 - 내팀)].체력)}";
        var 왕 = 내왕();
        왕상태.text = 왕.생존 ? $"{팀.직업}   {Mathf.CeilToInt(왕.체력)} / {왕.최대체력}" : $"왕 재등장   {Mathf.CeilToInt(왕.재등장)}초";
        임무.text = 팀.심부름 switch
        {
            0 => "심부름 1/4   주민을 선택하고 자원을 더블 클릭하세요.",
            1 => "심부름 2/4   제재소를 지으세요.",
            2 => "심부름 3/4   훈련소를 짓고 병사 한 명을 모집하세요.",
            3 => "심부름 4/4   적 회관 근처에 병력을 보내세요.",
            _ => "심부름 완료   적 마을회관을 무너뜨리세요."
        };
        알림.text = 팀.알림시간 > 0 ? 팀.알림 : 팀.모집종류 >= 0 ? $"모집 진행 중   {Mathf.CeilToInt(팀.모집대기)}초" : "";
        표시(알림, 알림.text.Length > 0);
        도움.text = 배치 >= 0 ? "좌클릭 설치  ·  우클릭 / Esc 취소  ·  회관 반경 8" :
            $"우클릭 왕 이동  ·  좌클릭 공격  ·  드래그 선택 / 더블 클릭 명령  ·  Space 왕 시점   |   선택 {선택부대.Count}";
        if (배치 < 0 && 선택부대.Count == 1)
        {
            foreach (int 번호 in 선택부대)
            {
                var 유닛 = 상태.대상들[번호];
                var (힘, 민첩, 지능, 체력) = 전장규칙.능력치(유닛.종류);
                도움.text = $"{유닛.종류}   힘 {힘}  ·  민첩 {민첩}  ·  지능 {지능}  ·  체력 {Mathf.CeilToInt(유닛.체력)}/{체력}   |   더블 클릭 명령";
            }
        }
        for (int i = 0; i < 4; i++)
        {
            스킬버튼[i].text = "QWER"[i] + "  " + 전장규칙.스킬이름[i] + (팀.쿨다운[i] > 0 ? $"  {Mathf.CeilToInt(팀.쿨다운[i])}초" : "");
            스킬버튼[i].EnableInClassList("ready", 팀.쿨다운[i] <= 0 && 왕.생존);
            행동버튼[i].EnableInClassList("chosen", i == 배치);
        }
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

    void 전장그리기(MeshGenerationContext 그림)
    {
        if (상태 == null || !전투중) return;
        var 펜 = 그림.painter2D;
        if (배치 >= 0) 건설격자(펜);
        for (int 팀 = 0; 팀 < 상태.진영들.Length; 팀++)
        {
            var 진영 = 상태.진영들[팀];
            if (진영.스킬표시시간 <= 0) continue;
            var 중심 = 위치변환(진영.스킬위치);
            float 여운 = 진영.스킬표시시간 / .48f;
            float 반경 = Vector2.Distance(중심, 위치변환(진영.스킬위치 + Vector2.right * 진영.스킬반경)) * (1.08f - 여운 * .18f);
            var 색 = 팀 == 0 ? 청록 : 적색;
            펜.BeginPath();
            펜.Arc(중심, 반경, Angle.Degrees(0), Angle.Degrees(360));
            펜.ClosePath();
            펜.fillColor = new Color(색.r, 색.g, 색.b, 여운 * .16f);
            펜.strokeColor = new Color(색.r, 색.g, 색.b, 여운 * .8f);
            펜.lineWidth = 2;
            펜.Fill();
            펜.Stroke();
        }
        for (int i = 0; i < 슬롯.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존) continue;
            bool 선택 = 선택부대.Contains(i);
            var 위치 = (Vector2)슬롯[i].transform.position;
            var 색 = 대상.팀 == 0 ? 청록 : 적색;
            if (선택)
            {
                var 점 = 위치변환(위치 - Vector2.up * .1f);
                if (전장표시.contentRect.Contains(점))
                    네모(펜, new Rect(점.x - 12, 점.y - 3, 24, 6), new Color(.21f, .83f, .72f, .32f), 청록);
            }
            if (!선택 && !대상.건물 && 대상.종류 != 대상종류.왕 && 대상.체력 >= 대상.최대체력) continue;
            float 높이 = 대상.건물 ? 2.12f : 1.15f;
            var 위 = 위치변환(위치 + Vector2.up * 높이);
            if (!전장표시.contentRect.Contains(위)) continue;
            float 폭 = 대상.건물 ? 49 : 27;
            네모(펜, new Rect(위.x - 폭 / 2, 위.y, 폭, 5), new Color(.06f, .11f, .13f, .9f));
            float 비율 = 대상.완공대기 > 0 ? 1 - 대상.완공대기 / 6 : 대상.체력 / 대상.최대체력;
            if (비율 > 0) 네모(펜, new Rect(위.x - 폭 / 2 + 1, 위.y + 1, (폭 - 2) * 비율, 3), 대상.완공대기 > 0 ? new Color(.9f, .72f, .38f) : 색);
        }
        if (드래그중 && 배치 < 0 && Vector2.Distance(누른위치, 현재마우스) > 9)
            네모(펜, 사각형(화면변환(누른위치), 화면변환(현재마우스)), new Color(.35f, .85f, .75f, .13f), 청록);
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
            if (!바닥타일맵.HasTile(칸)) continue;
            var 왼쪽아래 = (Vector2)바닥타일맵.CellToWorld(칸);
            var 오른쪽위 = (Vector2)바닥타일맵.CellToWorld(칸 + new Vector3Int(1, 1, 0));
            var 범위 = 사각형(위치변환(왼쪽아래), 위치변환(오른쪽위));
            if (!전장표시.contentRect.Overlaps(범위)) continue;
            var 색 = 장애물타일맵.HasTile(칸) ? new Color(1, .45f, .35f, .23f) : new Color(.75f, .89f, .71f, .23f);
            네모(펜, 범위, default, 색);
        }
    }

    Vector2 지도위치(Rect 범위, Vector2 위치) =>
        new(범위.x + (위치.x + 24) / 48 * 범위.width, 범위.y + (14 - 위치.y) / 28 * 범위.height);

    void 지도그리기(MeshGenerationContext 그림)
    {
        if (상태 == null) return;
        var 펜 = 그림.painter2D;
        var 범위 = 지도.contentRect;
        foreach (var 지점 in 자원들)
        {
            var 점 = 지도위치(범위, 지점.오브젝트.position);
            네모(펜, new Rect(점.x - 1, 점.y - 1, 2, 2), new Color(.61f, .62f, .43f, .83f));
        }
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 == null || !대상.생존) continue;
            var 점 = 지도위치(범위, 대상.위치);
            float 폭 = 대상.종류 == 대상종류.회관 ? 7 : 대상.건물 || 대상.종류 == 대상종류.왕 ? 4 : 2;
            네모(펜, new Rect(점.x - 폭 / 2, 점.y - 폭 / 2, 폭, 폭), 대상.팀 == 0 ? 청록 : 적색);
        }
        var 중심 = (Vector2)전장카메라.transform.position;
        float 세로 = 전장카메라.orthographicSize, 가로 = 세로 * 전장카메라.aspect;
        var 왼쪽위 = 지도위치(범위, 중심 + new Vector2(-가로, 세로));
        var 오른쪽아래 = 지도위치(범위, 중심 + new Vector2(가로, -세로));
        네모(펜, 사각형(왼쪽위, 오른쪽아래), default, new Color(.92f, .9f, .74f, .64f));
    }

    void 지도찍기(PointerDownEvent 사건)
    {
        if (패널열림 || 사건.button != 0) return;
        var 범위 = 지도.contentRect;
        Vector2 점 = 사건.localPosition;
        자동추적 = false;
        전장카메라.transform.position = 카메라한계(new Vector2((점.x - 범위.x) / 범위.width * 48 - 24,
            14 - (점.y - 범위.y) / 범위.height * 28));
    }
}

