using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class 전장규칙
{
    const int 적팀 = 1;
    const int 적채집인원 = 6, 적주민목표 = 8, 적모집단계 = 3;
    static readonly float[] 적채집비중 = { 3f, 2f, 1f, 1f, 1f };
    readonly int[] 적채집배정수 = new int[적채집비중.Length];
    readonly List<Vector2> 적경로 = new();
    readonly List<전장대상> 적부대 = new();
    float 적명령시간;
    bool 적진격, 적왕후퇴;
    Vector2 적부대목표;

    void 적진행(float 초)
    {
        if ((적명령시간 -= 초) > 0) return;
        적명령시간 = 2.5f;
        var 회관 = 상태.대상들[회관번호(적팀)];
        var 상대회관 = 상태.대상들[회관번호(1 - 적팀)];
        var 왕 = 상태.대상들[왕번호(적팀)];
        Vector2 전방 = new(-Mathf.Sign(회관.위치.x), 0);
        Vector2 집결지 = 회관.위치 + 전방 * 5;
        int 침입자 = 적군탐색(회관.위치, 10);
        적경제(회관.위치, 전방, 침입자 >= 0);
        적부대지휘(왕, 상대회관.위치, 집결지, 침입자);
        적왕지휘(왕, 회관.위치 - 전방 * 3, 집결지, 침입자);
    }

    void 적경제(Vector2 회관, Vector2 전방, bool 방어중)
    {
        int 시작 = 왕번호(적팀) + 6, 끝 = 왕번호(적팀) + 병사시작칸;
        var 진영 = 상태.진영들[적팀];
        적건설(0, 대상종류.제재소, 회관 + 전방 * 2.9f + Vector2.up * 1.8f);
        적건설(1, 대상종류.훈련소, 회관 + 전방 * 3f - Vector2.up * 1.8f);

        int 일꾼 = 수량(상태, 적팀, 대상종류.주민) - 진영.훈련대기인원;
        if (일꾼 < 적주민목표)
        {
            var 단계 = 주민투자단계들[적모집단계];
            int 모집 = 주민추첨최대인원(상태, 적팀, 단계);
            // 경제가 자리 잡으면 훈련비만큼 나무를 남기고 보충한다.
            if (일꾼 >= 적채집인원)
                모집 = Mathf.Min(모집, (진영.재화[0] - 비용[3][0]) / 단계.나무);
            모집 = Mathf.Min(모집, 적주민목표 - 일꾼);
            if (모집 > 0) 주민추첨시작(적팀, 적모집단계, 모집);
        }

        if (일꾼 > 적채집인원 && 진영.훈련대기인원 < 2
            && 지불가능(진영.재화, 3) && 수량(상태, 적팀, 대상종류.병사) + 훈련예약인원(적팀) < 병력제한
            && 완성훈련소(적팀) is { } 훈련소 && 훈련자리(훈련소, out var 입구))
        {
            int 후보 = -1;
            float 거리 = float.PositiveInfinity;
            for (int i = 시작; i < 끝; i++)
            {
                var 주민 = 상태.대상들[i];
                if (주민 is not { 생존: true, 훈련소: < 0 } || 주민.속박중) continue;
                float 제곱거리 = (주민.위치 - 훈련소.위치).sqrMagnitude;
                if (제곱거리 >= 거리 || !적도달가능(주민.위치, 입구, .15f)) continue;
                거리 = 제곱거리;
                후보 = i;
            }
            if (후보 >= 0) 주민훈련접수(적팀, Array.IndexOf(상태.대상들, 훈련소), new[] { 후보 });
        }

        if (!방어중) 적채집배정(시작, 끝, 회관, 진영.재화);
    }

    void 적건설(int 종류, 대상종류 대상, Vector2 선호위치)
    {
        if (수량(상태, 적팀, 대상) > 0 || !지불가능(상태.진영들[적팀].재화, 종류)) return;
        for (int 반경 = 0; 반경 <= 2; 반경++)
        for (int y = -반경; y <= 반경; y++)
        for (int x = -반경; x <= 반경; x++)
        {
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != 반경) continue;
            if (!건설가능(상태, 길찾기, 자원들, 적팀, 종류, 선호위치 + new Vector2(x, y), out var 위치)) continue;
            건설(적팀, 종류, 위치);
            return;
        }
    }

    void 적채집배정(int 시작, int 끝, Vector2 회관, int[] 재화)
    {
        Array.Clear(적채집배정수, 0, 적채집배정수.Length);
        for (int i = 시작; i < 끝; i++)
        {
            var 주민 = 상태.대상들[i];
            if (주민 is not { 생존: true, 훈련소: < 0, 채집터: >= 0 }) continue;
            var 지점 = 자원들[주민.채집터];
            if (!주민.이동중 && ((Vector2)지점.오브젝트.position - 주민.위치).sqrMagnitude > 채집거리 * 채집거리)
                주민.채집터 = -1;
            else
                적채집배정수[지점.재화번호]++;
        }
        for (int i = 시작; i < 끝; i++)
        {
            var 주민 = 상태.대상들[i];
            if (주민 is not { 생존: true, 훈련소: < 0, 채집터: < 0 } || 주민.속박중) continue;
            int 후보 = -1;
            float 최고점수 = float.NegativeInfinity;
            for (int j = 0; j < 자원들.Length; j++)
            {
                var 지점 = 자원들[j];
                Vector2 위치 = 지점.오브젝트.position;
                int 종류 = 지점.재화번호;
                float 중요도 = 적채집비중[종류] + (재화[종류] < 비용[3][종류] ? 2f : 0);
                float 거리 = Vector2.Distance(회관, 위치) + Vector2.Distance(주민.위치, 위치) * .5f;
                float 점수 = 중요도 / ((1 + 적채집배정수[종류]) * (1 + 거리 * .08f));
                if (점수 <= 최고점수 || !적도달가능(주민.위치, 위치, 채집거리)) continue;
                최고점수 = 점수;
                후보 = j;
            }
            if (후보 < 0) continue;
            이동지정(주민, 자원들[후보].오브젝트.position);
            주민.채집터 = 후보;
            적채집배정수[자원들[후보].재화번호]++;
        }
    }

    bool 적도달가능(Vector2 시작, Vector2 목표, float 거리)
    {
        if ((시작 - 목표).sqrMagnitude <= 거리 * 거리) return true;
        if (길찾기.통행가능(목표) && 길찾기.중심정렬(시작) == 길찾기.중심정렬(목표)) return true;
        길찾기.구하기(시작, 목표, 적경로);
        if (적경로.Count == 0) return false;
        var 끝 = 적경로[적경로.Count - 1];
        return (끝 - 목표).sqrMagnitude <= 거리 * 거리
            || (길찾기.통행가능(목표) && 끝 == 길찾기.중심정렬(목표));
    }

    int 적군탐색(Vector2 위치, float 사거리)
    {
        int 표적 = -1;
        float 거리 = 사거리 * 사거리;
        for (int i = 왕번호(1 - 적팀); i < 왕번호(1 - 적팀) + 진영칸; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 is not { 생존: true, 건물: false }) continue;
            float 후보 = (대상.위치 - 위치).sqrMagnitude;
            if (후보 >= 거리 || !표적보임(적팀, 대상)) continue;
            거리 = 후보;
            표적 = i;
        }
        return 표적;
    }

    void 적부대지휘(전장대상 왕, Vector2 상대회관, Vector2 집결지, int 침입자)
    {
        적부대.Clear();
        int 병력 = 0;
        int 시작 = 왕번호(적팀) + 병사시작칸, 끝 = 왕번호(적팀) + 진영칸;
        for (int i = 시작; i < 끝; i++)
            if (상태.대상들[i] is { 생존: true, 훈련소: < 0, 소환: 소환종류.없음 }) 병력++;

        bool 진격 = 침입자 < 0 && (적진격 ? 병력 >= 3
            : 왕.생존 && (상태.시간 >= 95 && 병력 >= 5 || 상태.시간 >= 180 && 병력 >= 3));
        Vector2 목표 = 침입자 >= 0 ? 상태.대상들[침입자].위치 : 진격 ? 상대회관 : 집결지;
        bool 목표갱신 = 적진격 != 진격 || (적부대목표 - 목표).sqrMagnitude > 9;
        적진격 = 진격;
        if (목표갱신) 적부대목표 = 목표;

        for (int i = 시작; i < 끝; i++)
        {
            var 병사 = 상태.대상들[i];
            if (병사 is not { 생존: true, 훈련소: < 0, 소환: 소환종류.없음 } || 병사.속박중) continue;
            if ((!목표갱신 && 병사.이동중) || (병사.위치 - 목표).sqrMagnitude <= 16) continue;
            적부대.Add(병사);
        }
        부대이동지정(적부대, 제한(목표));
    }

    void 적왕지휘(전장대상 왕, Vector2 후방, Vector2 집결지, int 침입자)
    {
        if (!왕.생존) { 적왕후퇴 = false; return; }
        if (왕.속박중 || 돌진들[적팀] != null || 왕공격동작중(왕)) return;

        var 직업 = 상태.진영들[적팀].직업;
        bool 장착 = 왕직업목록.장착가능(직업);
        bool 신규 = 왕직업목록.신규(직업);
        float 체력비율 = 왕.체력 / 왕.최대체력;
        if (체력비율 < .6f && 직업 == 왕직업.트린다미어) 시전(적팀, 왕, 0, 왕.위치);
        else if (체력비율 < .7f && !장착 && !신규) 시전(적팀, 왕, 2, 왕.위치);

        적왕후퇴 = 왕.체력 < 왕.최대체력 * (적왕후퇴 ? .6f : .3f);
        if (적왕후퇴)
        {
            if (장착) 장착스킬AI(적팀, 왕, -1);
            if ((왕.위치 - 후방).sqrMagnitude > 1 || 왕.표적 >= 0) 이동지정(왕, 제한(후방));
            return;
        }

        int 표적 = 적군탐색(왕.위치, 6);
        if (표적 < 0 && 적진격) 표적 = 가까운적(왕, 5);
        if (장착) 장착스킬AI(적팀, 왕, 표적);
        else if (표적 >= 0)
        {
            왕.표적 = 표적;
            var 위치 = 상태.대상들[표적].위치;
            if (신규)
                for (int 기술 = 0; 기술 < 4; 기술++) 시전(적팀, 왕, 기술, 위치);
            else
            {
                시전(적팀, 왕, 0, 위치);
                시전(적팀, 왕, 3, 위치);
            }
        }
        if (표적 >= 0 || 왕.표적 >= 0 || 돌진들[적팀] != null) return;

        Vector2 목적 = 침입자 >= 0 ? 상태.대상들[침입자].위치 : 집결지;
        if (적진격)
        {
            // 가까운 병사와 함께 전진하고, 뒤처졌으면 가장 가까운 병력과 합류한다.
            float 거리 = float.PositiveInfinity;
            for (int i = 왕번호(적팀) + 병사시작칸; i < 왕번호(적팀) + 진영칸; i++)
            {
                if (상태.대상들[i] is not { 생존: true, 훈련소: < 0, 소환: 소환종류.없음 } 병사) continue;
                float 후보 = (병사.위치 - 왕.위치).sqrMagnitude;
                if (후보 >= 거리) continue;
                거리 = 후보;
                목적 = 후보 <= 36 && 병사.이동중
                    ? Vector2.MoveTowards(병사.위치, 병사.목적지, 3) : 병사.위치;
            }
        }
        if ((왕.위치 - 목적).sqrMagnitude > 2.25f) 이동지정(왕, 제한(목적));
        else if (왕.이동중) 이동지정(왕, 왕.위치);
    }
}
