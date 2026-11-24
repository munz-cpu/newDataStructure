using UnityEngine;
using UnityEngine.UI;

public sealed class 스킬윤곽선 : MaskableGraphic
{
    static readonly Color32 장착색 = new(255, 181, 78, 255);
    static readonly Color32 배지바탕 = new(43, 27, 17, 255);
    bool 장착, 선택, 호버;

    public void 상태표시(bool 장착중, bool 선택중, bool 호버중)
    {
        if (장착 == 장착중 && 선택 == 선택중 && 호버 == 호버중) return;
        장착 = 장착중;
        선택 = 선택중;
        호버 = 호버중;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper 메시)
    {
        메시.Clear();
        var 칸 = GetPixelAdjustedRect();
        if (호버) 테두리(메시, 칸, 3, 3, Color.white);
        if (!장착) return;
        테두리(메시, 칸, 0, 선택 ? 3 : 2, 장착색);
        사각형(메시, 칸.xMax - 8, 칸.yMax - 8, 11, 11, 배지바탕);
        사각형(메시, 칸.xMax - 6, 칸.yMax - 6, 7, 7, 장착색);
    }

    static void 테두리(VertexHelper 메시, Rect 칸, float 확장, float 두께, Color32 색)
    {
        float x = 칸.xMin - 확장, y = 칸.yMin - 확장;
        float 너비 = 칸.width + 확장 * 2, 높이 = 칸.height + 확장 * 2;
        사각형(메시, x, y, 너비, 두께, 색);
        사각형(메시, x, y + 높이 - 두께, 너비, 두께, 색);
        사각형(메시, x, y + 두께, 두께, 높이 - 두께 * 2, 색);
        사각형(메시, x + 너비 - 두께, y + 두께, 두께, 높이 - 두께 * 2, 색);
    }

    internal static void 사각형(VertexHelper 메시, float x, float y, float 너비, float 높이, Color32 색)
    {
        int 시작 = 메시.currentVertCount;
        메시.AddVert(new Vector3(x, y), 색, Vector2.zero);
        메시.AddVert(new Vector3(x, y + 높이), 색, Vector2.zero);
        메시.AddVert(new Vector3(x + 너비, y + 높이), 색, Vector2.zero);
        메시.AddVert(new Vector3(x + 너비, y), 색, Vector2.zero);
        메시.AddTriangle(시작, 시작 + 1, 시작 + 2);
        메시.AddTriangle(시작 + 2, 시작 + 3, 시작);
    }
}
