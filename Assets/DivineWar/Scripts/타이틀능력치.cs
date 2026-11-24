using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class 타이틀능력치 : MaskableGraphic
{
    [SerializeField] TMP_Text 체력표시, 힘표시, 민첩표시;
    static readonly Color32 바탕색 = new(39, 27, 20, 255);
    static readonly Color32 테두리색 = new(90, 73, 47, 255);
    static readonly Color32 체력색 = new(207, 117, 95, 255);
    static readonly Color32 힘색 = new(228, 161, 85, 255);
    static readonly Color32 민첩색 = new(159, 178, 112, 255);
    const float 차오르는시간 = .36f;
    Vector3 비율, 목표비율;
    float 경과 = 차오르는시간;

    public void 표시(int 체력, int 힘, int 민첩, bool 차오르기 = false)
    {
        체력표시.text = $"체력  {체력}";
        힘표시.text = $"힘  {힘}";
        민첩표시.text = $"민첩  {민첩}";
        목표비율 = new Vector3(체력 / 300f, 힘 / 10f, 민첩 / 10f);
        비율 = 차오르기 ? Vector3.zero : 목표비율;
        경과 = 차오르기 ? 0 : 차오르는시간;
        SetVerticesDirty();
    }

    void Update()
    {
        if (경과 >= 차오르는시간) return;
        경과 = Mathf.Min(경과 + Time.unscaledDeltaTime, 차오르는시간);
        비율 = 목표비율 * Mathf.SmoothStep(0, 1, 경과 / 차오르는시간);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper 메시)
    {
        메시.Clear();
        var 칸 = GetPixelAdjustedRect();
        float 행높이 = 칸.height / 3f;
        float x = 칸.xMin + 112, 너비 = Mathf.Max(0, 칸.width - 136);
        막대(메시, x, 칸.yMax - 행높이 * .5f, 너비, 비율.x, 체력색);
        막대(메시, x, 칸.yMax - 행높이 * 1.5f, 너비, 비율.y, 힘색);
        막대(메시, x, 칸.yMax - 행높이 * 2.5f, 너비, 비율.z, 민첩색);
    }

    static void 막대(VertexHelper 메시, float x, float y, float 너비, float 비율, Color32 색)
    {
        스킬윤곽선.사각형(메시, x, y - 5, 너비, 10, 테두리색);
        스킬윤곽선.사각형(메시, x + 1, y - 4, Mathf.Max(0, 너비 - 2), 8, 바탕색);
        스킬윤곽선.사각형(메시, x + 1, y - 4, Mathf.Max(0, 너비 - 2) * Mathf.Clamp01(비율), 8, 색);
    }
}
