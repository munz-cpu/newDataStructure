using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class 전장규칙
{
    const int AI채집인원 = 8, AI주민목표 = 20, AI모집단계 = 3;
    const int AI수비병력 = 3, AI출격병력 = 8, AI최소출격병력 = 6, AI후반출격병력 = 20;
    const int AI후반분당증원 = 5;
    const float 첫진격준비시간 = 150f, 첫소규모진격준비시간 = 300f;
    const float AI후반시작 = 360f;
    const int AI탐사간격 = 4;
    static readonly float[] AI채집비중 = { 2f, 2f, 1.5f, 1.5f, 1f };
    static readonly int[] AI최소비축 = { 0, 35, 12, 10, 4 };
    readonly int[] AI채집배정수 = new int[AI채집비중.Length];
    readonly bool[] AI부족재화 = new bool[AI채집비중.Length];
    readonly bool[] AI발견채집종류 = new bool[AI채집비중.Length];
    readonly List<전장대상> AI부대 = new(), AI수비대 = new(), AI원정대 = new();
    readonly List<(Vector2 위치, float 반경제곱)> AI시야목록 = new();
    Vector2[] AI탐사지점들;

    sealed class AI정찰임무
    {
        public 전장대상 유닛;
        public int 목표 = -1;
        public float 제한시간;
    }

    sealed class 진영AI상태
    {
        public float 명령시간, 다음출격, 출격만료;
        public bool 진격, 첫진격완료, 왕후퇴;
        public Vector2 부대목표;
        public Vector2? 발견한적회관;
        public readonly AI정찰임무[] 정찰들 = { new(), new() };
        public readonly HashSet<전장대상> 출격대 = new();
        public bool 탐사완료;
        public bool 자원탐사필요;
        public AI정찰임무 감시임무;
        public Vector2? 감시위치;
        public bool[] 탐사칸, 발견자원;
        public float[] 정찰재시도;

        public bool 정찰중(전장대상 유닛)
        {
            foreach (var 정찰 in 정찰들)
                if (정찰.유닛 == 유닛) return true;
            return false;
        }
    }

    readonly 진영AI상태[] 진영AI들 = { new(), new() };

    bool AI조종중(int 팀) => 팀 == 0 ? 디버그자동플레이 : !디버그AI정지 && !디버그AI수동조종;

    void AI진행(int 팀, float 초)
    {
        var 판단 = 진영AI들[팀];
        if ((판단.명령시간 -= 초) > 0) return;
        판단.명령시간 = 2.5f;
        var 회관 = 상태.대상들[회관번호(팀)];
        var 왕 = 상태.대상들[왕번호(팀)];
        Vector2 전방 = new(회관.위치.x == 길찾기.전장범위.center.x
            ? (팀 == 0 ? 1 : -1) : Mathf.Sign(길찾기.전장범위.center.x - 회관.위치.x), 0);
        AI탐사기록(팀, 판단);
        var 상대회관 = 상태.대상들[회관번호(1 - 팀)];
        if (AI시야안(상대회관.위치))
            판단.발견한적회관 = 상대회관.생존 ? 상대회관.위치 : null;
        int 침입자 = 적군탐색(팀, 회관.위치, 10, 전투유닛만: true);
        Vector2 수비지 = AI이동자리(회관.위치, 회관.위치 + 전방 * 2);
        AI정찰지휘(팀, 판단, 회관.위치, 수비지, 전방);
        bool 본진준비 = AI경제(팀, 판단, 회관.위치, 전방, 침입자 >= 0);
        Vector2 집결지 = AI이동자리(수비지, 회관.위치 + 전방 * 5);
        AI부대지휘(팀, 판단, 왕, 집결지, 수비지, 침입자, 본진준비);
        AI왕지휘(팀, 판단, 왕, 회관.위치 - 전방 * 3, 집결지, 침입자);
    }

    void AI탐사기록(int 팀, 진영AI상태 판단)
    {
        AI시야목록.Clear();
        foreach (var 대상 in 상태.대상들)
            if (AI시야원(대상, 팀))
                AI시야목록.Add((대상.위치, 전장안개.시야반경 * 전장안개.시야반경));
        foreach (var 지대 in 상태.지대들)
            if (지대.팀 == 팀 && 지대.종류 == 지대종류.시야)
                AI시야목록.Add((지대.위치, 지대.반경 * 지대.반경));

        if (AI탐사지점들 == null)
        {
            var 범위 = 길찾기.전장범위;
            int 너비 = Mathf.CeilToInt(범위.width / AI탐사간격);
            int 높이 = Mathf.CeilToInt(범위.height / AI탐사간격);
            AI탐사지점들 = new Vector2[너비 * 높이];
            for (int y = 0; y < 높이; y++)
            for (int x = 0; x < 너비; x++)
                AI탐사지점들[y * 너비 + x] = 길찾기.중심정렬(제한(범위.min + new Vector2(x + .5f, y + .5f) * AI탐사간격));
        }
        판단.탐사칸 ??= new bool[AI탐사지점들.Length];
        판단.정찰재시도 ??= new float[AI탐사지점들.Length];
        판단.발견자원 ??= new bool[자원들.Length];
        for (int i = 0; i < 자원들.Length; i++)
            if (!판단.발견자원[i]) 판단.발견자원[i] = AI시야안(자원들[i].위치);
        for (int i = 0; i < 판단.탐사칸.Length; i++)
            if (!판단.탐사칸[i]) 판단.탐사칸[i] = AI시야안(AI탐사지점들[i]);
    }

    bool AI시야안(Vector2 위치)
    {
        foreach (var 시야 in AI시야목록)
            if ((시야.위치 - 위치).sqrMagnitude <= 시야.반경제곱) return true;
        return false;
    }

    static bool AI시야원(전장대상 대상, int 팀) =>
        대상 is { 생존: true, 울타리: false, 연습허수아비: false, 훈련남음: <= 0 } && 대상.팀 == 팀;

    void AI왕표적확인(전장대상 왕)
    {
        if (!적인가(왕.팀, 왕.표적)) return;
        var 표적 = 상태.대상들[왕.표적];
        const float 시야제곱 = 전장안개.시야반경 * 전장안개.시야반경;
        if (표적보임(왕.팀, 표적))
        {
            if ((왕.위치 - 표적.위치).sqrMagnitude <= 시야제곱 || 정찰범위(상태, 왕.팀, 표적.위치)) return;
            foreach (var 아군 in 상태.대상들)
                if (AI시야원(아군, 왕.팀) && (아군.위치 - 표적.위치).sqrMagnitude <= 시야제곱) return;
        }
        왕.표적 = -1;
        왕.지정공격 = false;
    }

    void AI정찰지휘(int 팀, 진영AI상태 판단, Vector2 회관, Vector2 수비지, Vector2 전방)
    {
        if (판단.발견한적회관 is Vector2 적회관 && !판단.탐사완료)
        {
            판단.탐사완료 = true;
            AI감시전환(판단, 적회관, 회관, 수비지);
        }
        if (판단.감시임무 is { } 감시 && 판단.감시위치 is Vector2 감시위치)
        {
            if (감시.유닛 is not { 생존: true, 훈련소: < 0 } 감시병
                || !(감시병.이동중 && 상태.시간 < 감시.제한시간)
                    && (감시병.위치 - 감시위치).sqrMagnitude > .25f)
            {
                AI정찰복귀(감시, 수비지);
                판단.감시임무 = null;
                판단.감시위치 = null;
            }
        }
        if (판단.탐사완료 && !판단.자원탐사필요)
        {
            foreach (var 임무 in 판단.정찰들)
                if (임무 != 판단.감시임무 && 임무.유닛 != null) AI정찰복귀(임무, 수비지);
            return;
        }

        int 주민 = 수량(상태, 팀, 대상종류.주민) - 훈련예약인원(팀);
        int 시작 = 왕번호(팀);
        for (int i = 0; i < 판단.정찰들.Length; i++)
        {
            var 임무 = 판단.정찰들[i];
            if (임무 == 판단.감시임무) continue;
            int 필요주민 = i == 0 || 판단.감시임무 == 판단.정찰들[0] ? 4 : 8;
            if (임무.유닛 is { 생존: false } && 임무.목표 >= 0)
            {
                판단.정찰재시도[임무.목표] = 상태.시간 + 90;
                임무.제한시간 = 상태.시간 + 30;
            }
            if (임무.유닛 is not { 생존: true, 훈련소: < 0 } || 주민 < 필요주민)
                AI정찰복귀(임무, 수비지);
            if (임무.유닛 == null)
            {
                if (주민 < 필요주민 || 상태.시간 < 임무.제한시간) continue;
                float 거리 = 100f;
                for (int 칸 = 시작 + 6; 칸 < 시작 + 병사시작칸; 칸++)
                {
                    var 후보 = 상태.대상들[칸];
                    if (후보 is not { 생존: true, 훈련소: < 0, 종류: 대상종류.주민 }
                        || 후보.속박중 || 판단.정찰중(후보)) continue;
                    float 제곱거리 = (후보.위치 - 회관).sqrMagnitude;
                    if (제곱거리 >= 거리) continue;
                    거리 = 제곱거리;
                    임무.유닛 = 후보;
                }
            }
            var 정찰병 = 임무.유닛;
            if (정찰병 == null || 정찰병.속박중) continue;
            if (임무.목표 >= 0)
            {
                if (정찰병.이동중 && 상태.시간 < 임무.제한시간) continue;
                if (!판단.탐사칸[임무.목표]) 판단.정찰재시도[임무.목표] = 상태.시간 + 45;
                if (정찰병.이동중) 이동지정(정찰병, 정찰병.위치);
                임무.목표 = -1;
            }

            // 막힌 후보는 잠시 건너뛰고 다음 판단에서 다른 길을 찾는다.
            for (int 시도 = 0; 시도 < 4; 시도++)
            {
                bool 자원중심 = 판단.자원탐사필요 && (판단.탐사완료 || i > 0);
                int 후보 = AI정찰목표(판단, 임무, 회관, 전방, 자원중심);
                if (후보 < 0)
                {
                    AI정찰복귀(임무, 수비지);
                    임무.제한시간 = 상태.시간 + 20;
                    break;
                }
                Vector2 위치 = AI탐사지점들[후보];
                if (!도달가능(정찰병.위치, 위치, .2f))
                {
                    판단.정찰재시도[후보] = 상태.시간 + 45;
                    continue;
                }
                임무.목표 = 후보;
                임무.제한시간 = 상태.시간 + 20 + Vector2.Distance(정찰병.위치, 위치) * 1.5f;
                이동지정(정찰병, 위치);
                break;
            }
        }
    }

    static void AI정찰복귀(AI정찰임무 임무, Vector2 수비지)
    {
        if (임무.유닛 is { 생존: true, 훈련소: < 0 } 정찰병) 이동지정(정찰병, 수비지);
        임무.유닛 = null;
        임무.목표 = -1;
    }

    void AI감시전환(진영AI상태 판단, Vector2 적회관, Vector2 회관, Vector2 수비지)
    {
        AI정찰임무 감시 = null;
        Vector2 감시지 = default;
        float 최고점수 = float.PositiveInfinity;
        var 지형 = 길찾기.지형;
        if (지형 != null)
        {
            var 중심 = 길찾기.중심정렬(적회관);
            foreach (var 임무 in 판단.정찰들)
            {
                if (임무.유닛 is not { 생존: true, 훈련소: < 0 } 주민 || 주민.속박중
                    || (주민.위치 - 적회관).sqrMagnitude > 18 * 18) continue;
                for (int y = -14; y <= 14; y++)
                for (int x = -14; x <= 14; x++)
                {
                    var 후보 = 중심 + new Vector2(x, y);
                    float 적거리 = Vector2.Distance(후보, 적회관);
                    if (적거리 < 5 || 적거리 > 14 || !지형.수풀인가(후보) || !길찾기.통행가능(후보)) continue;
                    float 점수 = 적거리 + Vector2.Distance(주민.위치, 후보) * .7f + Vector2.Distance(회관, 후보) * .25f;
                    if (점수 >= 최고점수 || !도달가능(주민.위치, 후보, .2f)) continue;
                    최고점수 = 점수;
                    감시지 = 후보;
                    감시 = 임무;
                }
            }
        }
        foreach (var 임무 in 판단.정찰들)
        {
            if (임무 != 감시) { AI정찰복귀(임무, 수비지); continue; }
            임무.목표 = -1;
            임무.제한시간 = 상태.시간 + 30;
            판단.감시임무 = 임무;
            판단.감시위치 = 감시지;
            이동지정(임무.유닛, 감시지);
        }
    }

    int AI정찰목표(진영AI상태 판단, AI정찰임무 임무, Vector2 회관, Vector2 전방, bool 자원중심)
    {
        int 후보 = -1;
        float 최고점수 = float.NegativeInfinity;
        for (int i = 0; i < 판단.탐사칸.Length; i++)
        {
            if (판단.탐사칸[i] || 판단.정찰재시도[i] > 상태.시간) continue;
            Vector2 위치 = AI탐사지점들[i];
            if (!길찾기.통행가능(위치)) continue;
            Vector2 이동 = 위치 - 임무.유닛.위치;
            float 점수 = Vector2.Dot(이동, 전방) * .35f - 이동.magnitude;
            if (자원중심)
            {
                점수 -= Vector2.Distance(위치, 회관) * .55f;
                if (판단.발견한적회관 is Vector2 적회관)
                    점수 -= Mathf.Max(0, 16 - Vector2.Distance(위치, 적회관)) * 3f;
            }
            foreach (var 다른임무 in 판단.정찰들)
            {
                if (다른임무.목표 < 0) continue;
                if (다른임무.목표 == i) { 점수 = float.NegativeInfinity; break; }
                점수 -= Mathf.Max(0, 12 - Vector2.Distance(위치, AI탐사지점들[다른임무.목표])) * 2;
            }
            if (점수 <= 최고점수) continue;
            최고점수 = 점수;
            후보 = i;
        }
        return 후보;
    }

    bool AI경제(int 팀, 진영AI상태 판단, Vector2 회관, Vector2 전방, bool 방어중)
    {
        int 주민 = 수량(상태, 팀, 대상종류.주민) - 훈련예약인원(팀);
        int 일꾼 = 주민;
        foreach (var 정찰 in 판단.정찰들)
            if (정찰.유닛 is { 종류: 대상종류.주민 }) 일꾼--;
        var 진영 = 상태.진영들[팀];
        int[] 재화 = 진영.재화;
        var 단계 = 주민투자단계들[AI모집단계];
        int 병력 = 양성예정병력(상태, 팀);
        bool 후반 = 상태.시간 >= AI후반시작;
        int 최소총병력 = AI출격기준(상태.시간) + AI수비병력;
        int 목표병력 = 후반 ? Mathf.Clamp(최소총병력 + 2, 32, 병력제한)
            : 상태.시간 >= 240 || 판단.첫진격완료 ? 24 : 17;
        bool 병력우선 = 후반 && 병력 < 목표병력;
        bool 전원집결중 = 최소총병력 == 병력제한 && !판단.진격;

        AI건설(팀, 0, 회관 + 전방 * 3f + Vector2.up * 2);
        if (주민 >= 5)
        {
            AI건설(팀, 1, 회관 - Vector2.up * 2);
            if (!병력우선) AI건설(팀, 0, 회관 - 전방 * 3f + Vector2.up * 2, 2);
        }

        int 주민목표 = 일꾼 < AI채집인원 ? 10 : 방어중 ? 12 : 병력우선 ? 14 : AI주민목표;
        int 모집예산 = 주민 < 주민목표 ? 단계.나무 * (병력우선 || 병력 < AI출격병력 + AI수비병력 ? 1 : 2) : 0;
        int 제재소 = 왕번호(팀) + 2;
        bool 본진준비 = 일꾼 >= AI채집인원 && 완성훈련소(팀) != null
            && (상태.대상들[제재소] is { 생존: true, 완공대기: <= 0 }
                || 상태.대상들[제재소 + 1] is { 생존: true, 완공대기: <= 0 });
        if (!병력우선 && !전원집결중 && 본진준비 && 일꾼 >= 10)
            AI마을확장(팀, 회관, 전방, 병력, 모집예산);

        if (진영.훈련대기인원 < (후반 ? 2 : 1))
        {
            int 이전대기수 = 진영.훈련대기인원;
            bool 기본병력부족 = 후반 && 병력 < 최소총병력;
            if (!기본병력부족 && !전원집결중 && 병력 >= 9 && AI훈련지불가능(재화, 0, 모집예산))
            {
                int 브루저 = AI병과수(팀, 병과.브루저);
                int 아처 = AI병과수(팀, 병과.아처);
                int 브루저부족 = (병력 + 1) / 3 - 브루저;
                int 아처부족 = 병력 / 3 - 아처;
                if (아처부족 > 브루저부족 && 아처부족 > 0)
                    AI전문훈련(팀, 판단, 회관, 아처건설);
                if (진영.훈련대기인원 == 이전대기수 && 브루저부족 > 0)
                    AI전문훈련(팀, 판단, 회관, 브루저건설);
                if (진영.훈련대기인원 == 이전대기수 && 아처부족 > 0 && 아처부족 <= 브루저부족)
                    AI전문훈련(팀, 판단, 회관, 아처건설);
            }
            if (진영.훈련대기인원 == 이전대기수 && 일꾼 > AI채집인원 && 병력 < 목표병력 && AI훈련지불가능(재화, 병력, 모집예산))
                for (int 학교 = 왕번호(팀) + 4; 학교 <= 왕번호(팀) + 5; 학교++)
                    if (AI훈련(팀, 판단, 회관, 학교)) break;
        }

        int 모집 = Mathf.Min(주민추첨최대인원(상태, 팀, 단계), Mathf.Min(주민목표 - 주민, 주민 < 10 ? 5 : 2));
        if (모집 > 0) 주민추첨시작(팀, AI모집단계, 모집);

        AI채집배정(팀, 판단, 회관, 재화, 병력, 모집예산);
        return 본진준비;
    }

    void AI마을확장(int 팀, Vector2 회관, Vector2 전방, int 병력, int 모집예산)
    {
        int[] 재화 = 상태.진영들[팀].재화;
        bool 건설예산(int 종류) => 재화[0] >= 비용[종류][0] + 모집예산;
        if (건설예산(브루저건설) && AI건설(팀, 브루저건설, 회관 - 전방 * 3 - Vector2.up * 2)) return;
        if (건설예산(아처건설) && AI건설(팀, 아처건설, 회관 + 전방 * 2 - Vector2.up * 4)) return;
        if (병력 < 5) return;
        if (건설예산(모닥불건설) && AI건설(팀, 모닥불건설, 회관 + 전방 * 3)) return;
        if (병력 < 8) return;

        int 타워 = 건설수량(상태, 팀, 타워건설);
        if (타워 == 0 && AI병과수(팀, 병과.아처) >= 3 && 건설예산(타워건설)
            && AI건설(팀, 타워건설, 회관 + 전방 * 4 + Vector2.up * 2)) return;

        int 깃발수 = 건설수량(상태, 팀, 깃발건설);
        if (깃발수 < 2 && 건설예산(깃발건설))
        {
            Vector2 위 = 회관 + 전방 * 5 + Vector2.up * 4;
            Vector2 아래 = 회관 + 전방 * 5 - Vector2.up * 4;
            if (깃발수 == 1)
                for (int i = 깃발시작칸 + 팀 * 깃발제한; i < 깃발시작칸 + (팀 + 1) * 깃발제한; i++)
                    if (상태.대상들[i] is { 생존: true } 깃발 && 깃발.위치.y > 회관.y)
                        (위, 아래) = (아래, 위);
            if (AI건설(팀, 깃발건설, 위, 깃발수 + 1) || 깃발수 == 0 && AI건설(팀, 깃발건설, 아래)) return;
        }
        if (건설예산(1) && AI건설(팀, 1, 회관 - 전방 * 2 + Vector2.up * 4, 2)) return;
        if (병력 >= 12 && 타워 == 1 && AI병과수(팀, 병과.아처) >= 4 && 건설예산(타워건설))
            AI건설(팀, 타워건설, 회관 + 전방 * 9 - Vector2.up * 3, 2);
    }

    int AI병과수(int 팀, 병과 종류)
    {
        int 수 = 0;
        for (int i = 왕번호(팀) + 병사시작칸; i < 왕번호(팀) + 진영칸; i++)
            if (병과선택가능(상태.대상들[i], 팀, 종류)) 수++;
        return 수;
    }

    static bool AI훈련지불가능(int[] 재화, int 병력, int 모집예산)
    {
        for (int i = 0; i < 재화.Length; i++)
            if (재화[i] < 병사훈련비용(i, 병력) + (i == 0 ? 모집예산 : 0)) return false;
        return true;
    }

    bool AI전문훈련(int 팀, 진영AI상태 판단, Vector2 회관, int 종류)
    {
        int 학교 = 시설시작(팀) + (종류 == 브루저건설 ? 0 : 2);
        return AI훈련(팀, 판단, 회관, 학교) || AI훈련(팀, 판단, 회관, 학교 + 1);
    }

    bool AI훈련(int 팀, 진영AI상태 판단, Vector2 회관, int 학교)
    {
        var 건물 = 상태.대상들[학교];
        if (건물 is not { 생존: true, 완공대기: <= 0 } || !훈련자리(건물, out var 입구)) return false;
        bool 전문 = 전문훈련소(건물);
        int 시작 = 왕번호(팀) + (전문 ? 병사시작칸 : 6);
        int 끝 = 왕번호(팀) + (전문 ? 진영칸 : 병사시작칸);
        int 후보 = -1;
        float 거리 = float.PositiveInfinity;
        for (int i = 시작; i < 끝; i++)
        {
            var 유닛 = 상태.대상들[i];
            if (!훈련가능유닛(팀, 건물, 유닛) || 유닛.속박중 || 판단.정찰중(유닛)) continue;
            if (전문 && ((유닛.위치 - 회관).sqrMagnitude > 144 || 판단.출격대.Contains(유닛))) continue;
            float 제곱거리 = (유닛.위치 - 입구).sqrMagnitude;
            if (제곱거리 >= 거리 || !도달가능(유닛.위치, 입구, .15f)) continue;
            거리 = 제곱거리;
            후보 = i;
        }
        if (후보 < 0) return false;
        int 이전대기수 = 상태.진영들[팀].훈련대기인원;
        주민훈련접수(팀, 학교, new[] { 후보 });
        return 상태.진영들[팀].훈련대기인원 > 이전대기수;
    }

    bool AI건설(int 팀, int 종류, Vector2 선호위치, int 목표수 = 1)
    {
        if (건설수량(상태, 팀, 종류) >= 목표수 || !지불가능(상태.진영들[팀].재화, 종류)) return false;
        for (int 반경 = 0; 반경 <= 2; 반경++)
        for (int y = -반경; y <= 반경; y++)
        for (int x = -반경; x <= 반경; x++)
        {
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != 반경) continue;
            if (!건설가능(상태, 길찾기, 자원들, 팀, 종류, 선호위치 + new Vector2(x, y), out var 위치)) continue;
            건설(팀, 종류, 위치);
            return true;
        }
        return false;
    }

    void AI채집배정(int 팀, 진영AI상태 판단, Vector2 회관, int[] 재화, int 병력, int 모집예산)
    {
        int 시작 = 왕번호(팀) + 6, 끝 = 왕번호(팀) + 병사시작칸;
        Array.Clear(AI채집배정수, 0, AI채집배정수.Length);
        Array.Clear(AI발견채집종류, 0, AI발견채집종류.Length);
        for (int 종류 = 0; 종류 < AI부족재화.Length; 종류++)
        {
            int 필요 = 병사훈련비용(종류, 병력) * 2 + (종류 == 0 ? 모집예산 : 0);
            AI부족재화[종류] = 재화[종류] < Mathf.Max(AI최소비축[종류], 필요);
        }
        for (int i = 0; i < 자원들.Length; i++)
            if (판단.발견자원[i] && 채집가능(i)) AI발견채집종류[자원들[i].재화번호] = true;

        for (int i = 시작; i < 끝; i++)
        {
            var 주민 = 상태.대상들[i];
            if (주민 is not { 생존: true, 훈련소: < 0, 채집터: >= 0 }) continue;
            var 지점 = 자원들[주민.채집터];
            if (!주민.이동중 && ((Vector2)지점.위치 - 주민.위치).sqrMagnitude > 채집거리 * 채집거리)
                주민.채집터 = -1;
            else
                AI채집배정수[지점.재화번호]++;
        }
        // 부족한 재료를 발견하면 기존 작업에 여유가 있는 일꾼부터 옮긴다.
        for (int 종류 = 0; 종류 < AI부족재화.Length; 종류++)
        {
            if (!AI부족재화[종류] || AI채집배정수[종류] > 0 || !AI발견채집종류[종류]) continue;
            for (int i = 시작; i < 끝; i++)
            {
                var 주민 = 상태.대상들[i];
                if (주민 is not { 생존: true, 훈련소: < 0 } || 주민.속박중 || 판단.정찰중(주민)) continue;
                int 이전종류 = 주민.채집터 < 0 ? -1 : 자원들[주민.채집터].재화번호;
                if (이전종류 < 0 ? 주민.이동중 : AI채집배정수[이전종류] <= 1) continue;
                int 후보 = AI채집후보(판단, 회관, 주민.위치, 종류);
                if (후보 < 0) continue;
                채집지정(주민, 후보);
                if (이전종류 >= 0) AI채집배정수[이전종류]--;
                AI채집배정수[종류]++;
                break;
            }
        }
        for (int i = 시작; i < 끝; i++)
        {
            var 주민 = 상태.대상들[i];
            if (주민 is not { 생존: true, 훈련소: < 0, 채집터: < 0, 이동중: false }
                || 주민.속박중 || 판단.정찰중(주민)) continue;
            int 후보 = AI채집후보(판단, 회관, 주민.위치);
            if (후보 < 0) continue;
            채집지정(주민, 후보);
            AI채집배정수[자원들[후보].재화번호]++;
        }
        판단.자원탐사필요 = false;
        for (int 종류 = 0; 종류 < AI부족재화.Length; 종류++)
            판단.자원탐사필요 |= AI부족재화[종류] && AI채집배정수[종류] == 0;
    }

    int AI채집후보(진영AI상태 판단, Vector2 회관, Vector2 주민위치, int 원하는종류 = -1)
    {
        int 후보 = -1;
        float 최고점수 = float.NegativeInfinity;
        for (int j = 0; j < 자원들.Length; j++)
        {
            if (!판단.발견자원[j] || !채집가능(j)) continue;
            var 지점 = 자원들[j];
            int 종류 = 지점.재화번호;
            if (원하는종류 >= 0 && 종류 != 원하는종류) continue;
            Vector2 위치 = 지점.위치;
            float 중요도 = AI채집비중[종류] + (AI부족재화[종류] ? 2f : 0);
            float 거리 = Vector2.Distance(회관, 위치) + Vector2.Distance(주민위치, 위치) * .5f;
            float 점수 = 중요도 / ((1 + AI채집배정수[종류]) * (1 + 거리 * .08f));
            if (점수 <= 최고점수 || 지점.숲나무 && !길찾기.채집인접칸있음(위치)
                || !도달가능(주민위치, 위치, 채집거리)) continue;
            최고점수 = 점수;
            후보 = j;
        }
        return 후보;
    }

    int 적군탐색(int 팀, Vector2 위치, float 사거리, bool 전투유닛만 = false)
    {
        int 표적 = -1;
        float 거리 = 사거리 * 사거리;
        for (int i = 왕번호(1 - 팀); i < 왕번호(1 - 팀) + 진영칸; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 is not { 생존: true, 건물: false, 연습허수아비: false }
                || 전투유닛만 && 대상.종류 == 대상종류.주민) continue;
            float 후보 = (대상.위치 - 위치).sqrMagnitude;
            if (후보 >= 거리 || !AI시야안(대상.위치) || !표적보임(팀, 대상)) continue;
            거리 = 후보;
            표적 = i;
        }
        return 표적;
    }

    int 전술공격표적(전장대상 유닛, float 탐지거리, bool 폭탄사용가능 = false, bool 아처전술 = false, bool 가까운순 = false)
    {
        int 표적 = -1;
        int 우선순위 = int.MaxValue;
        float 최단거리 = float.PositiveInfinity;
        float 탐지거리제곱 = 탐지거리 * 탐지거리;
        for (int i = 0; i < 상태.대상들.Length; i++)
        {
            var 적 = 상태.대상들[i];
            if (적 is not { 생존: true, 연습허수아비: false } || 적.팀 != 1 - 유닛.팀) continue;
            float 거리 = (적.위치 - 유닛.위치).sqrMagnitude;
            if (거리 > 탐지거리제곱) continue;
            int 순위 = 가까운순 ? 0 : 적.종류 switch
            {
                대상종류.왕 or 대상종류.병사 => 아처전술 ? 아처전투표적순위(적) : 0,
                대상종류.아처타워 => 3,
                대상종류.회관 => 5,
                대상종류.주민 => 6,
                _ => 적.건물 ? 4 : int.MaxValue
            };
            if (순위 == int.MaxValue || 순위 > 우선순위 || 순위 == 우선순위 && 거리 >= 최단거리
                || !표적보임(유닛.팀, 적) || !공격가능(유닛, i) && !(폭탄사용가능 && 적.건물)) continue;
            우선순위 = 순위;
            최단거리 = 거리;
            표적 = i;
        }
        return 표적;
    }

    static bool AI폭탄사용가능(진영상태 진영)
    {
        if (진영.직업 != 왕직업.아처) return false;
        for (int 칸 = 0; 칸 < 왕직업목록.장착칸수; 칸++)
            if (진영.쿨다운[칸] <= 0 && 왕직업목록.장착스킬(진영, 칸) == 장착기술.폭탄화살) return true;
        return false;
    }

    void AI부대지휘(int 팀, 진영AI상태 판단, 전장대상 왕, Vector2 집결지, Vector2 수비지, int 침입자, bool 본진준비)
    {
        AI부대.Clear();
        AI수비대.Clear();
        AI원정대.Clear();
        int 시작 = 왕번호(팀) + 병사시작칸, 끝 = 왕번호(팀) + 진영칸;
        int 필요병력 = AI출격기준(상태.시간);
        // 부대 이동 명령은 4열 대형을 사용하므로 후반의 큰 부대는 집결 범위도 늘린다.
        float 집결반경 = Mathf.Max(5f, Mathf.CeilToInt(필요병력 / 4f) * .5f + 1.5f);
        float 집결반경제곱 = 집결반경 * 집결반경;
        판단.출격대.RemoveWhere(병사 => !병사.생존);
        if (판단.진격 && (판단.출격대.Count < 4 || 상태.시간 >= 판단.출격만료 || !판단.발견한적회관.HasValue))
        {
            판단.진격 = false;
            판단.출격대.Clear();
        }

        bool 출격준비 = !판단.진격 && 침입자 < 0 && 본진준비 && 왕.생존 && 판단.발견한적회관.HasValue
            && 상태.시간 >= 판단.다음출격 && 상태.시간 >= 첫진격준비시간;
        if (출격준비)
        {
            int 수비 = AI수비병력, 정예 = 0;
            for (int i = 시작; i < 끝; i++)
            {
                var 병사 = 상태.대상들[i];
                if (!AI지휘가능(판단, 병사)) continue;
                if (수비 > 0) { 수비--; continue; }
                if ((병사.위치 - 집결지).sqrMagnitude > 집결반경제곱) continue;
                AI원정대.Add(병사);
                if (병사.병과 != 병과.일반) 정예++;
            }
            if (AI원정대.Count >= 필요병력 && (정예 >= 2 || 상태.시간 >= 첫소규모진격준비시간 + 90f))
            {
                판단.진격 = 판단.첫진격완료 = true;
                판단.다음출격 = 상태.시간 + 120;
                판단.출격만료 = 상태.시간 + 90;
                판단.출격대.UnionWith(AI원정대);
                판단.부대목표 = 판단.발견한적회관.Value;
                부대이동지정(AI원정대, 제한(판단.부대목표));
            }
        }

        int 남은수비병력 = 침입자 < 0 ? AI수비병력 : 0;
        Vector2 목표 = 침입자 >= 0 ? 상태.대상들[침입자].위치 : 집결지;
        for (int i = 시작; i < 끝; i++)
        {
            var 병사 = 상태.대상들[i];
            if (!AI지휘가능(판단, 병사)) continue;
            if (판단.출격대.Contains(병사)) continue;
            if (남은수비병력 > 0)
            {
                남은수비병력--;
                if (AI이동명령필요(병사, 수비지, 4))
                    AI수비대.Add(병사);
                continue;
            }
            if (AI이동명령필요(병사, 목표, 침입자 >= 0 ? 9 : 집결반경제곱)) AI부대.Add(병사);
        }
        부대이동지정(AI수비대, 제한(수비지));
        부대이동지정(AI부대, 제한(목표), 지면명령: 침입자 < 0);
    }

    static int AI출격기준(float 시간)
    {
        if (시간 < AI후반시작) return 시간 >= 첫소규모진격준비시간 ? AI최소출격병력 : AI출격병력;
        int 지난분 = Mathf.FloorToInt((시간 - AI후반시작) / 60f);
        return Mathf.Min(병력제한 - AI수비병력, AI후반출격병력 + 지난분 * AI후반분당증원);
    }

    static bool AI이동명령필요(전장대상 유닛, Vector2 목표, float 허용거리제곱) =>
        ((유닛.이동중 ? 유닛.목적지 : 유닛.위치) - 목표).sqrMagnitude > 허용거리제곱;

    bool 원거리거리조절(전장대상 유닛, 전장대상 적, float 사거리, float 이동거리, float 안전거리비율)
    {
        if (적.건물 || 적.팀 == 유닛.팀 || 이동거리 <= 0) return false;
        Vector2 차이 = 유닛.위치 - 적.위치;
        float 거리 = 차이.magnitude;
        float 안전거리 = 사거리 * 안전거리비율;
        if (거리 >= 안전거리) return false;

        Vector2 방향 = 거리 > .01f ? 차이 / 거리 : -유닛.시선;
        if (방향 == Vector2.zero) 방향 = 유닛.팀 == 0 ? Vector2.left : Vector2.right;
        float 걸음 = Mathf.Min(이동거리, 안전거리 - 거리);
        var 옆 = new Vector2(-방향.y, 방향.x);
        Vector2 다음 = 유닛.위치;
        float 최선거리 = 차이.sqrMagnitude;
        후보확인(방향);
        후보확인((방향 + 옆).normalized);
        후보확인((방향 - 옆).normalized);
        if (다음 == 유닛.위치) return false;
        유닛위치갱신(유닛, 다음);
        return true;

        void 후보확인(Vector2 후보방향)
        {
            Vector2 후보 = 길찾기.방향이동(유닛.위치, 제한(유닛.위치 + 후보방향 * 걸음));
            float 거리제곱 = (후보 - 적.위치).sqrMagnitude;
            if (거리제곱 <= 최선거리) return;
            다음 = 후보;
            최선거리 = 거리제곱;
        }
    }

    Vector2 AI이동자리(Vector2 시작, Vector2 목표)
    {
        목표 = 길찾기.중심정렬(제한(목표));
        for (int 반경 = 0; 반경 <= 3; 반경++)
        for (int y = -반경; y <= 반경; y++)
        for (int x = -반경; x <= 반경; x++)
        {
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != 반경) continue;
            var 후보 = 목표 + new Vector2(x, y);
            if (제한(후보) == 후보 && 길찾기.통행가능(후보) && 도달가능(시작, 후보, .2f)) return 후보;
        }
        return 시작;
    }

    static bool AI지휘가능(진영AI상태 판단, 전장대상 병사) =>
        병사 is { 생존: true, 훈련소: < 0, 소환: 소환종류.없음, 연습허수아비: false }
        && !병사.속박중 && !판단.정찰중(병사);

    void AI왕지휘(int 팀, 진영AI상태 판단, 전장대상 왕, Vector2 후방, Vector2 집결지, int 침입자)
    {
        if (!왕.생존) { 판단.왕후퇴 = false; return; }
        if (왕.속박중 || 돌진들[팀] != null || 왕공격동작중(왕)) return;

        var 진영 = 상태.진영들[팀];
        var 직업 = 진영.직업;
        bool 장착 = 왕직업목록.장착가능(직업);
        bool 신규 = 왕직업목록.신규(직업);
        float 체력비율 = 왕.체력 / 왕.최대체력;
        if (체력비율 < .6f && 직업 == 왕직업.트린다미어) 시전(팀, 왕, 0, 왕.위치);
        else if (체력비율 < .7f && !장착 && !신규) 시전(팀, 왕, 2, 왕.위치);

        판단.왕후퇴 = 왕.체력 < 왕.최대체력 * (판단.왕후퇴 ? .6f : .3f);
        if (판단.왕후퇴)
        {
            if (장착) 장착스킬AI(팀, 왕, -1);
            if ((왕.위치 - 후방).sqrMagnitude > 1 || 왕.표적 >= 0)
                이동지정(왕, AI이동자리(왕.위치, 후방));
            return;
        }

        bool 원정중 = 판단.진격 && 침입자 < 0;
        int 표적 = 원정중 ? 전술공격표적(왕, 전장안개.시야반경, AI폭탄사용가능(진영)) : 적군탐색(팀, 왕.위치, 6);
        if (원정중 && 적인가(팀, 왕.표적)) 왕.표적 = -1;
        if (장착) 장착스킬AI(팀, 왕, 표적);
        else if (표적 >= 0)
        {
            var 위치 = 상태.대상들[표적].위치;
            if (신규)
                for (int 기술 = 0; 기술 < 4; 기술++) 시전(팀, 왕, 기술, 위치);
            else
            {
                시전(팀, 왕, 0, 위치);
                시전(팀, 왕, 3, 위치);
            }
        }
        if (돌진들[팀] != null || 왕.속박중 || 왕공격동작중(왕)) return;
        // 일부 스킬은 이전 이동과 표적을 해제한다. 스킬이 끝나면 걸어서 교전에 합류한다.
        if (공격가능(왕, 왕.표적) || 공격가능(왕, 표적))
        {
            if (!공격가능(왕, 왕.표적)) 왕.표적 = 표적;
            return;
        }
        왕.표적 = -1;

        Vector2 목적 = 침입자 >= 0 ? 상태.대상들[침입자].위치 : 집결지;
        if (원정중)
        {
            // 수비대나 새로 훈련된 병사 대신 실제로 출격한 병사와 전진한다.
            float 거리 = float.PositiveInfinity;
            for (int i = 왕번호(팀) + 병사시작칸; i < 왕번호(팀) + 진영칸; i++)
            {
                var 병사 = 상태.대상들[i];
                if (병사 == null || !판단.출격대.Contains(병사)) continue;
                float 후보 = (병사.위치 - 왕.위치).sqrMagnitude;
                if (후보 >= 거리) continue;
                거리 = 후보;
                목적 = 후보 <= 36
                    ? Vector2.MoveTowards(병사.위치, 판단.부대목표, 2.5f) : 병사.위치;
            }
        }
        var 이동지 = AI이동자리(왕.위치, 목적);
        if ((왕.위치 - 이동지).sqrMagnitude > 1)
        {
            if (AI이동명령필요(왕, 이동지, .25f)) 이동지정(왕, 이동지);
        }
        else if (왕.이동중)
            왕.이동중 = false;
    }
}
