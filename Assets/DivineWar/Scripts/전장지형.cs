using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

sealed class 전장지형 : IDisposable
{
    public enum 종류 { 수풀, 연못, 바다, 숲, 습지, 바위, 흙길 }

    [Serializable]
    public struct 구역
    {
        public 종류 지형;
        public Vector2Int 왼쪽위;
        // #은 지형, .은 빈 칸. 첫 줄이 북쪽이다.
        public string[] 모양;
    }

    public const int 평지비용 = 5;
    public const int 흙길비용 = 4;
    const int 습지비용 = 8;

    // 곶은 해변을 좁게 두고, 만 안쪽에서는 흙이 육지 쪽으로 넓어진다.
    static readonly (Vector2Int 점, float 해변폭)[] 해안선 =
    {
        (new(-38, -18), 2), (new(-42, -15), 1), (new(-40, -9), 1),
        (new(-33, -2), 4), (new(-40, 7), 3), (new(-42, 14), 1),
        (new(-37, 19), 2), (new(-27, 21), 2), (new(-20, 18), 1),
        (new(-4, 19), 1), (new(2, 12), 4), (new(9, 13), 4),
        (new(16, 20), 2), (new(22, 22), 1), (new(29, 18), 1),
        (new(37, 19), 2), (new(42, 13), 1), (new(37, 8), 3),
        (new(39, 2), 1), (new(34, -2), 4), (new(36, -7), 3),
        (new(43, -11), 1), (new(41, -16), 1), (new(32, -21), 2),
        (new(25, -19), 1), (new(16, -22), 2), (new(8, -19), 2),
        (new(1, -17), 1), (new(-7, -20), 1), (new(-17, -21), 1),
        (new(-22, -15), 3), (new(-29, -18), 2)
    };

    // 양 진영에서 같은 전장을 사용하도록 배치와 변형을 칸 좌표로 결정한다.
    static readonly (Vector3Int 중심, 종류 지형, int 변형)[] 배치 =
    {
        (new(-34, -13, 0), 종류.연못, 0),
        (new(-25, 14, 0), 종류.연못, 1),
        // 해안과 연못 둑, 숲 가장자리에 이어지는 군락.
        (new(-32, 4, 0), 종류.수풀, 0),
        (new(-29, -15, 0), 종류.수풀, 2),
        (new(-21, 13, 0), 종류.수풀, 0),
        (new(-7, 4, 0), 종류.수풀, 1),
        (new(-11, -12, 0), 종류.수풀, 2)
    };

    static readonly string[][] 연못모양 =
    {
        new[] { "..####..", ".######.", "########", ".#######", "..#####.", "....##.." },
        new[] { "..###..", ".#####.", "######.", "#######", ".#####.", "..###.." }
    };
    static readonly string[][] 수풀모양 =
    {
        new[] { "...#...", "..###..", ".#####.", "####...", ".##...." },
        new[] { ".#.....", ".###...", "..####.", "...###.", ".....#." },
        new[] { ".##....", ".###...", "######.", "..####.", "....##." }
    };
    static readonly Vector3Int[] 방향 = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    readonly Tilemap 바닥;
    readonly Tilemap 지형맵;
    readonly List<UnityEngine.Object> 그래픽 = new();
    readonly Dictionary<Vector3Int, 종류> 지형칸 = new();
    readonly HashSet<Vector3Int> 해변칸 = new();

