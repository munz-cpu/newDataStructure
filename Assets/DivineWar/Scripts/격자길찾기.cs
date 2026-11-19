using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class 격자길찾기
{
    public sealed class 이동경로
    {
        public int 목적칸 = -1, 지도버전 = -1, 다음칸;
        public readonly List<Vector2> 지점들 = new();
    }

    static readonly Vector2Int[] 방향들 =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    readonly Tilemap 바닥;
    readonly BoundsInt 구역;
    readonly bool[] 통행, 건물차단, 닫힘;
    readonly int[] 거리, 이전;
    readonly int[] 건물칸;
    readonly Vector2[] 중심;
    readonly List<(int 칸, int 점수)> 후보 = new();
    int 지도버전;

    public 격자길찾기(Tilemap 바닥, Tilemap 장애물)
    {
        this.바닥 = 바닥;
        구역 = 바닥.cellBounds;
        int 칸수 = 구역.size.x * 구역.size.y;
        통행 = new bool[칸수];
        건물차단 = new bool[칸수];
        닫힘 = new bool[칸수];
        거리 = new int[칸수];
        이전 = new int[칸수];
        중심 = new Vector2[칸수];
        건물칸 = new int[전장규칙.전체칸];
        Array.Fill(건물칸, -1);
        for (int i = 0; i < 칸수; i++)
        {
            var 칸 = 칸좌표(i);
            통행[i] = 바닥.HasTile(칸) && !장애물.HasTile(칸);
            중심[i] = 바닥.GetCellCenterWorld(칸);
        }
    }

    public Vector2 중심정렬(Vector2 위치)
    {
        int 번호 = 위치칸(위치);
        return 번호 < 0 ? 위치 : 중심[번호];
    }

    int 위치칸(Vector2 위치) => 칸번호(바닥.WorldToCell(위치));
    int 칸번호(Vector3Int 칸)
    {
        int x = 칸.x - 구역.xMin, y = 칸.y - 구역.yMin;
        return x < 0 || x >= 구역.size.x || y < 0 || y >= 구역.size.y ? -1 : y * 구역.size.x + x;
    }

    Vector3Int 칸좌표(int 번호) => new(번호 % 구역.size.x + 구역.xMin, 번호 / 구역.size.x + 구역.yMin, 구역.zMin);
    bool 통행가능(int 번호) => 번호 >= 0 && 통행[번호] && !건물차단[번호];
    public bool 정적통행가능(Vector2 위치)
    {
        int 번호 = 위치칸(위치);
        return 번호 >= 0 && 통행[번호];
    }

    public void 건물반영(int 번호, Vector2 위치, bool 있음)
    {
        int 전 = 건물칸[번호], 후 = 있음 ? 위치칸(위치) : -1;
        if (전 == 후) return;
        if (전 >= 0) 건물차단[전] = false;
        if (후 >= 0) 건물차단[후] = true;
        건물칸[번호] = 후;
        지도버전++;
    }

    public bool 이동(이동경로 경로, Vector2 지금, Vector2 목적, float 길이, out Vector2 다음)
    {
        int 목표칸 = 위치칸(목적);
        if (경로.목적칸 != 목표칸 || 경로.지도버전 != 지도버전)
        {
            경로.목적칸 = 목표칸;
            경로.지도버전 = 지도버전;
            경로.다음칸 = 0;
            구하기(지금, 목적, 경로.지점들);
        }
        다음 = 지금;
        while (경로.다음칸 < 경로.지점들.Count)
        {
            Vector2 지점 = 경로.지점들[경로.다음칸];
            float 차이 = Vector2.Distance(다음, 지점);
            if (차이 > 길이)
            {
                다음 = Vector2.MoveTowards(다음, 지점, 길이);
                return false;
            }
            다음 = 지점;
            길이 -= 차이;
            경로.다음칸++;
        }
        if (통행가능(목표칸) && 위치칸(다음) == 목표칸)
        {
            다음 = Vector2.MoveTowards(다음, 목적, 길이);
            return (다음 - 목적).sqrMagnitude < .0001f;
        }
        return true;
    }

    public void 초기화(이동경로 경로) => 경로.지도버전 = -1;

    public void 구하기(Vector2 시작위치, Vector2 목표위치, List<Vector2> 결과)
    {
        결과.Clear();
        int 시작 = 위치칸(시작위치), 목표 = 위치칸(목표위치);
        if (시작 < 0 || 목표 < 0 || 시작 == 목표) return;
        Array.Fill(거리, int.MaxValue);
        Array.Clear(닫힘, 0, 닫힘.Length);
        후보.Clear();
        거리[시작] = 0;
        이전[시작] = -1;
        Vector3Int 목표좌표 = 칸좌표(목표);
        int 최선 = 시작;
        float 목표거리 = (중심[시작] - 목표위치).sqrMagnitude;
        bool 차단목표 = !통행가능(목표);
        float 인접거리 = float.PositiveInfinity;
        if (차단목표)
        {
            foreach (var 방향 in 방향들)
            {
                int 후보칸 = 칸번호(목표좌표 + new Vector3Int(방향.x, 방향.y, 0));
                if (통행가능(후보칸))
                    인접거리 = Mathf.Min(인접거리, (중심[후보칸] - 목표위치).sqrMagnitude);
            }
            // 이웃 열에서 더 가까운 칸이 가능한 경우에는 탐색을 계속한다.
            if (인접거리 >= 2.25f) 인접거리 = float.NegativeInfinity;
        }
        후보추가(시작, 추정거리(칸좌표(시작), 목표좌표));
        while (후보.Count > 0)
        {
            int 지금 = 후보꺼내기();
            if (닫힘[지금]) continue;
            닫힘[지금] = true;
            if (지금 == 목표) { 최선 = 지금; break; }
            float 차이 = (중심[지금] - 목표위치).sqrMagnitude;
            if (차이 < 목표거리) { 목표거리 = 차이; 최선 = 지금; }
            if (차단목표 && 차이 <= 인접거리 + .0001f && 통행가능(지금)) break;
            var 좌표 = 칸좌표(지금);
            foreach (var 방향 in 방향들)
            {
                int 다음 = 칸번호(좌표 + new Vector3Int(방향.x, 방향.y, 0));
                if (!통행가능(다음) || 닫힘[다음]) continue;
                bool 대각 = 방향.x != 0 && 방향.y != 0;
                if (대각 && (!통행가능(칸번호(좌표 + new Vector3Int(방향.x, 0, 0)))
                    || !통행가능(칸번호(좌표 + new Vector3Int(0, 방향.y, 0))))) continue;
                int 새거리 = 거리[지금] + (대각 ? 14 : 10);
                if (새거리 >= 거리[다음]) continue;
                거리[다음] = 새거리;
                이전[다음] = 지금;
                int 추정 = 추정거리(칸좌표(다음), 목표좌표);
                후보추가(다음, (새거리 + 추정) * 2048 + 추정);
            }
        }
        for (int 칸 = 최선; 칸 != 시작; 칸 = 이전[칸]) 결과.Add(중심[칸]);
        결과.Reverse();
    }

    static int 추정거리(Vector3Int a, Vector3Int b)
    {
        int x = Mathf.Abs(a.x - b.x), y = Mathf.Abs(a.y - b.y);
        return 10 * Mathf.Max(x, y) + 4 * Mathf.Min(x, y);
    }

    void 후보추가(int 칸, int 점수)
    {
        int i = 후보.Count;
        후보.Add((칸, 점수));
        while (i > 0)
        {
            int 부모 = (i - 1) / 2;
            if (후보[부모].점수 <= 점수) break;
            후보[i] = 후보[부모];
            i = 부모;
        }
        후보[i] = (칸, 점수);
    }

    int 후보꺼내기()
    {
        int 처음 = 후보[0].칸;
        var 마지막 = 후보[후보.Count - 1];
        후보.RemoveAt(후보.Count - 1);
        if (후보.Count == 0) return 처음;
        int i = 0;
        while (2 * i + 1 < 후보.Count)
        {
            int 자식 = 2 * i + 1;
            if (자식 + 1 < 후보.Count && 후보[자식 + 1].점수 < 후보[자식].점수) 자식++;
            if (후보[자식].점수 >= 마지막.점수) break;
            후보[i] = 후보[자식];
            i = 자식;
        }
        후보[i] = 마지막;
        return 처음;
    }
}

