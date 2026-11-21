using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 회관체력표시
{
    static readonly Color32 트랙색 = new(29, 22, 19, 255);
    static readonly Color32 글자색 = new(250, 239, 217, 255);
    static readonly Color32 미탐사색 = new(191, 178, 158, 255);

    readonly Slider 아군, 적군;
    readonly TMP_Text 아군글, 적군글;

    public 회관체력표시(RectTransform 부모, Sprite 테두리, TMP_Text 아군글,
        TMP_Text 적군글, TMP_Text 시계, Color 아군색, Color 적군색)
    {
        this.아군글 = 아군글;
        this.적군글 = 적군글;

        var 줄 = new GameObject("마을회관 체력 HUD", typeof(RectTransform)).GetComponent<RectTransform>();
        줄.SetParent(부모, false);
        줄.anchorMin = 줄.anchorMax = new Vector2(.5f, 1);
        줄.pivot = new Vector2(.5f, 1);
        줄.anchoredPosition = new Vector2(0, -12);
        줄.sizeDelta = new Vector2(840, 44);

        아군 = 막대(줄, "아군 회관 슬라이더", new Vector2(0, 0), new Vector2(.5f, 1),
            Vector2.zero, new Vector2(-47, 0), 테두리, 아군색, Slider.Direction.LeftToRight, 아군글);
        적군 = 막대(줄, "적 회관 슬라이더", new Vector2(.5f, 0), Vector2.one,
            new Vector2(47, 0), Vector2.zero, 테두리, 적군색, Slider.Direction.RightToLeft, 적군글);

        var 시간틀 = 이미지(줄, "경기 시간 테두리", Color.white, 테두리);
        배치(시간틀.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(-42, 0), new Vector2(42, 0));
        문구배치(시계, 시간틀.rectTransform, 24);
    }

    public void 갱신(전장대상 아군회관, 전장대상 적회관, bool 적발견)
    {
        아군.SetValueWithoutNotify(비율(아군회관));
        아군글.SetText("아군 회관   {0:0} / {1:0}", Mathf.Ceil(아군회관.체력), Mathf.Ceil(아군회관.최대체력));

        적군.SetValueWithoutNotify(적발견 ? 비율(적회관) : 0);
        적군글.color = 적발견 ? 글자색 : 미탐사색;
        if (적발견)
            적군글.SetText("적 회관   {0:0} / {1:0}", Mathf.Ceil(적회관.체력), Mathf.Ceil(적회관.최대체력));
        else
            적군글.SetText("적 회관 미탐사");
    }

    static float 비율(전장대상 회관) => 회관.최대체력 > 0 ? 회관.체력 / 회관.최대체력 : 0;

    static Slider 막대(RectTransform 부모, string 이름, Vector2 왼쪽앵커, Vector2 오른쪽앵커,
        Vector2 왼쪽여백, Vector2 오른쪽여백, Sprite 테두리, Color 색, Slider.Direction 방향, TMP_Text 글)
    {
        var 틀 = 이미지(부모, 이름, Color.white, 테두리);
        배치(틀.rectTransform, 왼쪽앵커, 오른쪽앵커, 왼쪽여백, 오른쪽여백);

        var 트랙 = 이미지(틀.rectTransform, "체력 트랙", 트랙색);
        배치(트랙.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 7), new Vector2(-8, -7));
        var 채움 = 이미지(트랙.rectTransform, "현재 체력", Color.Lerp(트랙색, 색, .62f));
        배치(채움.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var 밝은면 = 이미지(채움.rectTransform, "윗면", 색);
        배치(밝은면.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -4), Vector2.zero);

        var 슬라이더 = 틀.gameObject.AddComponent<Slider>();
        슬라이더.interactable = false;
        슬라이더.navigation = new Navigation { mode = Navigation.Mode.None };
        슬라이더.direction = 방향;
        슬라이더.fillRect = 채움.rectTransform;
        문구배치(글, 틀.rectTransform, 17);
        return 슬라이더;
    }

    static Image 이미지(Transform 부모, string 이름, Color 색, Sprite 그림 = null)
    {
        var 이미지 = new GameObject(이름, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        이미지.transform.SetParent(부모, false);
        이미지.raycastTarget = false;
        이미지.color = 색;
        이미지.sprite = 그림;
        if (그림 != null && 그림.border != Vector4.zero)
        {
            이미지.type = Image.Type.Sliced;
            이미지.pixelsPerUnitMultiplier = 1.5f;
        }
        return 이미지;
    }

    static void 문구배치(TMP_Text 글, RectTransform 부모, float 크기)
    {
        var 영역 = 글.rectTransform;
        영역.SetParent(부모, false);
        배치(영역, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
        글.raycastTarget = false;
        글.alignment = TextAlignmentOptions.Center;
        글.fontSize = 크기;
        글.color = 글자색;
        글.gameObject.SetActive(true);
    }

    static void 배치(RectTransform 영역, Vector2 왼쪽앵커, Vector2 오른쪽앵커, Vector2 왼쪽여백, Vector2 오른쪽여백)
    {
        영역.anchorMin = 왼쪽앵커;
        영역.anchorMax = 오른쪽앵커;
        영역.offsetMin = 왼쪽여백;
        영역.offsetMax = 오른쪽여백;
    }
}
