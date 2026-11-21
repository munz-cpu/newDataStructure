using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class 스킬쿨다운테두리 : MaskableGraphic
{
    const float 두께 = 1.5f;
    const float 여백 = 4f;
    static readonly Color32 준비색 = new(174, 135, 80, 180);
    static readonly Color32 진행색 = new(239, 193, 108, 255);
    float 채움 = 1f;
    bool 대기중;
    TextMeshProUGUI 남은시간글;
    int 표시초;

    public void 문구준비(TMP_FontAsset 글꼴)
    {
        if (남은시간글 != null) return;
        남은시간글 = new GameObject("남은 쿨다운", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        var 영역 = 남은시간글.rectTransform;
        영역.SetParent(rectTransform, false);
        영역.anchorMin = Vector2.zero;
        영역.anchorMax = Vector2.right;
        영역.offsetMin = new Vector2(여백 + 두께, 6);
        영역.offsetMax = new Vector2(-여백 - 두께, 16);
        남은시간글.font = 글꼴;
        남은시간글.fontSize = 11;
        남은시간글.color = new Color32(239, 193, 108, 255);
        남은시간글.alignment = TextAlignmentOptions.Midline;
        남은시간글.textWrappingMode = TextWrappingModes.NoWrap;
        남은시간글.enableAutoSizing = true;
        남은시간글.fontSizeMin = 9;
        남은시간글.fontSizeMax = 11;
        남은시간글.overflowMode = TextOverflowModes.Truncate;
        남은시간글.raycastTarget = false;
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
        bool 재사용대기 = 초 > 0;
        if (채움 == 진행률 && 대기중 == 재사용대기) return;
        채움 = 진행률;
        대기중 = 재사용대기;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper 메시)
    {
        메시.Clear();
        var 범위 = rectTransform.rect;
        float 왼쪽 = 범위.xMin + 여백;
        float 오른쪽 = 범위.xMax - 여백;
        float 아래 = 범위.yMin + 여백;
        float 위 = 범위.yMax - 여백;
        float 가로 = 오른쪽 - 왼쪽;
        float 세로 = 위 - 아래 - 두께 * 2;
        if (가로 <= 두께 * 2 || 세로 <= 0) return;

        float 진행길이 = (가로 + 세로) * 2 * 채움;
        사각형(메시, 왼쪽, 아래, 왼쪽 + Mathf.Clamp(진행길이, 0, 가로), 아래 + 두께);
        진행길이 -= 가로;
        사각형(메시, 오른쪽 - 두께, 아래 + 두께, 오른쪽, 아래 + 두께 + Mathf.Clamp(진행길이, 0, 세로));
        진행길이 -= 세로;
        사각형(메시, 오른쪽 - Mathf.Clamp(진행길이, 0, 가로), 위 - 두께, 오른쪽, 위);
        진행길이 -= 가로;
        사각형(메시, 왼쪽, 위 - 두께 - Mathf.Clamp(진행길이, 0, 세로), 왼쪽 + 두께, 위 - 두께);
    }

    void 사각형(VertexHelper 메시, float 왼쪽, float 아래, float 오른쪽, float 위)
    {
        if (왼쪽 >= 오른쪽 || 아래 >= 위) return;
        int 시작 = 메시.currentVertCount;
        Color32 색 = color * (Color)(대기중 ? 진행색 : 준비색);
        메시.AddVert(new Vector3(왼쪽, 아래), 색, Vector2.zero);
        메시.AddVert(new Vector3(왼쪽, 위), 색, Vector2.zero);
        메시.AddVert(new Vector3(오른쪽, 위), 색, Vector2.zero);
        메시.AddVert(new Vector3(오른쪽, 아래), 색, Vector2.zero);
        메시.AddTriangle(시작, 시작 + 1, 시작 + 2);
        메시.AddTriangle(시작 + 2, 시작 + 3, 시작);
    }
}