    public 전장지형(Tilemap 바닥, Tilemap 장애물, Tilemap 지형맵, TileBase[] 해변타일, TileBase[] 흙타일,
        자원지점[] 자원들, 구역[] 전술구역)
    {
        this.바닥 = 바닥;
        this.지형맵 = 지형맵;
        var 그림판 = Resources.Load<Texture2D>("battle-terrain");
        var 수풀 = new Tile[4];
        for (int i = 0; i < 수풀.Length; i++)
        {
            수풀[i] = 타일(그림(그림판, i * 32, 0, 32, 24, new(.5f, .45f), 22));
            float 옆 = (i & 1) == 0 ? -.09f : .09f;
            float 위 = (i & 2) == 0 ? -.07f : .07f;
            수풀[i].transform = Matrix4x4.Translate(new Vector3(옆, 위, 0));
        }
        var 숲바닥 = new Tile[4];
        for (int i = 0; i < 숲바닥.Length; i++)
            숲바닥[i] = 타일(그림(그림판, 128 + i * 24, 0, 24, 24, new(.5f, .5f)));
        var 바위 = new[]
        {
            타일(그림(그림판, 0, 37, 112, 83, new(.5f, .47f), 86)),
            타일(그림(그림판, 112, 37, 112, 83, new(.5f, .47f), 86))
        };
        var 물가 = new Tile[16];
        var 습지 = new Tile[16];
        for (int i = 0; i < 물가.Length; i++)
        {
            물가[i] = 타일(그림(그림판, i % 8 * 24, 120 + i / 8 * 24, 24, 24, new(.5f, .5f)));
            습지[i] = 타일(물가[i].sprite);
            습지[i].color = new Color(.91f, .98f, .62f);
        }
        var 깊은물 = 타일(물가[0].sprite);
        깊은물.color = new Color(.78f, .86f, .91f);

        var 원래구역 = 바닥.cellBounds;
        var 넓힌구역 = new BoundsInt(원래구역.min - new Vector3Int(12, 7, 0), 원래구역.size + new Vector3Int(24, 14, 0));
        var 기본바닥 = Array.Find(바닥.GetTilesBlock(원래구역), 타일 => 타일 != null);
        var 바닥좌표 = new List<Vector3Int>();
        var 바닥타일 = new List<TileBase>();
        var 얕은바다 = new HashSet<Vector3Int>();
        foreach (var 칸 in 넓힌구역.allPositionsWithin)
        {
            bool 육지 = 육지인가(칸);
            float 거리제곱 = 해안거리제곱(칸, out float 해변폭);
            if (!육지)
            {
                지형칸.Add(칸, 종류.바다);
                if (거리제곱 < 2.4f * 2.4f) 얕은바다.Add(칸);
            }
            bool 해변 = 육지 && 거리제곱 < 해변폭 * 해변폭;
            if (해변) 해변칸.Add(칸);
            // 물가 스프라이트의 투명한 부분에도 해변 흙이 이어지게 한다.
            if (해변타일 is { Length: > 0 } && (해변 || (!육지 && 거리제곱 <= 1f)))
            {
                바닥좌표.Add(칸);
                바닥타일.Add(해변타일[무늬번호(칸) % 해변타일.Length]);
                continue;
            }
            if (원래구역.Contains(칸)) continue;
            // 가장자리에 맞닿은 바닥 무늬를 모서리까지 이어 붙인다.
            var 원본칸 = new Vector3Int(
                대칭좌표(칸.x, 원래구역.xMin, 원래구역.xMax),
                대칭좌표(칸.y, 원래구역.yMin, 원래구역.yMax),
                칸.z);
            바닥좌표.Add(칸);
            바닥타일.Add(바닥.GetTile(원본칸) ?? 기본바닥);
        }
        바닥.SetTiles(바닥좌표.ToArray(), 바닥타일.ToArray());

        var 자원칸 = new HashSet<Vector3Int>();
        foreach (var 지점 in 자원들)
            자원칸.Add(바닥.WorldToCell(지점.오브젝트.position));

        foreach (var (중심, 지형, 변형) in 배치)
        {
            for (int 회전 = 0; 회전 < 2; 회전++)
            {
                var 모양 = 지형 == 종류.연못 ? 연못모양[변형] : 수풀모양[변형];
                var 후보 = new HashSet<Vector3Int>();
                for (int y = 0; y < 모양.Length; y++)
                for (int x = 0; x < 모양[y].Length; x++)
                {
                    if (모양[y][x] != '#') continue;
                    var 칸 = 중심 + new Vector3Int(x - 모양[y].Length / 2, 모양.Length / 2 - y, 0);
                    if (회전 == 1) 칸 = 반대칸(칸);
                    if (넓힌구역.Contains(칸) && !장애물.HasTile(칸) && !자원칸.Contains(칸) && !지형칸.ContainsKey(칸))
                        후보.Add(칸);
                }
                var 기준칸 = 회전 == 0 ? 중심 : 반대칸(중심);
                if (!후보.Remove(기준칸)) continue;

                // 잘려 나간 칸 때문에 같은 수풀이 두 덩어리로 나뉘지 않게 한다.
                var 연결 = new List<Vector3Int> { 기준칸 };
                for (int i = 0; i < 연결.Count; i++)
                    foreach (var 방향칸 in 방향)
                        if (후보.Remove(연결[i] + 방향칸)) 연결.Add(연결[i] + 방향칸);
                if (연결.Count < 8) continue;
                foreach (var 칸 in 연결) 지형칸.Add(칸, 지형);
            }
        }

        if (전술구역 != null)
        {
            foreach (var 구역 in 전술구역)
            for (int y = 0; y < 구역.모양.Length; y++)
            for (int x = 0; x < 구역.모양[y].Length; x++)
            {
                if (구역.모양[y][x] != '#') continue;
                var 칸 = new Vector3Int(구역.왼쪽위.x + x, 구역.왼쪽위.y - y, 원래구역.zMin);
                if (!바닥.HasTile(칸) || 장애물.HasTile(칸) || 자원칸.Contains(칸)) continue;
                지형칸.TryAdd(칸, 구역.지형);
            }
        }

        if (흙타일 is { Length: > 0 })
        {
            var 흙무늬 = new HashSet<TileBase>(흙타일);
            foreach (var 칸 in 원래구역.allPositionsWithin)
                if (!해변칸.Contains(칸) && !장애물.HasTile(칸) && 흙무늬.Contains(바닥.GetTile(칸)))
                    지형칸.TryAdd(칸, 종류.흙길);
        }

        var 지형좌표 = new List<Vector3Int>(지형칸.Count);
        var 지형타일 = new List<TileBase>(지형칸.Count);
        foreach (var (칸, 지형) in 지형칸)
        {
            if (지형 == 종류.흙길) continue;
            int 무늬 = 무늬번호(칸);
            Tile 타일;
            switch (지형)
            {
                case 종류.수풀: 타일 = 수풀[무늬]; break;
                case 종류.숲: 타일 = 숲바닥[무늬]; break;
                case 종류.바위: 타일 = 바위[무늬 & 1]; break;
                default:
                    int 가장자리 = 0;
                    for (int i = 0; i < 방향.Length; i++)
                    {
                        var 이웃 = 칸 + 방향[i];
                        if (!넓힌구역.Contains(이웃)) continue;
                        bool 이어짐 = 지형칸.TryGetValue(이웃, out var 다음지형) &&
                            (지형 == 종류.습지 ? 다음지형 == 종류.습지 : 다음지형 is 종류.연못 or 종류.바다);
                        if (!이어짐) 가장자리 |= 1 << i;
                    }
                    타일 = 지형 == 종류.습지 ? 습지[가장자리]
                        : 지형 == 종류.바다 && 가장자리 == 0 && !얕은바다.Contains(칸)
                            ? 깊은물 : 물가[가장자리];
                    break;
            }
            지형좌표.Add(칸);
            지형타일.Add(타일);
        }
        지형맵.SetTiles(지형좌표.ToArray(), 지형타일.ToArray());
    }

