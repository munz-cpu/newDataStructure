using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public sealed partial class 신들의전쟁
{
    [SerializeField] TMP_SpriteAsset 건축재료아이콘;
    readonly List<(메뉴버튼 버튼, TMP_Text 글, int 종류, string 이름)> 건설수량표시 = new();
    VisualElement 건설설명상자;
    Label 건설설명제목, 건설설명본문;
    int 설명중인건물 = -1;
    bool 망치커서중;
    const float 망치타격길이 = .43f;
    float 망치타격시작 = float.NegativeInfinity;
    Vector2 망치타격위치;
    bool 망치타격중 => Time.unscaledTime - 망치타격시작 < 망치타격길이;
    AudioSource 건설효과음재생기;
    readonly List<float> 건설타격예정 = new();
    readonly bool[] 이전에건설중 = new bool[전장규칙.전체칸];
    float 다음선택음시각, 다음이동명령음시각;

    void 선택효과음재생()
    {
        if (전투중 && !일시정지 && 선택부대.Count > 0)
            전장확인음재생(유닛선택효과음, .48f, ref 다음선택음시각);
    }

    void 전장확인음재생(AudioClip 소리, float 음량, ref float 다음재생시각)
    {
        float 현재시각 = Time.unscaledTime;
        if (소리 == null || 효과음재생기 == null || 현재시각 < 다음재생시각) return;
        효과음재생기.PlayOneShot(소리, 음량);
        다음재생시각 = 현재시각 + Mathf.Max(.1f, 소리.length);
    }

    void 건설소리준비()
    {
        if (나무채집효과음 == null || 건설효과음재생기 != null) return;
        // 기존 오브젝트에 전용 채널만 추가해 다른 효과음의 피치를 바꾸지 않는다.
        건설효과음재생기 = gameObject.AddComponent<AudioSource>();
        건설효과음재생기.playOnAwake = false;
        건설효과음재생기.spatialBlend = 0;
        if (효과음재생기 != null) 건설효과음재생기.outputAudioMixerGroup = 효과음재생기.outputAudioMixerGroup;
    }

    void 효과음음량갱신(float 음량)
    {
        if (효과음재생기 != null) 효과음재생기.volume = 음량;
        if (건설효과음재생기 != null) 건설효과음재생기.volume = 음량;
    }

    void 건설소리시작()
    {
        if (건설효과음재생기 == null) return;
        건설타격예정.Add(상태.시간 + .16f);
    }

    void 건설소리갱신()
    {
        if (일시정지 || 상태.승자 >= 0) return;
        건설완료음갱신();
        if (건설효과음재생기 == null) return;
        for (int i = 0; i < 건설타격예정.Count;)
        {
            if (상태.시간 < 건설타격예정[i]) { i++; continue; }
            건설타격예정.RemoveAt(i);
            건설효과음재생기.pitch = Random.Range(.92f, 1.08f);
            건설효과음재생기.PlayOneShot(나무채집효과음, Random.Range(.8f, 1f));
        }
    }

    void 건설완료음갱신()
    {
        if (효과음재생기 == null || 건설완료효과음 == null) return;
        bool 완공됨 = false;
        for (int i = 0; i < 상태.대상들.Length; i++)
        {
            var 대상 = 상태.대상들[i];
            bool 아군건물 = 대상 != null && 대상.팀 == 내팀 && 대상.생존 && 대상.건물;
            bool 건설중 = 아군건물 && 대상.완공대기 > 0;
            완공됨 |= 이전에건설중[i] && 아군건물 && !건설중;
            이전에건설중[i] = 건설중;
        }
        if (완공됨) 효과음재생기.PlayOneShot(건설완료효과음, .68f);
    }

    void 건설소리해제()
    {
        건설타격예정.Clear();
        System.Array.Clear(이전에건설중, 0, 이전에건설중.Length);
        다음선택음시각 = 다음이동명령음시각 = 0;
        if (건설효과음재생기 != null) 건설효과음재생기.Stop();
    }

    void 건설표시준비()
    {
        건설설명상자 = 뿌리.Q("building-tooltip");
        건설설명상자.style.unityFont = 도움.font.sourceFontFile;
        건설설명제목 = 건설설명상자.Q<Label>("building-tooltip-title");
        건설설명본문 = 건설설명상자.Q<Label>("building-tooltip-detail");
        건설설명숨기기();
        건설수량표시.Clear();
        void 추가(메뉴버튼 버튼, int 종류, string 이름)
        {
            if (버튼 == null) return;
            var 글 = 버튼.GetComponentInChildren<TMP_Text>(true);
            if (글 == null) return;
            글.enableAutoSizing = true;
            글.fontSizeMin = 11;
            건설수량표시.Add((버튼, 글, 종류, 이름));

            var 비용글 = 버튼.transform.Find("건축 비용")?.GetComponent<TMP_Text>();
            if (비용글 == null || 건축재료아이콘 == null) return;
            비용글.spriteAsset = 건축재료아이콘;
            비용글.text = 건설비용문구(종류);
        }
        추가(행동버튼[0], 0, "제재소");
        추가(행동버튼[1], 1, "훈련소");
        추가(행동버튼[4], 4, "나무 울타리");
        추가(행동버튼[5], 5, "돌 울타리");
        추가(행동버튼[6], 6, "모닥불");
        string[] 이름들 = { "브루저 훈련소", "아처 훈련소", "아처타워", "지뢰", "점프패드" };
        for (int i = 0; i < 시설버튼.Length; i++) 추가(시설버튼[i], i + 전장규칙.브루저건설, 이름들[i]);
        추가(깃발버튼, 전장규칙.깃발건설, "깃발");
        foreach (var 그림 in 건설커서.GetComponentsInChildren<Graphic>(true)) 그림.raycastTarget = false;
        건설커서.sizeDelta = new Vector2(42, 42);
        망치타격시작 = float.NegativeInfinity;
    }

    static string 건설비용문구(int 종류)
    {
        var 문구 = new StringBuilder();
        if (종류 == 전장규칙.타워건설) 문구.Append("아처 1명 + ");
        var 수량들 = 전장규칙.비용[종류];
        bool 재료표시됨 = false;
        for (int i = 0; i < 수량들.Length; i++)
        {
            if (수량들[i] == 0) continue;
            if (재료표시됨) 문구.Append("   ");
            문구.Append("<sprite name=\"").Append(채집아이콘이름[i]).Append("\"> ").Append(수량들[i]);
            재료표시됨 = true;
        }
        if (종류 is 전장규칙.브루저건설 or 전장규칙.아처건설) 문구.Append("  /  기본 훈련 2초");
        if (종류 == 전장규칙.깃발건설) 문구.Append($"  /  건설 영역 +{전장규칙.건설반경}칸");
        return 문구.ToString();
    }

    void 건설수량갱신()
    {
        foreach (var (_, 글, 종류, 이름) in 건설수량표시)
        {
            string 표시 = $"{이름}  {전장규칙.건설수량(상태, 내팀, 종류)} / {전장규칙.건설정원(상태, 내팀, 종류)}";
            if (글.text != 표시) 글.text = 표시;
        }
    }

    static string 건물설명(int 종류) => 종류 switch
    {
        0 => "완공되면 5초마다 나무 7개를 자동으로 생산합니다.",
        1 => "아군 주민을 일반 병사로 훈련합니다.",
        전장규칙.나무울타리건설 => "유닛의 이동을 막는 저렴한 방어벽입니다. 배치 중 V로 방향을 돌릴 수 있습니다.",
        전장규칙.돌울타리건설 => "나무 울타리보다 체력이 높은 방어벽입니다. 배치 중 V로 방향을 돌릴 수 있습니다.",
        전장규칙.모닥불건설 => "날고기를 구울 수 있습니다. 반경 3칸 안의 아군 병사는 초당 체력 2를 회복합니다.",
        전장규칙.브루저건설 => "일반 병사를 체력이 높은 근접 병과인 브루저로 훈련합니다.",
        전장규칙.아처건설 => "일반 병사를 멀리서 화살을 쏘는 아처로 훈련합니다.",
        전장규칙.타워건설 => "주변 적 유닛에게 자동으로 화살을 쏩니다. 아처 1명이 주둔하며, 직접 철거하면 돌아옵니다.",
        전장규칙.지뢰건설 => "적이 가까이 오면 한 번 폭발해 주변 적 유닛에게 피해를 줍니다.",
        전장규칙.점프건설 => "밟은 아군을 이동하거나 바라보는 방향으로 최대 4칸 도약시킵니다.",
        전장규칙.깃발건설 => $"완공되면 깃발 반경 {전장규칙.건설반경}칸 안에도 건물을 지을 수 있습니다.",
        _ => ""
    };

    void 건설설명갱신()
    {
        if (건축목록열림 && Application.isFocused && 뿌리.panel != null)
        {
            foreach (var (버튼, _, 종류, 이름) in 건설수량표시)
            {
                if (!버튼.isActiveAndEnabled || !버튼.포인터안) continue;
                건설설명표시((RectTransform)버튼.transform, 종류, 이름);
                return;
            }
        }
        건설설명숨기기();
    }

    void 건설설명표시(RectTransform 버튼, int 종류, string 이름)
    {
        if (설명중인건물 != 종류)
        {
            설명중인건물 = 종류;
            건설설명제목.text = 이름;
            건설설명본문.text = 건물설명(종류);
        }

        Vector2 왼쪽위(RectTransform 영역) => 화면변환(RectTransformUtility.WorldToScreenPoint(
            null, 영역.TransformPoint(new Vector2(영역.rect.xMin, 영역.rect.yMax))));
        var 화면 = 뿌리.contentRect;
        var 목록위치 = 왼쪽위((RectTransform)건축목록.transform);
        float 너비 = Mathf.Min(276, 화면.width * .86f);
        float 높이 = 건설설명상자.resolvedStyle.height;
        if (!float.IsFinite(높이) || 높이 <= 0) 높이 = 92;
        const float 여백 = 8;
        bool 왼쪽배치 = 목록위치.x >= 너비 + 여백 * 2;
        건설설명상자.style.width = 너비;
        건설설명상자.style.left = 왼쪽배치 ? 목록위치.x - 너비 - 여백
            : Mathf.Clamp(목록위치.x, 여백, Mathf.Max(여백, 화면.width - 너비 - 여백));
        float 위 = 왼쪽배치 ? 왼쪽위(버튼).y : 목록위치.y - 높이 - 여백;
        건설설명상자.style.top = Mathf.Clamp(위, 여백, Mathf.Max(여백, 화면.height - 높이 - 여백));
        표시(건설설명상자, true);
    }

    void 건설설명숨기기()
    {
        if (설명중인건물 < 0) return;
        설명중인건물 = -1;
        표시(건설설명상자, false);
    }

    void 망치커서표시(bool 표시)
    {
        건설커서.gameObject.SetActive(표시);
        if (!표시)
        {
            망치타격시작 = float.NegativeInfinity;
            건설커서.localRotation = Quaternion.identity;
            건설커서.localScale = Vector3.one;
        }
        if (망치커서중 == 표시) return;
        망치커서중 = 표시;
        UnityEngine.Cursor.visible = !표시;
    }

    void 망치내려찍기()
    {
        망치타격시작 = Time.unscaledTime;
        망치타격위치 = 현재마우스;
    }

    void 망치커서갱신()
    {
        망치커서표시(true);
        var 부모 = (RectTransform)건설커서.parent;
        Vector2 화면위치 = 망치타격중 ? 망치타격위치 : 현재마우스;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 화면위치, null, out var 커서위치);
        var 자세 = 망치자세(Time.unscaledTime - 망치타격시작);
        var 회전축 = new Vector2(.18f, .16f);
        건설커서.pivot = 회전축;
        // 손잡이를 축으로 돌리되 대기 자세의 망치 머리는 포인터에 맞춘다.
        var 손잡이보정 = Vector2.Scale(회전축 - new Vector2(.75f, .83f), 건설커서.rect.size);
        건설커서.anchoredPosition = 커서위치 - new Vector2(부모.rect.xMin, 부모.rect.yMax) + 손잡이보정 + 자세.이동;
        건설커서.localRotation = Quaternion.Euler(0, 0, 자세.각도);
        건설커서.localScale = new Vector3(자세.크기.x, 자세.크기.y, 1);
    }

    // 들어올리기 → 가속 타격 → 짧은 히트 스톱 → 반동 → 탄성 복귀.
    static (Vector2 이동, float 각도, Vector2 크기) 망치자세(float 시간)
    {
        if (시간 < 0 || 시간 >= 망치타격길이) return (Vector2.zero, 0, Vector2.one);
        if (시간 < .09f)
        {
            float t = 시간 / .09f;
            float e = 1 - Mathf.Pow(1 - t, 3);
            return (new Vector2(7, 17) * e, 38 * e, Vector2.Lerp(Vector2.one, new Vector2(.94f, 1.1f), e));
        }
        if (시간 < .16f)
        {
            float t = (시간 - .09f) / .07f;
            float e = t * t * t;
            return (Vector2.Lerp(new Vector2(7, 17), new Vector2(-3, -7), e),
                Mathf.Lerp(38, -48, e), Vector2.Lerp(new Vector2(.94f, 1.1f), new Vector2(1.2f, .78f), e));
        }
        if (시간 < .195f) return (new Vector2(-3, -7), -48, new Vector2(1.2f, .78f));
        if (시간 < .29f)
        {
            float t = (시간 - .195f) / .095f;
            float e = 1 - Mathf.Pow(1 - t, 3);
            return (Vector2.Lerp(new Vector2(-3, -7), new Vector2(2, 5), e),
                Mathf.Lerp(-48, 12, e), Vector2.Lerp(new Vector2(1.2f, .78f), new Vector2(.97f, 1.06f), e));
        }
        float 복귀 = (시간 - .29f) / .14f;
        float 탄성 = Mathf.Pow(1 - 복귀, 2) * Mathf.Cos(복귀 * Mathf.PI * 2);
        return (new Vector2(2, 5) * 탄성, 12 * 탄성, Vector2.one + new Vector2(-.03f, .06f) * 탄성);
    }
}

public sealed partial class 전장규칙
{
    public static int 건설정원(int 종류) => 종류 switch
    {
        0 or 1 or 브루저건설 or 아처건설 => 2,
        나무울타리건설 or 돌울타리건설 => 기본울타리한도,
        모닥불건설 => 모닥불한도,
        타워건설 => 4,
        지뢰건설 or 점프건설 => 8,
        깃발건설 => 기본깃발한도,
        _ => 0
    };

    public static int 건설수량(전장상태 상태, int 팀, int 종류)
    {
        int 수 = 0;
        대상종류 유형 = 종류 switch
        {
            0 => 대상종류.제재소, 1 => 대상종류.훈련소,
            모닥불건설 => 대상종류.모닥불, 깃발건설 => 대상종류.깃발,
            _ => 시설종류(종류)
        };
        foreach (var 대상 in 상태.대상들)
            if (대상 is { 생존: true } && 대상.팀 == 팀
                && (울타리건설인가(종류) ? 대상.울타리 : 대상.종류 == 유형)) 수++;
        return 수;
    }
}
