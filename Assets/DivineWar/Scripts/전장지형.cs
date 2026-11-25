using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

sealed class 전장지형 : IDisposable
{
    public enum 종류 { 수풀, 연못, 바다, 숲, 습지, 바위, 흙길, 강, 여울 }

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
        (new(-59, -33), 2), (new(-65, -27), 1), (new(-61, -16), 1),
        (new(-51, -4), 4), (new(-61, 12), 3), (new(-65, 26), 1),
        (new(-57, 35), 2), (new(-42, 39), 2), (new(-31, 33), 1),
        (new(-6, 35), 1), (new(4, 22), 4), (new(14, 24), 4),
        (new(25, 37), 2), (new(34, 41), 1), (new(44, 33), 1),
        (new(57, 36), 2), (new(65, 25), 1), (new(57, 15), 3),
        (new(60, 4), 1), (new(53, -4), 4), (new(55, -12), 3),
        (new(66, -21), 1), (new(63, -30), 1), (new(49, -38), 2),
        (new(38, -35), 1), (new(25, -41), 2), (new(12, -35), 2),
        (new(1, -31), 1), (new(-11, -37), 1), (new(-26, -38), 1),
        (new(-34, -27), 3), (new(-44, -33), 2)
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
    readonly List<(Vector3Int 칸, Rect 범위, float 알파)> 수풀그림들 = new();
    bool 수풀페이드중;
    readonly HashSet<Vector3Int> 해변칸 = new();
    readonly HashSet<Vector3Int> 광맥칸 = new();
    internal 자원지점[] 자원들 { get; }

    public 전장지형(Tilemap 바닥, Tilemap 장애물, Tilemap 지형맵, TileBase[] 해변타일, TileBase[] 흙타일,
        자원지점[] 자원들, 구역[] 전술구역, bool 그래픽표시 = true, SpriteRenderer 숲나무프리팹 = null)
        : this(바닥, 장애물, 지형맵, 해변타일, 흙타일, 자원들, 전술구역, 그래픽표시, 숲나무프리팹, 0) { }

