using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 신들의전쟁
{
    readonly List<(TMP_Text 글, int 종류, string 이름)> 건설수량표시 = new();
    bool 망치커서중;
    const float 망치타격길이 = .43f;
    float 망치타격시작 = float.NegativeInfinity;
    Vector2 망치타격위치;
    bool 망치타격중 => Time.unscaledTime - 망치타격시작 < 망치타격길이;
    AudioSource 건설효과음재생기;
    readonly List<float> 건설타격예정 = new();

    void 건설소리준비()
    {
        if (나무채집효과음 == null || 건설효과음재생기 != null) return;
        // 기존 오브젝트에 전용 채널만 추가해 다른 효과음의 피치를 바꾸지 않는다.
        건설효과음재생기 = gameObject.AddComponent<AudioSource>();
        건설효과음재생기.playOnAwake = false;
        건설효과음재생기.spatialBlend = 0;
        건설효과음재생기.outputAudioMixerGroup = 효과음재생기.outputAudioMixerGroup;
    }

    void 건설소리시작()
    {
        if (건설효과음재생기 == null) return;
        건설타격예정.Add(상태.시간 + .16f);
    }

    void 건설소리갱신()
    {
        if (일시정지 || 상태.승자 >= 0 || 건설효과음재생기 == null) return;
        for (int i = 0; i < 건설타격예정.Count;)
        {
            if (상태.시간 < 건설타격예정[i]) { i++; continue; }
            건설타격예정.RemoveAt(i);
            건설효과음재생기.pitch = Random.Range(.92f, 1.08f);
            건설효과음재생기.PlayOneShot(나무채집효과음, Random.Range(.8f, 1f));
        }
    }

    void 건설소리해제()
    {
        건설타격예정.Clear();
        if (건설효과음재생기 != null) 건설효과음재생기.Stop();
    }

    void 건설표시준비()
    {
        건설수량표시.Clear();
        void 추가(메뉴버튼 버튼, int 종류, string 이름)
        {
            if (버튼 == null) return;
            var 글 = 버튼.GetComponentInChildren<TMP_Text>(true);
            if (글 == null) return;
            글.enableAutoSizing = true;
            글.fontSizeMin = 11;
            건설수량표시.Add((글, 종류, 이름));
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

    void 건설수량갱신()
    {
        foreach (var (글, 종류, 이름) in 건설수량표시)
        {
            string 표시 = $"{이름}  {전장규칙.건설수량(상태, 내팀, 종류)} / {전장규칙.건설정원(상태, 내팀, 종류)}";
            if (글.text != 표시) 글.text = 표시;
        }
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
        Cursor.visible = !표시;
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
        모닥불건설 => 1,
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
