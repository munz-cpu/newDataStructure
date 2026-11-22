using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 유닛상태창
{
    const float 너비 = 202, 높이 = 158, 여백 = 8, 등장거리 = 13;
    const float 등장시간 = .24f, 채움시간 = .38f, 채움간격 = .045f;
    static readonly Color32 제목색 = new(246, 210, 145, 255);
    static readonly Color32 글자색 = new(246, 236, 217, 255);
    static readonly Color32 트랙색 = new(26, 22, 20, 255);

    readonly RectTransform 부모, 상자;
    readonly CanvasGroup 투명도;
    readonly TMP_Text 제목;
    readonly Button 추적버튼;
    readonly RectTransform[] 채움들 = new RectTransform[4];
    readonly float[] 비율들 = new float[4];
    readonly float 능력치눈금;
    int 표시번호 = -1;
    float 등장시각, 표시체력;

    public GameObject 오브젝트 => 상자.gameObject;

    public 유닛상태창(RectTransform 부모, TMP_FontAsset 글꼴, Sprite 테두리, System.Action<int> 추적요청)
    {
        this.부모 = 부모;
        var 배경 = 이미지(부모, "유닛 상태창", 테두리 != null ? Color.white : new Color32(48, 35, 27, 248));
        if (테두리 != null)
        {
            배경.sprite = 테두리;
            배경.type = Image.Type.Sliced;
            배경.pixelsPerUnitMultiplier = 1.5f;
        }
        상자 = 배경.rectTransform;
        상자.anchorMin = 상자.anchorMax = 부모.pivot;
        상자.pivot = new Vector2(.5f, .5f);
        상자.sizeDelta = new Vector2(너비, 높이);
        투명도 = 오브젝트.AddComponent<CanvasGroup>();
        투명도.interactable = true;
        투명도.blocksRaycasts = true;
        제목 = 글(글꼴, "주민", 17, 10, 168, 23, 19, 제목색);
        var 구분선 = 이미지(상자, "구분선", new Color32(136, 107, 71, 210));
        배치(구분선.rectTransform, 17, 36, 168, 1);

        막대(0, 글꼴, "체력", new Color32(224, 119, 98, 255));
        막대(1, 글꼴, "힘", new Color32(229, 165, 90, 255));
        막대(2, 글꼴, "민첩", new Color32(132, 185, 125, 255));
        막대(3, 글꼴, "지능", new Color32(119, 180, 206, 255));
        추적버튼 = Object.Instantiate(Resources.Load<GameObject>("UnitFollowButton"), 상자, false).GetComponent<Button>();
        추적버튼.onClick.AddListener(() => 추적요청(표시번호));

        int 최대보정 = 0;
        foreach (var 단계 in 전장규칙.주민투자목록)
            최대보정 = Mathf.Max(최대보정, 단계.최대보정);
        var 주민 = 전장규칙.능력치(대상종류.주민);
        var 병사 = 전장규칙.능력치(대상종류.병사);
        능력치눈금 = Mathf.Max(
            전장규칙.주민능력치보정(Mathf.Max(주민.힘, 주민.민첩, 주민.지능), 최대보정),
            Mathf.Max(병사.힘, 병사.민첩, 병사.지능));
        숨기기();
    }

    public bool 버튼가리킴(Vector2 화면) => 오브젝트.activeSelf
        && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)추적버튼.transform, 화면, null);

    public void 갱신(int 번호, 전장대상 대상, Bounds 유닛범위, Camera 카메라, bool 추적중)
    {
        var 색들 = 추적버튼.colors;
        Color 기본색 = 추적중 ? (Color)new Color32(142, 218, 186, 255) : Color.white;
        if (색들.normalColor != 기본색)
        {
            색들.normalColor = 기본색;
            색들.highlightedColor = 추적중 ? 기본색 : Color.white * 1.12f;
            색들.selectedColor = 기본색;
            추적버튼.colors = 색들;
        }
        var 중심 = 카메라.WorldToScreenPoint(유닛범위.center);
        var 위 = 카메라.WorldToScreenPoint(new Vector3(유닛범위.center.x, 유닛범위.max.y, 유닛범위.center.z));
        var 아래 = 카메라.WorldToScreenPoint(new Vector3(유닛범위.center.x, 유닛범위.min.y, 유닛범위.center.z));
        var 화면 = 카메라.pixelRect;
        if (중심.z <= 0 || 중심.x < 화면.xMin || 중심.x > 화면.xMax
            || 위.y < 화면.yMin || 아래.y > 화면.yMax)
        {
            숨기기();
            return;
        }

        float 지금 = Time.unscaledTime;
        float 체력비율 = 대상.최대체력 > 0 ? Mathf.Clamp01(대상.체력 / 대상.최대체력) : 0;
        if (표시번호 != 번호)
        {
            표시번호 = 번호;
            등장시각 = 지금;
            표시체력 = 체력비율;
            제목.text = 대상.병과 != 병과.일반 ? 대상.병과.ToString() : 대상.해골병사 ? "정예 해골 병사" : 대상.종류 == 대상종류.병사 ? "병사" : "주민";
            상자.SetAsLastSibling();
            오브젝트.SetActive(true);
        }

        float 경과 = 지금 - 등장시각;
        float 등장 = 감속(경과 / 등장시간);
        투명도.alpha = Mathf.SmoothStep(0, 1, 경과 / 등장시간);
        상자.localScale = Vector3.one * Mathf.Lerp(.94f, 1, 등장);
        위치갱신(위, 아래, 등장);

        표시체력 = Mathf.Lerp(표시체력, 체력비율, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
        var (힘, 민첩, 지능, _) = 전장규칙.능력치(대상);
        비율들[0] = 표시체력;
        비율들[1] = 힘 / 능력치눈금;
        비율들[2] = 민첩 / 능력치눈금;
        비율들[3] = 지능 / 능력치눈금;
        for (int i = 0; i < 채움들.Length; i++)
        {
            float 차오름 = 감속((경과 - .09f - i * 채움간격) / 채움시간);
            var 채움 = 채움들[i];
            채움.anchorMax = new Vector2(Mathf.Clamp01(비율들[i]) * 차오름, 1);
        }
    }

    public void 숨기기()
    {
        표시번호 = -1;
        if (오브젝트.activeSelf) 오브젝트.SetActive(false);
    }

    void 위치갱신(Vector2 위, Vector2 아래, float 등장)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 위, null, out var 머리);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(부모, 아래, null, out var 발);
        var 범위 = 부모.rect;
        float 반폭 = 너비 * .5f, 반높이 = 높이 * .5f;
        float 최소X = 범위.xMin + 반폭 + 여백, 최대X = 범위.xMax - 반폭 - 여백;
        float 최소Y = 범위.yMin + 반높이 + 여백, 최대Y = 범위.yMax - 반높이 - 여백;
        float 간격 = 여백 + 반높이;
        float 목표Y = 머리.y + 간격 <= 최대Y ? 머리.y + 간격 : 발.y - 간격;
        상자.anchoredPosition = new Vector2(
            최소X <= 최대X ? Mathf.Clamp(머리.x, 최소X, 최대X) : 범위.center.x,
            최소Y <= 최대Y ? Mathf.Clamp(목표Y - 등장거리 * (1 - 등장), 최소Y, 최대Y) : 범위.center.y);
    }

    void 막대(int 번호, TMP_FontAsset 글꼴, string 항목, Color 색)
    {
        float 위 = 42 + 번호 * 19;
        글(글꼴, 항목, 17, 위, 43, 19, 14, 글자색);
        var 트랙 = 이미지(상자, 항목 + " 트랙", 트랙색);
        배치(트랙.rectTransform, 65, 위 + 5, 120, 10);
        var 안쪽 = 이미지(트랙.rectTransform, "홈", new Color32(56, 49, 43, 255)).rectTransform;
        안쪽.anchorMin = Vector2.zero;
        안쪽.anchorMax = Vector2.one;
        안쪽.offsetMin = Vector2.one;
        안쪽.offsetMax = -Vector2.one;
        var 채움 = 이미지(안쪽, "채움", Color.Lerp(트랙색, 색, .78f)).rectTransform;
        채움.anchorMin = Vector2.zero;
        채움.anchorMax = Vector2.up;
        채움.offsetMin = 채움.offsetMax = Vector2.zero;
        채움들[번호] = 채움;
        var 윗면 = 이미지(채움, "윗면", 색).rectTransform;
        윗면.anchorMin = new Vector2(0, 1);
        윗면.anchorMax = Vector2.one;
        윗면.offsetMin = new Vector2(0, -2);
        윗면.offsetMax = Vector2.zero;
    }

    TMP_Text 글(TMP_FontAsset 글꼴, string 내용, float 왼쪽, float 위, float 폭, float 줄높이, int 크기, Color 색)
    {
        var 글 = new GameObject(내용, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        글.rectTransform.SetParent(상자, false);
        배치(글.rectTransform, 왼쪽, 위, 폭, 줄높이);
        글.font = 글꼴;
        글.text = 내용;
        글.fontSize = 크기;
        글.color = 색;
        글.alignment = TextAlignmentOptions.MidlineLeft;
        글.textWrappingMode = TextWrappingModes.NoWrap;
        글.raycastTarget = false;
        return 글;
    }

    static Image 이미지(Transform 부모, string 이름, Color 색)
    {
        var 그림 = new GameObject(이름, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        그림.rectTransform.SetParent(부모, false);
        그림.color = 색;
        그림.raycastTarget = false;
        return 그림;
    }

    static void 배치(RectTransform 영역, float 왼쪽, float 위, float 폭, float 높이)
    {
        영역.anchorMin = 영역.anchorMax = 영역.pivot = Vector2.up;
        영역.anchoredPosition = new Vector2(왼쪽, -위);
        영역.sizeDelta = new Vector2(폭, 높이);
    }

    static float 감속(float 진행)
    {
        float 남은비율 = 1 - Mathf.Clamp01(진행);
        return 1 - 남은비율 * 남은비율 * 남은비율;
    }
}