    public 전장지형(Tilemap 바닥, Tilemap 장애물, Tilemap 지형맵, TileBase[] 해변타일, TileBase[] 흙타일,
        자원지점[] 자원들, 구역[] 전술구역, bool 그래픽표시, SpriteRenderer 숲나무프리팹, int 시드)
    {
        this.바닥 = 바닥;
        this.지형맵 = 지형맵;
        var 난수 = 시드 == 0 ? null : new System.Random(시드);
        var 해안 = 난수 == null ? 해안선 : Array.ConvertAll(해안선, 꼭짓점 =>
            (꼭짓점.점 + new Vector2Int(난수.Next(-2, 3), 난수.Next(-2, 3)), 꼭짓점.해변폭));
        var 원래구역 = 바닥.cellBounds;
        var 여백 = new Vector3Int(36, 26, 0);
        var 넓힌구역 = new BoundsInt(원래구역.min - 여백, 원래구역.size + 여백 * 2);
        var 기본바닥 = Array.Find(바닥.GetTilesBlock(원래구역), 타일 => 타일 != null);
        var 바닥좌표 = new List<Vector3Int>();
        var 바닥타일 = new List<TileBase>();
        var 얕은바다 = new HashSet<Vector3Int>();
        foreach (var 칸 in 넓힌구역.allPositionsWithin)
        {
            bool 육지 = 육지인가(칸, 해안);
            float 거리제곱 = 해안거리제곱(칸, 해안, out float 해변폭);
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
        {
            var 칸 = 바닥.WorldToCell(지점.위치);
            자원칸.Add(칸);
            if (지점.재화번호 is 2 or 3) 광맥칸.Add(칸);
        }

        if (난수 != null) 강배치(넓힌구역, 장애물, 자원칸, 난수);
        foreach (var (기준위치, 지형, 기본변형) in 배치)
        {
            var 중심 = 난수 == null ? 기준위치 : 기준위치 + new Vector3Int(난수.Next(-4, 5), 난수.Next(-4, 5), 0);
            var 모양들 = 지형 == 종류.연못 ? 연못모양 : 수풀모양;
            var 모양 = 모양들[난수?.Next(모양들.Length) ?? 기본변형];
            for (int 회전 = 0; 회전 < 2; 회전++)
            {
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

        var 숲칸들 = new List<(Vector3Int 칸, float 앞정도)>();
        if (전술구역 != null)
        {
            foreach (var 구역 in 전술구역)
            for (int y = 0; y < 구역.모양.Length; y++)
            for (int x = 0; x < 구역.모양[y].Length; x++)
            {
                if (구역.모양[y][x] != '#') continue;
                var 칸 = new Vector3Int(구역.왼쪽위.x + x, 구역.왼쪽위.y - y, 원래구역.zMin);
                if (!바닥.HasTile(칸) || 장애물.HasTile(칸) || 자원칸.Contains(칸) || 해변칸.Contains(칸)) continue;
                if (지형칸.TryAdd(칸, 구역.지형) && 구역.지형 == 종류.숲)
                    숲칸들.Add((칸, y / (float)Mathf.Max(1, 구역.모양.Length - 1)));
            }
        }

        if (흙타일 is { Length: > 0 })
        {
            var 흙무늬 = new HashSet<TileBase>(흙타일);
            foreach (var 칸 in 원래구역.allPositionsWithin)
                if (!해변칸.Contains(칸) && !장애물.HasTile(칸) && 흙무늬.Contains(바닥.GetTile(칸)))
                    지형칸.TryAdd(칸, 종류.흙길);
        }

        // 씬에 배치된 자원 번호를 유지하고, 숲 나무는 구역의 칸 순서대로 이어 붙인다.
        this.자원들 = new 자원지점[자원들.Length + 숲칸들.Count];
        Array.Copy(자원들, this.자원들, 자원들.Length);
        for (int i = 0; i < 숲칸들.Count; i++)
        {
            var (칸, 앞정도) = 숲칸들[i];
            int 무늬 = 무늬번호(칸);
            var 위치 = 지형맵.GetCellCenterWorld(칸)
                + new Vector3((무늬 & 1) == 0 ? -.12f : .12f, (무늬 & 2) == 0 ? -.28f : -.16f, 0);
            this.자원들[자원들.Length + i] = new 자원지점
            {
                재화번호 = 0,
                숲나무 = true,
                숲위치 = 위치,
                오브젝트 = 그래픽표시 && 숲나무프리팹 != null ? 숲나무배치(숲나무프리팹, 위치, 앞정도) : null
            };
        }
        if (!그래픽표시) return;
        var 그림판 = Resources.Load<Texture2D>("battle-terrain");
        var 수풀 = new Tile[4];
        for (int i = 0; i < 수풀.Length; i++)
        {
            수풀[i] = 타일(그림(그림판, i * 32, 0, 32, 24, new(.5f, .45f), 22));
            수풀[i].flags &= ~TileFlags.LockColor;
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
        var 여울 = new Tile[16];
        for (int i = 0; i < 물가.Length; i++)
        {
            물가[i] = 타일(그림(그림판, i % 8 * 24, 120 + i / 8 * 24, 24, 24, new(.5f, .5f)));
            습지[i] = 타일(물가[i].sprite);
            습지[i].color = new Color(.91f, .98f, .62f);
            여울[i] = 타일(물가[i].sprite);
            여울[i].color = new Color(.78f, .98f, .87f);
        }
        var 깊은물 = 타일(물가[0].sprite);
        깊은물.color = new Color(.78f, .86f, .91f);

        var 지형좌표 = new List<Vector3Int>(지형칸.Count);
        var 지형타일 = new List<TileBase>(지형칸.Count);
        바닥좌표.Clear();
        바닥타일.Clear();
        foreach (var (칸, 지형) in 지형칸)
        {
            if (지형 == 종류.흙길) continue;
            int 무늬 = 무늬번호(칸);
            Tile 타일;
            switch (지형)
            {
                case 종류.수풀:
                    타일 = 수풀[무늬];
                    Vector2 시작 = 지형맵.CellToWorld(칸);
                    Vector2 끝 = 지형맵.CellToWorld(칸 + new Vector3Int(1, 1, 0));
                    수풀그림들.Add((칸, new Rect(시작, 끝 - 시작), 1));
                    break;
                case 종류.숲:
                    타일 = 숲바닥[무늬];
                    if (숲나무프리팹 != null)
                    {
                        바닥좌표.Add(칸);
                        바닥타일.Add(타일);
                        continue;
                    }
                    break;
                case 종류.바위: 타일 = 바위[무늬 & 1]; break;
                default:
                    int 가장자리 = 0;
                    for (int i = 0; i < 방향.Length; i++)
                    {
                        var 이웃 = 칸 + 방향[i];
                        if (!넓힌구역.Contains(이웃)) continue;
                        bool 이어짐 = 지형칸.TryGetValue(이웃, out var 다음지형) &&
                            (지형 == 종류.습지 ? 다음지형 == 종류.습지 : 다음지형 is 종류.연못 or 종류.바다 or 종류.강 or 종류.여울);
                        if (!이어짐) 가장자리 |= 1 << i;
                    }
                    타일 = 지형 == 종류.습지 ? 습지[가장자리]
                        : 지형 == 종류.여울 ? 여울[가장자리]
                        : 가장자리 == 0 && (지형 == 종류.강 || 지형 == 종류.바다 && !얕은바다.Contains(칸))
                            ? 깊은물 : 물가[가장자리];
                    break;
            }
            지형좌표.Add(칸);
            지형타일.Add(타일);
        }
        바닥.SetTiles(바닥좌표.ToArray(), 바닥타일.ToArray());
        지형맵.SetTiles(지형좌표.ToArray(), 지형타일.ToArray());
    }

    void 강배치(BoundsInt 범위, Tilemap 장애물, HashSet<Vector3Int> 자원칸, System.Random 난수)
    {
        const int 굽이간격 = 18;
        int 위치 = 난수.Next(-7, 8);
        var 굽이들 = new int[범위.size.y / 굽이간격 + 2];
        for (int i = 0; i < 굽이들.Length; i++) 굽이들[i] = 난수.Next(-7, 8);
        int 중심(int y)
        {
            int 줄 = y - 범위.yMin;
            int 구간 = 줄 / 굽이간격, 나머지 = 줄 % 굽이간격;
            return 위치 + (굽이들[구간] * (굽이간격 - 나머지) + 굽이들[구간 + 1] * 나머지) / 굽이간격;
        }

        // 여울은 양쪽 둑까지 막힘 없는 띠를 골라 둔다.
        var 여울들 = new List<int>(3);
        foreach (int 기준 in new[] { -17, 0, 17 })
        {
            var 후보들 = new List<int>();
            for (int y = Mathf.Max(범위.yMin + 1, 기준 - 6); y <= Mathf.Min(범위.yMax - 2, 기준 + 6); y++)
            {
                bool 통행가능 = true;
                for (int 줄 = y - 1; 줄 <= y + 1 && 통행가능; 줄++)
                for (int x = 중심(줄) - 4; x <= 중심(줄) + 4; x++)
                {
                    var 칸 = new Vector3Int(x, 줄, 범위.zMin);
                    if (!지형칸.ContainsKey(칸) && !장애물.HasTile(칸) && !자원칸.Contains(칸)) continue;
                    통행가능 = false;
                    break;
                }
                if (통행가능) 후보들.Add(y);
            }
            if (후보들.Count > 0) 여울들.Add(후보들[난수.Next(후보들.Count)]);
        }

        for (int y = 범위.yMin; y < 범위.yMax; y++)
        {
            int x중심 = 중심(y);
            bool 여울 = 여울들.Exists(줄 => Mathf.Abs(줄 - y) <= 1);
            int 폭 = 여울 ? 1 : 2;
            for (int x = x중심 - 폭; x <= x중심 + 폭; x++)
            {
                var 칸 = new Vector3Int(x, y, 범위.zMin);
                if (!장애물.HasTile(칸) && !자원칸.Contains(칸))
                    지형칸.TryAdd(칸, 여울 ? 종류.여울 : 종류.강);
            }
        }
    }

    internal static int 깊이순서(float y) => (int)(-y * 80) + 1600;

    Transform 숲나무배치(SpriteRenderer 프리팹, Vector3 위치, float 앞정도)
    {
        var 나무 = UnityEngine.Object.Instantiate(프리팹, 위치, Quaternion.identity, 지형맵.transform);
        나무.transform.localScale *= Mathf.Lerp(.78f, 1.04f, 앞정도);
        나무.sortingOrder = 깊이순서(위치.y) - 2;
        그래픽.Add(나무.gameObject);
        return 나무.transform;
    }

    static bool 육지인가(Vector3Int 칸, (Vector2Int 점, float 해변폭)[] 해안선)
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

    static float 해안거리제곱(Vector3Int 칸, (Vector2Int 점, float 해변폭)[] 해안선, out float 해변폭)
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

    static int 무늬번호(Vector3Int 칸) => (int)(칸해시(칸) & 3);

    static uint 칸해시(Vector3Int 칸)
    {
        int 해시 = unchecked(칸.x * 73856093 ^ 칸.y * 19349663);
        return unchecked((uint)(해시 ^ (해시 >> 13)));
    }

    bool 숲인가(Vector3Int 칸) => 지형칸.TryGetValue(칸, out var 지형) && 지형 == 종류.숲;

    public bool 도약이숲을가로지름(Vector2 출발, Vector2 착지)
    {
        var 시작 = 바닥.LocalToCellInterpolated(바닥.WorldToLocal(출발));
        var 끝 = 바닥.LocalToCellInterpolated(바닥.WorldToLocal(착지));
        var 이동 = 끝 - 시작;
        var 현재칸 = 바닥.WorldToCell(출발);
        var 끝칸 = 바닥.WorldToCell(착지);
        int 가로방향 = 이동.x < 0 ? -1 : 1;
        int 세로방향 = 이동.y < 0 ? -1 : 1;

        // 도약 선분이 지나는 칸을 경계에 닿는 순서대로 검사한다.
        while (현재칸 != 끝칸)
        {
            if (숲인가(현재칸)) return true;
            float 가로경계 = 현재칸.x == 끝칸.x ? float.PositiveInfinity
                : (현재칸.x + (가로방향 > 0 ? 1 : 0) - 시작.x) / 이동.x;
            float 세로경계 = 현재칸.y == 끝칸.y ? float.PositiveInfinity
                : (현재칸.y + (세로방향 > 0 ? 1 : 0) - 시작.y) / 이동.y;
            if (가로경계 < 세로경계) 현재칸.x += 가로방향;
            else if (세로경계 < 가로경계) 현재칸.y += 세로방향;
            else
            {
                if (숲인가(현재칸 + new Vector3Int(가로방향, 0, 0)) ||
                    숲인가(현재칸 + new Vector3Int(0, 세로방향, 0))) return true;
                현재칸 += new Vector3Int(가로방향, 세로방향, 0);
            }
        }
        return 숲인가(끝칸);
    }

    public bool 차단됨(Vector3Int 칸) => 광맥칸.Contains(칸) ||
        지형칸.TryGetValue(칸, out var 지형) && 지형 is 종류.연못 or 종류.바다 or 종류.숲 or 종류.바위 or 종류.강;

    public bool 은신지형인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 is 종류.수풀 or 종류.숲;

    internal bool 수풀인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 == 종류.수풀;

    internal const float 열린수풀알파 = .42f;

    internal void 수풀투명도갱신(Vector2? 중심, float 초)
    {
        if (!중심.HasValue && !수풀페이드중) return;
        수풀페이드중 = false;
        for (int i = 0; i < 수풀그림들.Count; i++)
        {
            var (칸, 범위, 알파) = 수풀그림들[i];
            var 위치 = 중심.GetValueOrDefault();
            var 가까운점 = new Vector2(Mathf.Clamp(위치.x, 범위.xMin, 범위.xMax),
                Mathf.Clamp(위치.y, 범위.yMin, 범위.yMax));
            bool 열림 = 중심.HasValue && 전장규칙.수풀시야안(위치, 가까운점);
            float 다음알파 = Mathf.MoveTowards(알파, 열림 ? 열린수풀알파 : 1, 초 * 5);
            수풀페이드중 |= 다음알파 < 1;
            if (다음알파 == 알파) continue;
            지형맵.SetColor(칸, new Color(1, 1, 1, 다음알파));
            수풀그림들[i] = (칸, 범위, 다음알파);
        }
    }

    public bool 건설가능(Vector2 위치) =>
        !지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) || 지형 == 종류.흙길;

    public int 이동비용(Vector3Int 칸) =>
        지형칸.TryGetValue(칸, out var 지형) ? 지형 switch
        {
            종류.흙길 => 흙길비용,
            종류.습지 or 종류.여울 => 습지비용,
            _ => 평지비용
        } : 평지비용;
    public bool 습지인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 is 종류.습지 or 종류.여울;

    public bool 바다인가(Vector2 위치) =>
        지형칸.TryGetValue(바닥.WorldToCell(위치), out var 지형) && 지형 == 종류.바다;

    internal bool 숲인가(Vector2 위치) => 숲인가(바닥.WorldToCell(위치));

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
                종류.강 => new Color32(52, 100, 125, 255),
                종류.여울 => new Color32(91, 132, 137, 255),
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
        foreach (var 오브젝트 in 그래픽)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(오브젝트);
            else UnityEngine.Object.DestroyImmediate(오브젝트);
        }
    }
}
