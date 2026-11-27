using System;
using UnityEngine;

public sealed partial class 전장규칙
{
    internal const int 디버그허수아비한도 = 4;
    internal bool 디버그연습전 { get; }
    internal bool 디버그AI정지 { get; set; }
    internal bool 디버그AI수동조종 { get; set; }
    internal bool 디버그자동플레이 { get; private set; }
    readonly bool[] 디버그무적팀 = new bool[2];
    readonly bool[] 디버그즉시훈련팀 = new bool[2];

    bool 디버그조작가능(int 팀) => 디버그연습전 && 상태.승자 < 0 && (uint)팀 < 2;

    internal bool 디버그자동플레이전환()
    {
        if (!디버그조작가능(0) || 디버그AI수동조종) return false;
        디버그왕정지(0);
        디버그자동플레이 = !디버그자동플레이;
        if (디버그자동플레이) 진영AI들[0].명령시간 = 0;
        return true;
    }

    internal bool 디버그무적(int 팀) => 디버그무적팀[팀];
    internal bool 디버그즉시훈련(int 팀) => 디버그즉시훈련팀[팀];

    internal void 디버그무적전환(int 팀)
    {
        if (디버그조작가능(팀)) 디버그무적팀[팀] = !디버그무적팀[팀];
    }

    internal void 디버그즉시훈련전환(int 팀)
    {
        if (!디버그조작가능(팀)) return;
        디버그즉시훈련팀[팀] = !디버그즉시훈련팀[팀];
        훈련상태갱신(팀);
    }

    internal void 디버그재화지급(int 팀, int 종류)
    {
        if (!디버그조작가능(팀) || (uint)종류 >= 재화이름.Length) return;
        var 재화 = 상태.진영들[팀].재화;
        재화[종류] = (int)Math.Min(int.MaxValue, 재화[종류] + 1000L);
    }

    internal void 디버그쿨다운초기화(int 팀)
    {
        if (디버그조작가능(팀)) Array.Clear(상태.진영들[팀].쿨다운, 0, 왕직업목록.장착칸수);
    }

    internal void 디버그왕정지(int 팀)
    {
        if (!디버그조작가능(팀)) return;
        int 번호 = 왕번호(팀);
        var 왕 = 상태.대상들[번호];
        이동지정(왕, 왕.위치);
        왕.이동중 = false;
        왕.도약시간 = 0;
        왕방향입력[팀] = default;
        돌진들[팀] = null;
        길찾기.초기화(경로들[번호]);
    }

    internal bool 디버그순간이동(int 팀, Vector2 위치)
    {
        if (!디버그조작가능(팀) || !디버그배치가능(위치)) return false;
        var 왕 = 상태.대상들[왕번호(팀)];
        if (!왕.생존) return false;
        왕.위치 = 위치;
        디버그왕정지(팀);
        return true;
    }

    internal void 디버그완전회복(int 팀)
    {
        if (!디버그조작가능(팀)) return;
        var 왕 = 상태.대상들[왕번호(팀)];
        if (왕.생존) 왕.체력 = 왕.최대체력;
        else
        {
            왕.재등장 = 0;
            왕부활(왕번호(팀), 왕);
        }
    }

    internal bool 디버그직업변경(int 팀, 왕직업 직업)
    {
        if (!디버그조작가능(팀) || !왕직업목록.장착가능(직업)) return false;
        var 진영 = 상태.진영들[팀];
        if (진영.직업 == 직업) return false;

        int 번호 = 왕번호(팀);
        var 이전 = 상태.대상들[번호];
        디버그왕정지(팀);
        진영.직업 = 직업;
        진영.장착구성 = 왕직업목록.기본장착;
        float 최대체력 = 능력치(대상종류.왕, 직업).체력 + 진영.시련유물 * 40;
        var 왕 = 배치(번호, 팀, 대상종류.왕, 이전.위치, 최대체력);
        왕.체력 = 최대체력 * Mathf.Clamp01(이전.체력 / 이전.최대체력);
        왕.재등장 = 이전.재등장;
        왕.시선 = 이전.시선;
        왕.왼쪽 = 이전.왼쪽;
        디버그쿨다운초기화(팀);
        왕기술효과갱신();
        왕능력치갱신();
        return true;
    }

    internal bool 디버그스킬변경(int 팀, int 칸, int 후보)
    {
        if (!디버그조작가능(팀) || (uint)칸 >= 왕직업목록.장착칸수) return false;
        var 진영 = 상태.진영들[팀];
        if ((uint)후보 >= 왕직업목록.장착후보수(진영.직업)) return false;
        int 이전 = 진영.장착구성;
        int 다음 = 왕직업목록.장착변경(이전, 칸, 후보);
        if (이전 == 다음) return false;
        for (int i = 0; i < 왕직업목록.장착칸수; i++)
            if (왕직업목록.후보번호(이전, i) != 왕직업목록.후보번호(다음, i)) 진영.쿨다운[i] = 0;
        진영.장착구성 = 다음;
        return true;
    }

    bool 디버그배치가능(Vector2 위치) =>
        float.IsFinite(위치.x) && float.IsFinite(위치.y) && 길찾기.통행가능(위치);

    internal int 디버그허수아비배치칸(int 팀, Vector2 위치)
    {
        if (!디버그조작가능(팀) || !디버그배치가능(위치)) return -1;
        int 수량 = 0;
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 is not { 연습허수아비: true }) continue;
            if ((대상.위치 - 위치).sqrMagnitude < .64f) return -1;
            수량++;
        }
        if (수량 >= 디버그허수아비한도) return -1;
        int 상대팀 = 1 - 팀;
        int 칸 = 빈칸(상태, 상대팀, 대상종류.병사);
        return 양성예정병력(상태, 상대팀) < 병력제한 ? 칸 : -1;
    }

    internal bool 디버그허수아비배치(int 팀, Vector2 위치)
    {
        int 칸 = 디버그허수아비배치칸(팀, 위치);
        if (칸 < 0) return false;
        var 허수아비 = 배치(칸, 1 - 팀, 대상종류.병사, 위치, 1000);
        허수아비.연습허수아비 = true;
        허수아비.허수아비위치 = 위치;
        return true;
    }

    internal bool 디버그허수아비제거(int 팀, Vector2 위치)
    {
        if (!디버그조작가능(팀)) return false;
        int 번호 = -1;
        float 거리 = .85f * .85f;
        for (int i = 0; i < 상태.대상들.Length; i++)
        {
            if (상태.대상들[i] is not { 연습허수아비: true } 허수아비) continue;
            float 후보 = (허수아비.위치 - 위치).sqrMagnitude;
            if (후보 > 거리) continue;
            거리 = 후보;
            번호 = i;
        }
        if (번호 < 0) return false;
        상태.대상들[번호] = null;
        길찾기.초기화(경로들[번호]);
        return true;
    }

    void 디버그허수아비진행(전장대상 대상, float 초)
    {
        대상.위치 = 대상.목적지 = 대상.허수아비위치;
        대상.이동중 = false;
        대상.효과시간 -= 초;
        if (상태.시간 >= 대상.허수아비회복시각) 대상.체력 = 대상.최대체력;
    }
}
