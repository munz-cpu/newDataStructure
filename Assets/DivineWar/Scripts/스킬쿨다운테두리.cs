using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class 스킬쿨다운테두리 : MaskableGraphic
{
    const float 두께 = 2f;
    const float 좌우여백 = 10f;
    const float 아래여백 = 7f;
    float 채움 = 1f;
    TextMeshProUGUI 남은시간글;
    int 표시초;

    public void 문구준비(TMP_FontAsset 글꼴, string 스킬이름)
    {
        if (남은시간글 != null) return;
        var 이름 = 문구생성("스킬 이름", 글꼴, new Vector2(59, 27), new Vector2(78, 27), 19,
            new Color32(229, 240, 243, 255));
        이름.text = 스킬이름;
        남은시간글 = 문구생성("남은 쿨다운", 글꼴, new Vector2(59, 10), new Vector2(78, 20), 14,
            new Color32(169, 195, 204, 255));
        남은시간글.gameObject.SetActive(false);
    }

    public void 진행표시(float 남은시간, float 쿨다운길이, bool 사용가능)
    {
        int 초 = Mathf.Max(0, Mathf.CeilToInt(남은시간));
        if (남은시간글 != null && 표시초 != 초)
        {
            표시초 = 초;
            if (초 > 0) 남은시간글.SetText("{0:0}초", 초);
            남은시간글.gameObject.SetActive(초 > 0);
        }

        float 진행률 = 사용가능 ? Mathf.Clamp01(1 - 남은시간 / 쿨다운길이) : 0;
        if (채움 == 진행률) return;
        채움 = 진행률;
        SetVerticesDirty();
    }

    TextMeshProUGUI 문구생성(string 이름, TMP_FontAsset 글꼴, Vector2 위치, Vector2 크기, float 글자크기, Color 글자색)
    {
        var 문구 = new GameObject(이름, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var 영역 = 문구.rectTransform;
        영역.SetParent(rectTransform.parent, false);
        영역.anchorMin = 영역.anchorMax = 영역.pivot = Vector2.zero;
        영역.anchoredPosition = 위치;
        영역.sizeDelta = 크기;
        문구.font = 글꼴;
        문구.fontSize = 글자크기;
        문구.color = 글자색;
        문구.alignment = TextAlignmentOptions.MidlineLeft;
        문구.textWrappingMode = TextWrappingModes.NoWrap;
        문구.raycastTarget = false;
        return 문구;
    }

    protected override void OnPopulateMesh(VertexHelper 메시)
    {
        메시.Clear();
        var 범위 = rectTransform.rect;
        float 왼쪽 = 범위.xMin + 좌우여백;
        float 아래 = 범위.yMin + 아래여백;
        float 길이 = (범위.width - 좌우여백 * 2) * 채움;
        사각형(메시, 왼쪽, 아래, 왼쪽 + 길이, 아래 + 두께);
    }

    void 사각형(VertexHelper 메시, float 왼쪽, float 아래, float 오른쪽, float 위)
    {
        if (왼쪽 >= 오른쪽 || 아래 >= 위) return;
        int 시작 = 메시.currentVertCount;
        메시.AddVert(new Vector3(왼쪽, 아래), color, Vector2.zero);
        메시.AddVert(new Vector3(왼쪽, 위), color, Vector2.zero);
        메시.AddVert(new Vector3(오른쪽, 위), color, Vector2.zero);
        메시.AddVert(new Vector3(오른쪽, 아래), color, Vector2.zero);
        메시.AddTriangle(시작, 시작 + 1, 시작 + 2);
        메시.AddTriangle(시작 + 2, 시작 + 3, 시작);
    }
}
