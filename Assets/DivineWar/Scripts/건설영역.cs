using UnityEngine;

public sealed partial class 전장규칙
{
    public const int 깃발건설 = 12, 깃발제한 = 4;
    public const int 기본깃발한도 = 2, 기본울타리한도 = 12;
    public const int 깃발시작칸 = 시련시작칸 + 시련제한;
    public const float 건설반경 = 8;

    // 슬롯 수는 유지하고 경기 중 획득한 보상으로 실제 건설 한도만 늘린다.
    public static int 건설정원(전장상태 상태, int 팀, int 종류)
    {
        var 진영 = 상태.진영들[팀];
        if (종류 == 깃발건설) return 기본깃발한도 + Mathf.Clamp(진영.깃발한도증가, 0, 깃발제한 - 기본깃발한도);
        if (울타리건설인가(종류)) return 기본울타리한도 + Mathf.Clamp(진영.울타리한도증가, 0, 울타리제한 - 기본울타리한도);
        return 건설정원(종류);
    }

    public static bool 건설영역안(전장상태 상태, int 팀, Vector2 위치)
    {
        if (상태 == null || (uint)팀 >= 2 || !float.IsFinite(위치.x) || !float.IsFinite(위치.y)) return false;
        if (상태.대상들[회관번호(팀)] is { 생존: true } 회관 && (회관.위치 - 위치).sqrMagnitude <= 건설반경 * 건설반경) return true;
        for (int i = 깃발시작칸 + 팀 * 깃발제한; i < 깃발시작칸 + (팀 + 1) * 깃발제한; i++)
            if (상태.대상들[i] is { 생존: true, 종류: 대상종류.깃발, 완공대기: <= 0 } 깃발 && 깃발.팀 == 팀
                && (깃발.위치 - 위치).sqrMagnitude <= 건설반경 * 건설반경) return true;
        return false;
    }

    internal static int 회관피해감소율(전장상태 상태, int 팀)
    {
        if (상태.대상들[회관번호(팀)] is not { 생존: true }) return 0;
        int 완공건물 = 0;
        foreach (var 대상 in 상태.대상들)
            if (대상 is { 생존: true, 건물: true, 완공대기: <= 0 }
                && 대상.팀 == 팀 && 대상.종류 != 대상종류.회관 && ++완공건물 == 9) return 70;
        return 완공건물 >= 6 ? 50 : 완공건물 >= 3 ? 30 : 0;
    }

    // Shared by authoritative orders and the hover quote so the quoted head count matches eligibility.
    public static bool 훈련가능유닛(int 팀, 전장대상 학교, 전장대상 유닛)
    {
        if (학교 is not { 생존: true, 완공대기: <= 0 } || 학교.팀 != 팀
            || 학교.종류 != 대상종류.훈련소 && !전문훈련소(학교)) return false;
        return 유닛 is { 생존: true, 훈련소: < 0, 소환: 소환종류.없음, 병과: 병과.일반, 연습허수아비: false, 해골병사: false }
            && 유닛.팀 == 팀 && (전문훈련소(학교) ? 유닛.종류 == 대상종류.병사 : 유닛.종류 == 대상종류.주민);
    }

    public static bool 병과선택가능(전장대상 유닛, int 팀, 병과 종류)
        => 유닛 is { 생존: true, 종류: 대상종류.병사, 소환: 소환종류.없음, 연습허수아비: false } && 유닛.팀 == 팀 && 유닛.병과 == 종류;

    public static bool 주민선택가능(전장대상 유닛, int 팀)
        => 유닛 is { 생존: true, 종류: 대상종류.주민 } && 유닛.팀 == 팀;
}