    static bool 육지인가(Vector3Int 칸)
    {
        // 칸 중심과 꼭짓점을 정수 단위로 비교해 경계 위의 칸을 물로 분류한다.
        int x = 칸.x * 2 + 1, y = 칸.y * 2 + 1;
        bool 안쪽 = false;
        for (int i = 0, 이전 = 해안선.Length - 1; i < 해안선.Length; 이전 = i++)
        {
            var a = 해안선[이전].점;
            var b = 해안선[i].점;
            if ((a.y * 2 > y) == (b.y * 2 > y)) continue;
            int 높이차 = b.y - a.y;
            int 외적 = (b.x - a.x) * (y - a.y * 2) - 높이차 * (x - a.x * 2);
            if (외적 == 0) return false;
            if ((외적 > 0) == (높이차 > 0)) 안쪽 = !안쪽;
        }
        return 안쪽;
    }

    static float 해안거리제곱(Vector3Int 칸, out float 해변폭)
    {
        var 중심 = new Vector2(칸.x + .5f, 칸.y + .5f);
        float 최소거리 = float.PositiveInfinity;
        해변폭 = 0;
        for (int i = 0, 이전 = 해안선.Length - 1; i < 해안선.Length; 이전 = i++)
        {
            var 시작 = 해안선[이전];
            var 끝 = 해안선[i];
            Vector2 구간 = 끝.점 - 시작.점;
            Vector2 차이 = 중심 - (Vector2)시작.점;
            float 비율 = Mathf.Clamp01(Vector2.Dot(차이, 구간) / 구간.sqrMagnitude);
            float 거리 = (차이 - 구간 * 비율).sqrMagnitude;
            if (거리 >= 최소거리) continue;
            최소거리 = 거리;
            해변폭 = Mathf.Lerp(시작.해변폭, 끝.해변폭, 비율);
        }
        return 최소거리;
    }

