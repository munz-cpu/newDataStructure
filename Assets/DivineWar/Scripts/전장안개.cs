using System;
using UnityEngine;

sealed class 전장안개 : IDisposable
{
    public static readonly Rect 전장범위 = new(-24, -14, 48, 28);

    const int 칸밀도 = 2;
    const float 시야반경 = 6;
    const float 갱신간격 = .1f;
    static readonly Color32 미탐사 = new(15, 20, 30, 255);

    readonly int 너비 = (int)전장범위.width * 칸밀도;
    readonly int 높이 = (int)전장범위.height * 칸밀도;
    readonly Color32[] 픽셀;
    readonly Material 재질;
    readonly Sprite 구름;
    readonly SpriteRenderer 안개그림;
    float 다음갱신;

    public Texture2D 탐사지도 { get; }

    public 전장안개(Transform 부모)
    {
        픽셀 = new Color32[너비 * 높이];
        Array.Fill(픽셀, 미탐사);
        탐사지도 = new Texture2D(너비, 높이, TextureFormat.RGBA32, false)
        {
            name = "전장 탐사 지도",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        탐사지도.SetPixels32(픽셀);
        탐사지도.Apply();

        var 무늬 = Resources.Load<Texture2D>("battle-fog");
        var 셰이더 = Resources.Load<Shader>("전장 안개");
        재질 = new Material(셰이더);
        재질.SetTexture("_Exploration", 탐사지도);
        구름 = Sprite.Create(무늬, new Rect(0, 0, 무늬.width, 무늬.height),
            new Vector2(.5f, .5f), 무늬.width, 0, SpriteMeshType.FullRect);

        var 오브젝트 = new GameObject("전장 안개");
        오브젝트.transform.SetParent(부모, false);
        오브젝트.transform.position = 전장범위.center;
        오브젝트.transform.localScale = new Vector3(전장범위.width, 전장범위.height, 1);
        안개그림 = 오브젝트.AddComponent<SpriteRenderer>();
        안개그림.sharedMaterial = 재질;
        안개그림.sprite = 구름;
        안개그림.sortingOrder = short.MaxValue;
    }

    public bool 보이기 { set => 안개그림.enabled = value; }

    public bool 탐사됨(Vector2 위치)
    {
        int x = Mathf.FloorToInt((위치.x - 전장범위.xMin) * 칸밀도);
        int y = Mathf.FloorToInt((위치.y - 전장범위.yMin) * 칸밀도);
        return x >= 0 && x < 너비 && y >= 0 && y < 높이 && 픽셀[y * 너비 + x].a == 0;
    }

    public bool 갱신(전장대상[] 대상들, int 내팀, float 현재시간)
    {
        if (현재시간 < 다음갱신) return false;
        다음갱신 = 현재시간 + 갱신간격;

        bool 변경 = false;
        foreach (var 대상 in 대상들)
        {
            if (대상 == null || 대상.팀 != 내팀 || !대상.생존) continue;
            var 중심 = (대상.위치 - 전장범위.min) * 칸밀도;
            const float 반경 = 시야반경 * 칸밀도;
            int 왼쪽 = Mathf.Max(0, Mathf.FloorToInt(중심.x - 반경));
            int 오른쪽 = Mathf.Min(너비 - 1, Mathf.FloorToInt(중심.x + 반경));
            int 아래 = Mathf.Max(0, Mathf.FloorToInt(중심.y - 반경));
            int 위 = Mathf.Min(높이 - 1, Mathf.FloorToInt(중심.y + 반경));
            for (int y = 아래; y <= 위; y++)
            for (int x = 왼쪽; x <= 오른쪽; x++)
            {
                int 번호 = y * 너비 + x;
                var 차이 = new Vector2(x + .5f, y + .5f) - 중심;
                if (픽셀[번호].a == 0 || 차이.sqrMagnitude > 반경 * 반경) continue;
                픽셀[번호] = default;
                변경 = true;
            }
        }

        if (!변경) return false;
        탐사지도.SetPixels32(픽셀);
        탐사지도.Apply();
        return true;
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(안개그림.gameObject);
        UnityEngine.Object.Destroy(구름);
        UnityEngine.Object.Destroy(재질);
        UnityEngine.Object.Destroy(탐사지도);
    }
}
