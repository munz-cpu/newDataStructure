using System;
using System.Collections.Generic;
using UnityEngine;

sealed class 전장그림자 : IDisposable
{
    const int 크기 = 64;
    const int 그림자순서 = -10000;
    static readonly Vector2 방향 = new Vector2(1, -.42f).normalized;
    static readonly Quaternion 회전 = Quaternion.Euler(0, 0, Mathf.Atan2(방향.y, 방향.x) * Mathf.Rad2Deg);
    static readonly Color32 그림자색 = new(23, 25, 24, 133);

    readonly GameObject 뿌리;
    readonly Texture2D 질감;
    readonly Sprite 모양;
    readonly Material 재질;
    readonly (SpriteRenderer 원본, SpriteRenderer 그림자)[] 유닛;
    readonly List<(SpriteRenderer 원본, SpriteRenderer 그림자, Vector2 크기)> 지형 = new();

    public 전장그림자(Transform 부모, SpriteRenderer[] 슬롯, SpriteRenderer[] 자원, Transform 장식지형)
    {
        뿌리 = new GameObject("전장 그림자");
        뿌리.transform.SetParent(부모, false);
        재질 = 슬롯[0].sharedMaterial;
        질감 = new Texture2D(크기, 크기, TextureFormat.RGBA32, false)
        {
            name = "전장 그림자 모양",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var 픽셀 = new Color32[크기 * 크기];
        for (int y = 0; y < 크기; y++)
        for (int x = 0; x < 크기; x++)
        {
            var 점 = new Vector2((x + .5f) * 2 / 크기 - 1, (y + .5f) * 2 / 크기 - 1);
            byte 농도 = (byte)Mathf.RoundToInt(255 * Mathf.Clamp01((1 - 점.magnitude) / .24f));
            픽셀[y * 크기 + x] = new Color32(255, 255, 255, 농도);
        }
        질감.SetPixels32(픽셀);
        질감.Apply(false, true);
        모양 = Sprite.Create(질감, new Rect(0, 0, 크기, 크기), new Vector2(.5f, .5f), 크기,
            0, SpriteMeshType.FullRect);

        유닛 = new (SpriteRenderer, SpriteRenderer)[슬롯.Length];
        for (int i = 0; i < 슬롯.Length; i++) 유닛[i] = (슬롯[i], 그림자생성());

        var 지형원본 = new HashSet<SpriteRenderer>(자원);
        if (장식지형 != null)
            지형원본.UnionWith(장식지형.GetComponentsInChildren<SpriteRenderer>(true));

        foreach (var 원본 in 지형원본)
        {
            if (원본 == null || 원본.sprite == null) continue;
            var 너비 = 원본.bounds.size.x;
            지형.Add((원본, 그림자생성(), new Vector2(너비 * .71f, 너비 * .34f)));
        }
    }

    public bool 보이기 { set => 뿌리.SetActive(value); }

    SpriteRenderer 그림자생성()
    {
        var 오브젝트 = new GameObject("바닥 그림자");
        오브젝트.transform.SetParent(뿌리.transform, false);
        오브젝트.transform.rotation = 회전;
        var 그림 = 오브젝트.AddComponent<SpriteRenderer>();
        그림.sprite = 모양;
        그림.sharedMaterial = 재질;
        그림.color = 그림자색;
        그림.sortingOrder = 그림자순서;
        그림.enabled = false;
        return 그림;
    }

    public void 반영(전장대상[] 대상들, 전장안개 안개)
    {
        for (int i = 0; i < 유닛.Length; i++)
        {
            var (원본, 그림자) = 유닛[i];
            var 대상 = 대상들[i];
            그림자.enabled = 대상 is { 생존: true } && 본체표시됨(원본);
            if (!그림자.enabled) continue;
            var 크기 = 대상.종류 switch
            {
                대상종류.회관 => new Vector2(2.34f, 1.1f),
                대상종류.제재소 or 대상종류.훈련소 => new Vector2(2.08f, .94f),
                대상종류.나무울타리 or 대상종류.돌울타리 => new Vector2(.72f, .38f),
                대상종류.왕 => new Vector2(1.28f, .55f),
                _ => new Vector2(1.10f, .52f)
            };
            위치반영(그림자, 원본.transform.position, 크기);
        }

        foreach (var (원본, 그림자, 크기) in 지형)
        {
            그림자.enabled = 본체표시됨(원본) && 안개.탐사됨(원본.transform.position);
            if (그림자.enabled) 위치반영(그림자, 원본.transform.position, 크기);
        }
    }

    static bool 본체표시됨(SpriteRenderer 원본) =>
        원본 != null && 원본.enabled && 원본.gameObject.activeInHierarchy && !원본.forceRenderingOff;

    static void 위치반영(SpriteRenderer 그림자, Vector3 원점, Vector2 크기)
    {
        그림자.transform.position = 원점 + (Vector3)(방향 * (크기.x * .22f));
        그림자.transform.localScale = new Vector3(크기.x, 크기.y, 1);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(뿌리);
        UnityEngine.Object.Destroy(모양);
        UnityEngine.Object.Destroy(질감);
    }
}