    static int 대칭좌표(int 좌표, int 최소, int 최대)
    {
        if (좌표 < 최소) return Mathf.Min(최소 + (최소 - 좌표 - 1), 최대 - 1);
        if (좌표 >= 최대) return Mathf.Max(최대 - 1 - (좌표 - 최대), 최소);
        return 좌표;
    }

    static Vector3Int 반대칸(Vector3Int 칸) => new(-칸.x - 1, -칸.y - 1, 칸.z);

    static int 무늬번호(Vector3Int 칸)
    {
        int 해시 = unchecked(칸.x * 73856093 ^ 칸.y * 19349663);
        return (해시 ^ (해시 >> 13)) & 3;
    }

    public bool 차단됨(Vector3Int 칸) =>
        지형칸.TryGetValue(칸, out var 지형) && 지형 is 종류.연못 or 종류.바다 or 종류.바위;

    public bool 은신지형인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 is 종류.수풀 or 종류.숲;

    public bool 건설가능(Vector2 위치) =>
        !지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) || 지형 == 종류.흙길;

    public int 이동비용(Vector3Int 칸) =>
        지형칸.TryGetValue(칸, out var 지형) ? 지형 switch
        {
            종류.흙길 => 흙길비용,
            종류.습지 => 습지비용,
            _ => 평지비용
        } : 평지비용;
    public bool 바다인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 == 종류.바다;

    public bool 대상보임(전장대상 대상, int 관측팀) =>
        대상.팀 == 관측팀 || 대상.건물 || !은신지형인가(대상.위치);

    public Color32 지도색(Vector2 위치)
    {
        var 칸 = 바닥.WorldToCell(위치);
        if (지형칸.TryGetValue(칸, out var 지형))
            return 지형 switch
            {
                종류.수풀 => new Color32(77, 117, 76, 255),
                종류.숲 => new Color32(44, 97, 58, 255),
                종류.습지 => new Color32(89, 115, 103, 255),
                종류.바위 => new Color32(126, 130, 111, 255),
                종류.흙길 => new Color32(130, 108, 77, 255),
                종류.바다 => new Color32(41, 79, 101, 255),
                _ => new Color32(58, 108, 133, 255)
            };
        return 해변칸.Contains(칸) ? new Color32(111, 99, 76, 255) : new Color32(52, 78, 68, 255);
    }

    Sprite 그림(Texture2D 그림판, int x, int y, int 너비, int 높이, Vector2 중심, float 픽셀밀도 = 24)
    {
        var 그림 = Sprite.Create(그림판, new Rect(x, 그림판.height - y - 높이, 너비, 높이), 중심,
            픽셀밀도, 0, SpriteMeshType.FullRect);
        그래픽.Add(그림);
        return 그림;
    }

    Tile 타일(Sprite 그림)
    {
        var 타일 = ScriptableObject.CreateInstance<Tile>();
        타일.sprite = 그림;
        타일.colliderType = Tile.ColliderType.None;
        그래픽.Add(타일);
        return 타일;
    }

    public void Dispose()
    {
        if (지형맵 != null) 지형맵.ClearAllTiles();
        foreach (var 오브젝트 in 그래픽) UnityEngine.Object.Destroy(오브젝트);
    }
}
