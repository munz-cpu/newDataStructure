using UnityEngine;

internal enum 고기종류 { 날돼지고기, 날닭고기, 익힌돼지고기, 익힌닭고기 }

static class 전장식량
{
    internal const int 종류수 = 4;
    internal const float 조리거리 = 2.5f, 회복반경 = 4f, 조리시간 = 3f;
    internal static readonly string[] 이름 = { "날돼지고기", "날닭고기", "익힌 돼지고기", "익힌 닭고기" };
    internal static readonly string[] 아이콘이름 = { "pork-raw", "chicken-raw", "pork-cooked", "chicken-cooked" };
    internal static readonly int[] 회복량 = { 16, 8, 40, 24 };

    internal static bool 날고기인가(int 종류) => 종류 is (int)고기종류.날돼지고기 or (int)고기종류.날닭고기;

    internal static string 조리시작(전장상태 상태, int 팀, int 종류)
    {
        var 진영 = 상태.진영들[팀];
        if (!날고기인가(종류) || 진영.고기[종류] <= 0) return "구울 날고기가 없습니다.";
        if (진영.조리중인고기 >= 0) return "모닥불에서 고기를 굽고 있습니다.";
        var 왕 = 상태.대상들[전장규칙.왕번호(팀)];
        var 모닥불 = 상태.대상들[전장규칙.모닥불번호(팀)];
        if (!왕.생존 || 모닥불 is not { 생존: true, 완공대기: <= 0 }
            || (왕.위치 - 모닥불.위치).sqrMagnitude > 조리거리 * 조리거리)
            return "완공된 아군 모닥불 가까이에서 고기를 구우세요.";

        진영.고기[종류]--;
        진영.조리중인고기 = 종류;
        진영.조리남은시간 = 조리시간;
        return null;
    }

    internal static (string 문장, bool 경고) 조리진행(전장상태 상태, int 팀, float 초)
    {
        var 진영 = 상태.진영들[팀];
        int 고기 = 진영.조리중인고기;
        if (고기 < 0) return default;
        var 모닥불 = 상태.대상들[전장규칙.모닥불번호(팀)];
        bool 완료가능 = 모닥불 is { 생존: true, 완공대기: <= 0 };
        if (완료가능 && (진영.조리남은시간 -= 초) > 0) return default;
        진영.조리중인고기 = -1;
        진영.조리남은시간 = 0;
        if (!완료가능)
        {
            진영.고기[고기]++;
            return ("모닥불이 꺼져 날고기가 반환됐습니다.", true);
        }
        int 익힌고기 = 고기 + (int)고기종류.익힌돼지고기;
        진영.고기[익힌고기]++;
        return ($"{이름[익힌고기]} +1", false);
    }

    internal static (string 문장, bool 경고) 사용(전장상태 상태, int 팀, int 종류)
    {
        var 진영 = 상태.진영들[팀];
        if ((uint)종류 >= 종류수 || 진영.고기[종류] <= 0) return ("사용할 고기가 없습니다.", true);
        var 왕 = 상태.대상들[전장규칙.왕번호(팀)];
        if (!왕.생존) return ("왕이 살아 있어야 고기를 사용할 수 있습니다.", true);

        int 회복인원 = 0;
        foreach (var 유닛 in 상태.대상들)
        {
            if (유닛 is not { 생존: true, 종류: 대상종류.주민 or 대상종류.병사 }
                || 유닛.체력 >= 유닛.최대체력 || (유닛.위치 - 왕.위치).sqrMagnitude > 회복반경 * 회복반경) continue;
            유닛.체력 = Mathf.Min(유닛.최대체력, 유닛.체력 + 회복량[종류]);
            회복인원++;
        }
        if (회복인원 == 0) return ("주변에 체력이 줄어든 주민이나 병사가 없습니다.", true);
        진영.고기[종류]--;
        진영.음식회복위치 = 왕.위치;
        진영.음식회복시간 = .48f;
        return ($"{이름[종류]} 사용 · {회복인원}명 체력 회복 (최대 {회복량[종류]})", false);
    }
}
