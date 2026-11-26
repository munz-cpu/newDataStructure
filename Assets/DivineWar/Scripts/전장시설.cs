using UnityEngine;

public enum 병과 { 일반, 브루저, 아처 }

public sealed partial class 전장규칙
{
    int 훈련예약인원(int 팀)
    {
        int 수 = 0;
        foreach (var 신청 in 훈련대기열[팀]) if (신청.주민.종류 == 대상종류.주민) 수++;
        return 수;
    }
    public const int 시설시작칸 = 모닥불시작칸 + 2;
    public const int 시설진영칸 = 24;
    public const int 브루저건설 = 7, 아처건설 = 8, 타워건설 = 9, 지뢰건설 = 10, 점프건설 = 11;
    public static bool 전문훈련소(전장대상 대상) => 대상 != null && 대상.종류 is 대상종류.브루저훈련소 or 대상종류.아처훈련소;
    public static bool 시설건설인가(int 번호) => 번호 >= 브루저건설 && 번호 <= 점프건설;
    public static bool 시설인가(전장대상 대상) => 대상 != null && 대상.종류 >= 대상종류.브루저훈련소 && 대상.종류 <= 대상종류.점프패드;
    public static int 시설시작(int 팀) => 시설시작칸 + 팀 * 시설진영칸;
    public static 대상종류 시설종류(int 번호) => 번호 switch {
        브루저건설 => 대상종류.브루저훈련소, 아처건설 => 대상종류.아처훈련소,
        타워건설 => 대상종류.아처타워, 지뢰건설 => 대상종류.지뢰, _ => 대상종류.점프패드
    };

    static int 빈시설칸(전장상태 상태, int 팀, int 종류)
    {
        int 시작 = 시설시작(팀) + (종류 switch { 7 => 0, 8 => 2, 9 => 4, 10 => 8, _ => 16 });
        int 수 = 건설정원(종류);
        for (int i = 시작; i < 시작 + 수; i++)
            if (상태.대상들[i] is not { 생존: true }) return i;
        return -1;
    }

    static 전장대상 타워재료(전장상태 상태, int 팀)
    {
        for (int i = 왕번호(팀) + 병사시작칸; i < 왕번호(팀) + 진영칸; i++)
            if (상태.대상들[i] is { 생존: true, 병과: 병과.아처, 훈련소: < 0, 소환: 소환종류.없음 } 아처) return 아처;
        return null;
    }

    internal static int 주둔아처수(전장상태 상태, int 팀)
    {
        int 수 = 0;
        for (int i = 왕번호(팀) + 병사시작칸; i < 왕번호(팀) + 진영칸; i++)
            if (상태.대상들[i] is { 타워주둔: true }) 수++;
        return 수;
    }

    void 타워주둔시작(전장대상 타워, 전장대상 아처)
    {
        타워.주둔아처 = 아처;
        아처.타워주둔 = true;
        아처.위치 = 타워.위치;
        이동지정(아처, 타워.위치);
        아처.이동중 = false;
        아처.공격순번++;
        기본공격대기들.RemoveAll(예약 => 예약.공격자 == 아처);
    }

    void 타워주둔종료(전장대상 타워, bool 반환)
    {
        var 아처 = 타워.주둔아처;
        타워.주둔아처 = null;
        아처.타워주둔 = false;
        if (반환)
        {
            int 번호 = System.Array.IndexOf(상태.대상들, 아처);
            길찾기.초기화(경로들[번호]);
        }
        else 아처.체력 = 0;
    }

    void 시설진행(int 번호, 전장대상 시설, float 초)
    {
        시설.완공대기 = Mathf.Max(0, 시설.완공대기 - 초);
        if (시설.완공대기 > 0) return;
        if (시설.종류 == 대상종류.아처타워 && 시설.공격대기 <= 0)
        {
            int 적 = 가까운적(시설, 7f, true);
            if (적 >= 0) { 기본공격시작(시설, 적, 28); 시설.공격대기 = 1f; }
        }
        if (시설.종류 == 대상종류.지뢰)
        {
            int 적 = 가까운적(시설, .85f, true);
            if (적 < 0) return;
            for (int i = 0; i < 전체칸; i++)
                if (상태.대상들[i] is { 생존: true, 건물: false } 유닛 && 유닛.팀 != 시설.팀
                    && Vector2.Distance(유닛.위치, 시설.위치) <= 2f) 피해(i, 75 * 범위피해배율, 시설.팀);
            지대추가(시설.팀, 지대종류.근접타격, 시설.위치, 2f, .35f);
            시설.체력 = 0;
        }
        if (시설.종류 != 대상종류.점프패드) return;
        for (int i = 0; i < 전체칸; i++)
        {
            var 유닛 = 상태.대상들[i];
            if (유닛 is not { 생존: true, 건물: false, 훈련소: < 0 } || 유닛.팀 != 시설.팀
                || 유닛.도약대기 > 0 || 유닛.속박중 || Vector2.Distance(유닛.위치, 시설.위치) > .65f) continue;
            var 방향 = 유닛.이동중 ? (유닛.목적지 - 유닛.위치).normalized : 유닛.시선.normalized;
            if (방향.sqrMagnitude < .1f) 방향 = 유닛.팀 == 0 ? Vector2.right : Vector2.left;
            // Shorten the leap until its landing fits the actual navigation grid.
            for (float 거리 = 4; 거리 >= 1; 거리 -= .5f)
            {
                var 착지 = 제한(유닛.위치 + 방향 * 거리);
                if (!길찾기.통행가능(착지) || 길찾기.지형?.도약이숲을가로지름(유닛.위치, 착지) == true) continue;
                유닛.도약출발 = 유닛.위치;
                유닛.위치 = 착지;
                유닛.도약시간 = .5f;
                유닛.도약대기 = 3;
                길찾기.초기화(경로들[i]);
                break;
            }
        }
    }
}
