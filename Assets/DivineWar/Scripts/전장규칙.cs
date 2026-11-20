using System;
using System.Collections.Generic;
using UnityEngine;

public enum 대상종류 { 왕, 회관, 제재소, 훈련소, 주민, 병사 }
public enum 명령종류 { 이동, 공격, 부대이동, 채집, 건설, 모집, 스킬, 방향이동 }
public enum 왕직업 { 수호자, 추적자, 예언자 }

[Serializable]
public sealed class 전장명령
{
    public 명령종류 종류;
    public Vector2 위치;
    public int 값;
    public int[] 부대;
}

[Serializable]
public sealed class 전장대상
{
    public int 팀;
    public 대상종류 종류;
    public Vector2 위치, 목적지;
    public float 체력, 최대체력, 공격대기, 효과시간, 방패시간, 재등장, 완공대기;
    public int 표적 = -1, 채집터 = -1;
    public bool 이동중, 왼쪽;
    public bool 생존 => 체력 > 0;
    public bool 건물 => 종류 is 대상종류.회관 or 대상종류.제재소 or 대상종류.훈련소;
}

[Serializable]
public sealed class 진영상태
{
    public int[] 재화 = new int[5];
    public float[] 쿨다운 = new float[4];
    public 왕직업 직업;
    public int 심부름;
    public string 알림;
    public float 알림시간, 제재시간, 모집대기, 스킬표시시간, 스킬반경;
    public Vector2 스킬위치;
    public bool 채집완료, 제재완료, 모집완료, 진격완료;
    public int 모집종류 = -1;
}

[Serializable]
public sealed class 전장상태
{
    public 전장대상[] 대상들;
    public 진영상태[] 진영들;
    public List<Vector2> 손님왕경로;
    public float 시간;
    public int 승자 = -1;
}

[Serializable]
public struct 자원지점
{
    public Transform 오브젝트;
    public int 재화번호;
}

public sealed class 전장규칙
{
    public const int 병력제한 = 16;
    public const int 주민제한 = 6;
    public const int 진영칸 = 28;
    public const int 전체칸 = 진영칸 * 2;
    public static readonly string[] 재화이름 = { "나무", "돌", "구리", "철", "미네랄" };
    public static readonly string[] 스킬이름 = { "징벌", "돌진", "회복", "신벌" };
    public static readonly int[][] 비용 =
    {
        new[] { 35, 15, 0, 0, 0 }, new[] { 60, 35, 0, 0, 0 },
        new[] { 25, 0, 0, 0, 0 }, new[] { 15, 8, 4, 3, 1 }
    };
    static readonly float[] 스킬시간 = { 5, 8, 12, 30 };

    readonly 자원지점[] 자원들;
    readonly 격자길찾기 길찾기;
    readonly 격자길찾기.이동경로[] 경로들 = new 격자길찾기.이동경로[전체칸];
    readonly (Vector2 방향, float 만료)[] 왕방향입력 = new (Vector2, float)[2];
    readonly System.Random 난수;
    float 적명령시간;
    int 적웨이브;
    public 전장상태 상태 { get; }
    public static int 왕번호(int 팀) => 팀 * 진영칸;
    public static int 회관번호(int 팀) => 왕번호(팀) + 1;

    public void 왕이동경로복사(int 팀, List<Vector2> 결과)
    {
        결과.Clear();
        int 번호 = 왕번호(팀);
        var 왕 = 상태.대상들[번호];
        if (상태.승자 < 0 && 왕.생존 && 왕.이동중 && 왕.표적 < 0)
            길찾기.남은경로추가(경로들[번호], 왕.위치, 왕.목적지, 결과);
    }

    public static (int 힘, int 민첩, int 지능, int 체력) 능력치(대상종류 종류, 왕직업 직업 = 왕직업.수호자)
    {
        if (종류 == 대상종류.주민) return (2, 4, 6, 65);
        if (종류 == 대상종류.병사) return (5, 5, 2, 105);
        return 직업 switch
        {
            왕직업.수호자 => (8, 4, 4, 240),
            왕직업.추적자 => (6, 9, 4, 175),
            _ => (4, 5, 9, 195)
        };
    }

    public static string 직업설명(왕직업 직업)
    {
        var (힘, 민첩, 지능, 체력) = 능력치(대상종류.왕, 직업);
        string 특기 = 직업 switch
        {
            왕직업.수호자 => "돌진 시 보호막",
            왕직업.추적자 => "빠른 이동과 긴 돌진",
            _ => "강한 회복과 넓은 신벌"
        };
        return $"힘 {힘}  ·  민첩 {민첩}  ·  지능 {지능}  ·  체력 {체력}   |   {특기}";
    }

    public 전장규칙(자원지점[] 자원들, 격자길찾기 길찾기, Vector2[] 거점들, 왕직업 청, 왕직업 적, int 시드)
    {
        this.자원들 = 자원들;
        this.길찾기 = 길찾기;
        for (int i = 0; i < 경로들.Length; i++) 경로들[i] = new 격자길찾기.이동경로();
        난수 = new System.Random(시드);
        상태 = new 전장상태
        {
            대상들 = new 전장대상[전체칸],
            진영들 = new[] { 새진영(청), 새진영(적) }
        };
        bool 북서쪽 = 난수.Next(2) == 0;
        int 첫번째 = 난수.Next(2);
        for (int 팀 = 0; 팀 < 2; 팀++)
        {
            int 쪽 = (팀 + 첫번째) % 2;
            Vector2 거점 = 길찾기.중심정렬(거점들[쪽 * 2 + (북서쪽 ? 쪽 : 1 - 쪽)]);
            float 안쪽 = -Mathf.Sign(거점.x);
            배치(회관번호(팀), 팀, 대상종류.회관, 거점, 1350);
            배치(왕번호(팀), 팀, 대상종류.왕, 거점 + new Vector2(안쪽 * 2, 0), 능력치(대상종류.왕, 상태.진영들[팀].직업).체력);
            배치(빈칸(상태, 팀, 대상종류.주민), 팀, 대상종류.주민, 거점 + new Vector2(안쪽, -1.8f), 능력치(대상종류.주민).체력);
        }
    }

    static 진영상태 새진영(왕직업 직업) => new()
    {
        직업 = 직업, 재화 = new[] { 125, 75, 16, 10, 5 }, 알림 = ""
    };

    전장대상 배치(int 번호, int 팀, 대상종류 종류, Vector2 위치, float 체력)
    {
        var 대상 = new 전장대상
        {
            팀 = 팀, 종류 = 종류, 위치 = 위치, 목적지 = 위치, 체력 = 체력, 최대체력 = 체력
        };
        상태.대상들[번호] = 대상;
        길찾기.초기화(경로들[번호]);
        if (대상.건물) 길찾기.건물반영(번호, 위치, true);
        return 대상;
    }

    static int 빈칸(전장상태 상태, int 팀, 대상종류 종류)
    {
        int 시작 = 왕번호(팀) + (종류 switch
        {
            대상종류.제재소 => 2, 대상종류.훈련소 => 4, 대상종류.주민 => 6, _ => 12
        });
        int 끝 = 시작 + (종류 == 대상종류.병사 ? 병력제한 : 종류 == 대상종류.주민 ? 주민제한 : 2);
        for (int i = 시작; i < 끝; i++)
            if (상태.대상들[i] == null || !상태.대상들[i].생존) return i;
        return -1;
    }

    public static int 수량(전장상태 상태, int 팀, 대상종류 종류)
    {
        int 합계 = 0;
        foreach (var 대상 in 상태.대상들)
            if (대상 != null && 대상.생존 && 대상.팀 == 팀 && 대상.종류 == 종류) 합계++;
        return 합계;
    }

    static Vector2 제한(Vector2 위치) => new(Mathf.Clamp(위치.x, -22.8f, 22.8f), Mathf.Clamp(위치.y, -12.7f, 12.7f));

    void 메시지(int 팀, string 문장)
    {
        상태.진영들[팀].알림 = 문장;
        상태.진영들[팀].알림시간 = 3.5f;
    }

    void 심부름진행(int 팀)
    {
        var 진영 = 상태.진영들[팀];
        while (진영.심부름 switch
        {
            0 => 진영.채집완료, 1 => 진영.제재완료, 2 => 진영.모집완료, 3 => 진영.진격완료, _ => false
        })
        {
            진영.심부름++;
            진영.재화[0] += 20;
            진영.재화[1] += 10;
            진영.재화[4] += 2;
            메시지(팀, "심부름 완료   +20 나무  +10 돌  +2 미네랄");
        }
    }

    static bool 지불가능(int[] 재화, int 항목)
    {
        for (int i = 0; i < 재화.Length; i++)
            if (재화[i] < 비용[항목][i]) return false;
        return true;
    }

    void 비용차감(int 팀, int 항목)
    {
        var 재화 = 상태.진영들[팀].재화;
        for (int i = 0; i < 재화.Length; i++) 재화[i] -= 비용[항목][i];
    }

    bool 지불(int 팀, int 항목)
    {
        if (!지불가능(상태.진영들[팀].재화, 항목)) { 메시지(팀, "재화가 부족합니다."); return false; }
        비용차감(팀, 항목);
        return true;
    }

    public void 명령(int 팀, 전장명령 내용)
    {
        if (상태.승자 >= 0 || 내용 == null || 팀 < 0 || 팀 > 1) return;
        if (!float.IsFinite(내용.위치.x) || !float.IsFinite(내용.위치.y)) return;
        var 위치 = 제한(내용.위치);
        var 왕 = 상태.대상들[왕번호(팀)];
        switch (내용.종류)
        {
            case 명령종류.이동:
                if (왕.생존)
                {
                    왕방향입력[팀] = default;
                    이동지정(왕, 위치);
                }
                break;
            case 명령종류.방향이동:
                방향이동지정(팀, 왕, 내용.위치);
                break;
            case 명령종류.공격:
                if (왕.생존 && 적인가(팀, 내용.값))
                {
                    왕방향입력[팀] = default;
                    이동지정(왕, 상태.대상들[내용.값].위치);
                    왕.표적 = 내용.값;
                }
                break;
            case 명령종류.채집:
                if (내용.값 < 0 || 내용.값 >= 자원들.Length) return;
                foreach (var 주민 in 내부대(팀, 내용.부대))
                {
                    if (주민.종류 != 대상종류.주민) continue;
                    이동지정(주민, 자원들[내용.값].오브젝트.position);
                    주민.채집터 = 내용.값;
                }
                break;
            case 명령종류.부대이동:
                int 대형 = 0;
                foreach (var 유닛 in 내부대(팀, 내용.부대))
                {
                    var 간격 = new Vector2((대형 % 4 - 1.5f) * .58f, -(대형 / 4) * .48f);
                    이동지정(유닛, 제한(위치 + 간격));
                    대형++;
                }
                break;
            case 명령종류.건설:
                건설(팀, 내용.값, 위치);
                break;
            case 명령종류.모집:
                모집(팀, 내용.값);
                break;
            case 명령종류.스킬:
                if (왕.생존) 시전(팀, 왕, 내용.값, 위치);
                break;
        }
    }

    IEnumerable<전장대상> 내부대(int 팀, int[] 번호들)
    {
        if (번호들 == null || 번호들.Length > 병력제한 + 주민제한) yield break;
        foreach (int 번호 in 번호들)
        {
            if (번호 < 0 || 번호 >= 전체칸) continue;
            var 유닛 = 상태.대상들[번호];
            if (유닛 != null && 유닛.생존 && 유닛.팀 == 팀
                && 유닛.종류 is 대상종류.주민 or 대상종류.병사) yield return 유닛;
        }
    }

    static void 이동지정(전장대상 유닛, Vector2 위치)
    {
        유닛.목적지 = 위치;
        유닛.표적 = -1;
        유닛.채집터 = -1;
        유닛.이동중 = true;
    }

    void 방향이동지정(int 팀, 전장대상 왕, Vector2 방향)
    {
        if (!왕.생존 || Mathf.Abs(방향.x) > 1 || Mathf.Abs(방향.y) > 1) return;
        if (방향 == Vector2.zero)
        {
            왕방향입력[팀] = default;
            return;
        }
        if (왕방향입력[팀].만료 <= 상태.시간)
        {
            왕.이동중 = false;
            왕.표적 = -1;
            길찾기.초기화(경로들[왕번호(팀)]);
        }
        // 입력 갱신이 끊겨도 왕이 계속 움직이지 않게 한다.
        왕방향입력[팀] = (방향.normalized, 상태.시간 + .36f);
    }

    void 건설(int 팀, int 종류, Vector2 위치)
    {
        if (종류 < 0 || 종류 > 1) return;
        위치 = 길찾기.중심정렬(위치);
        var 건물 = (대상종류)(종류 + (int)대상종류.제재소);
        string 오류 = 건설오류(상태, 길찾기, 자원들, 팀, 종류, 위치, out int 칸);
        if (오류 != null) { 메시지(팀, 오류); return; }
        비용차감(팀, 종류);
        배치(칸, 팀, 건물, 위치, 종류 == 0 ? 210 : 320).완공대기 = 6;
    }

    internal static bool 건설가능(전장상태 상태, 격자길찾기 길찾기, 자원지점[] 자원들,
        int 팀, int 종류, Vector2 위치, out Vector2 배치위치)
    {
        배치위치 = 길찾기.중심정렬(제한(위치));
        return 상태.승자 < 0 && 건설오류(상태, 길찾기, 자원들, 팀, 종류, 배치위치, out _) == null;
    }

    static string 건설오류(전장상태 상태, 격자길찾기 길찾기, 자원지점[] 자원들,
        int 팀, int 종류, Vector2 위치, out int 칸)
    {
        칸 = 빈칸(상태, 팀, (대상종류)(종류 + (int)대상종류.제재소));
        var 회관 = 상태.대상들[회관번호(팀)];
        if (칸 < 0) return "이 건물은 최대 2채입니다.";
        if (Vector2.Distance(위치, 회관.위치) > 8 || !길찾기.정적통행가능(위치))
            return "회관 반경 8 안의 땅에 배치하세요.";
        foreach (var 대상 in 상태.대상들)
            if (대상 != null && 대상.생존 && 대상.건물 && Vector2.Distance(대상.위치, 위치) < 2.15f)
                return "다른 건물과 간격이 필요합니다.";
        foreach (var 지점 in 자원들)
            if (Vector2.Distance(지점.오브젝트.position, 위치) < 1.55f)
                return "자원 지점과 간격이 필요합니다.";
        return 지불가능(상태.진영들[팀].재화, 종류) ? null : "재화가 부족합니다.";
    }

    void 모집(int 팀, int 종류)
    {
        if (종류 is < 0 or > 1) return;
        var 진영 = 상태.진영들[팀];
        var 유닛 = 종류 == 0 ? 대상종류.주민 : 대상종류.병사;
        if (진영.모집종류 >= 0) { 메시지(팀, "모집이 진행 중입니다."); return; }
        if (빈칸(상태, 팀, 유닛) < 0) { 메시지(팀, "정원에 도달했습니다."); return; }
        if (종류 == 1 && 완성훈련소(팀) == null) { 메시지(팀, "완성된 훈련소가 필요합니다."); return; }
        if (!지불(팀, 종류 + 2)) return;
        진영.모집종류 = 종류;
        진영.모집대기 = 종류 == 0 ? 4 : 6;
    }

    전장대상 완성훈련소(int 팀)
    {
        for (int i = 왕번호(팀) + 4; i <= 왕번호(팀) + 5; i++)
        {
            var 건물 = 상태.대상들[i];
            if (건물 != null && 건물.생존 && 건물.완공대기 <= 0) return 건물;
        }
        return null;
    }

    void 시전(int 팀, 전장대상 왕, int 번호, Vector2 위치)
    {
        if (번호 < 0 || 번호 >= 스킬시간.Length) return;
        var 진영 = 상태.진영들[팀];
        if (진영.쿨다운[번호] > 0) return;
        var 능력 = 능력치(대상종류.왕, 진영.직업);
        if (번호 == 1)
        {
            float 거리 = 진영.직업 == 왕직업.추적자 ? 6 : 4;
            var 경로 = 경로들[왕번호(팀)];
            길찾기.초기화(경로);
            길찾기.이동(경로, 왕.위치, 위치, 거리, out 왕.위치);
            길찾기.초기화(경로);
            이동지정(왕, 왕.위치);
            왕.이동중 = false;
            if (진영.직업 == 왕직업.수호자) 왕.방패시간 = 3;
            진영.스킬위치 = 왕.위치;
            진영.스킬반경 = 1.25f;
        }
        else if (번호 == 2)
        {
            진영.스킬위치 = 왕.위치;
            진영.스킬반경 = 4;
            foreach (var 대상 in 상태.대상들)
                if (대상 != null && 대상.생존 && !대상.건물 && 대상.팀 == 팀 && Vector2.Distance(대상.위치, 왕.위치) < 4)
                    대상.체력 = Mathf.Min(대상.최대체력, 대상.체력 + 15 + 능력.지능 * 5);
        }
        else
        {
            if (Vector2.Distance(위치, 왕.위치) > (번호 == 0 ? 5 : 7))
            { 메시지(팀, "스킬 사거리 밖입니다."); return; }
            float 반경 = 번호 == 0 ? 1.7f : 2.58f + 능력.지능 * .18f;
            진영.스킬위치 = 위치;
            진영.스킬반경 = 반경;
            for (int i = 0; i < 전체칸; i++)
                if (적인가(팀, i) && Vector2.Distance(상태.대상들[i].위치, 위치) < 반경)
                    피해(i, 번호 == 0 ? 38 : 88, 팀);
        }
        왕.효과시간 = .45f;
        진영.스킬표시시간 = .48f;
        진영.쿨다운[번호] = 스킬시간[번호];
    }

    bool 적인가(int 팀, int 번호)
    {
        if (번호 < 0 || 번호 >= 전체칸) return false;
        var 대상 = 상태.대상들[번호];
        return 대상 != null && 대상.생존 && 대상.팀 != 팀;
    }

    void 피해(int 번호, float 피해량, int 공격팀)
    {
        var 대상 = 상태.대상들[번호];
        if (!대상.생존 || 상태.승자 >= 0) return;
        float 방어 = 대상.방패시간 > 0 ? .38f : 대상.종류 == 대상종류.회관 ? .63f : 1;
        대상.체력 = Mathf.Max(0, 대상.체력 - 피해량 * 방어);
        대상.효과시간 = .16f;
        if (대상.생존) return;
        if (대상.건물) 길찾기.건물반영(번호, 대상.위치, false);
        if (대상.종류 == 대상종류.회관) 상태.승자 = 공격팀;
        if (대상.종류 == 대상종류.왕)
        {
            왕방향입력[대상.팀] = default;
            대상.재등장 = 14;
        }
    }

    public void 진행(float 초, bool 혼자)
    {
        if (상태.승자 >= 0) return;
        상태.시간 += 초;
        for (int 팀 = 0; 팀 < 2; 팀++) 경제(팀, 초);
        for (int i = 0; i < 전체칸 && 상태.승자 < 0; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null) continue;
            if (!대상.생존)
            {
                if (대상.종류 == 대상종류.왕 && (대상.재등장 -= 초) <= 0)
                {
                    var 회관 = 상태.대상들[회관번호(대상.팀)];
                    배치(i, 대상.팀, 대상종류.왕, 회관.위치 + Vector2.down * 1.1f, 대상.최대체력);
                }
                continue;
            }
            대상.공격대기 -= 초;
            대상.효과시간 -= 초;
            대상.방패시간 -= 초;
            if (대상.건물)
            {
                if (대상.종류 == 대상종류.회관 && 대상.공격대기 <= 0)
                {
                    int 침입자 = 가까운적(대상, 4.5f, true);
                    if (침입자 >= 0) { 피해(침입자, 25, 대상.팀); 대상.공격대기 = 1.4f; }
                }
                continue;
            }
            var 진영 = 상태.진영들[대상.팀];
            if (!진영.진격완료 && 대상.종류 == 대상종류.병사
                && Vector2.Distance(대상.위치, 상태.대상들[회관번호(1 - 대상.팀)].위치) < 8)
                진영.진격완료 = true;
            유닛진행(i, 대상, 초);
        }
        if (혼자 && 상태.승자 < 0) 적진행(초);
    }

    void 경제(int 팀, float 초)
    {
        var 진영 = 상태.진영들[팀];
        진영.스킬표시시간 -= 초;
        진영.알림시간 -= 초;
        for (int i = 0; i < 4; i++) 진영.쿨다운[i] = Mathf.Max(0, 진영.쿨다운[i] - 초);
        for (int i = 왕번호(팀) + 2; i < 왕번호(팀) + 6; i++)
        {
            var 건물 = 상태.대상들[i];
            if (건물 == null || !건물.생존 || 건물.완공대기 <= 0) continue;
            건물.완공대기 -= 초;
            if (건물.완공대기 <= 0 && 건물.종류 == 대상종류.제재소) 진영.제재완료 = true;
        }
        심부름진행(팀);
        if ((진영.제재시간 -= 초) <= 0)
        {
            진영.제재시간 = 5;
            진영.재화[0] += 2;
            진영.재화[1] += 1;
            for (int i = 왕번호(팀) + 2; i <= 왕번호(팀) + 3; i++)
            {
                var 제재소 = 상태.대상들[i];
                if (제재소 != null && 제재소.생존 && 제재소.완공대기 <= 0) 진영.재화[0] += 7;
            }
            if (Mathf.FloorToInt(상태.시간 / 5) % 3 == 0)
                for (int i = 2; i < 진영.재화.Length; i++) 진영.재화[i]++;
        }
        if (진영.모집종류 < 0 || (진영.모집대기 -= 초) > 0) return;
        int 종류 = 진영.모집종류;
        진영.모집종류 = -1;
        var 출구 = 종류 == 0 ? 상태.대상들[회관번호(팀)] : 완성훈련소(팀);
        if (출구 == null)
        {
            for (int i = 0; i < 진영.재화.Length; i++) 진영.재화[i] += 비용[종류 + 2][i];
            메시지(팀, "훈련소가 파괴되어 병사 모집 비용을 돌려받았습니다.");
            return;
        }
        var 유닛 = 종류 == 0 ? 대상종류.주민 : 대상종류.병사;
        int 칸 = 빈칸(상태, 팀, 유닛);
        if (칸 < 0) return;
        var 위치 = 출구.위치 + new Vector2((float)(난수.NextDouble() - .5) * 2.6f, -1.55f);
        배치(칸, 팀, 유닛, 제한(위치), 능력치(유닛).체력);
        if (종류 == 1) 진영.모집완료 = true;
    }

    Vector2 왕방향이동(Vector2 지금, Vector2 방향, float 거리)
    {
        var 다음 = 길찾기.방향이동(지금, 제한(지금 + 방향 * 거리));
        var 이동 = 다음 - 지금;
        if (방향.x != 0 && 방향.y != 0 && 이동 != Vector2.zero && (이동.x == 0 || 이동.y == 0))
        {
            // 한 축이 막히면 남은 이동 거리를 통행 가능한 축에 쓴다.
            var 목표 = 제한(지금 + 이동.normalized * 거리);
            다음 = 길찾기.방향이동(다음, 목표);
        }
        return 다음;
    }

    void 유닛진행(int 번호, 전장대상 유닛, float 초)
    {
        Vector2 목적 = 유닛.목적지;
        var 능력 = 능력치(유닛.종류, 상태.진영들[유닛.팀].직업);
        float 이동거리 = (유닛.종류 switch
        {
            대상종류.왕 => 3.18f,
            대상종류.주민 => 1.98f,
            _ => 2.35f
        } + 능력.민첩 * .18f) * 초;
        if (유닛.종류 == 대상종류.왕 && 왕방향입력[유닛.팀].만료 > 상태.시간)
        {
            유닛위치갱신(유닛, 왕방향이동(유닛.위치, 왕방향입력[유닛.팀].방향, 이동거리));
            return;
        }
        if (유닛.종류 == 대상종류.병사) 유닛.표적 = 가까운적(유닛, 4.1f);
        if (적인가(유닛.팀, 유닛.표적))
        {
            var 적 = 상태.대상들[유닛.표적];
            목적 = 적.위치;
            if (Vector2.Distance(유닛.위치, 목적) <= (적.건물 ? 1.45f : .95f))
            {
                if (유닛.공격대기 <= 0)
                {
                    float 힘 = (유닛.종류 == 대상종류.왕 ? 8 : 3) + 2 * 능력.힘;
                    피해(유닛.표적, 힘, 유닛.팀);
                    유닛.공격대기 = 유닛.종류 == 대상종류.왕 ? .65f : .93f;
                }
                return;
            }
        }
        else
        {
            유닛.표적 = -1;
            if (유닛.채집터 >= 0)
            {
                var 지점 = 자원들[유닛.채집터];
                목적 = 지점.오브젝트.position;
                if (Vector2.Distance(유닛.위치, 목적) < 1.38f)
                {
                    if (유닛.공격대기 <= 0)
                    {
                        int 재화 = 지점.재화번호;
                        상태.진영들[유닛.팀].재화[재화] += 재화 < 2 ? 6 : 3;
                        유닛.공격대기 = 3.6f - 능력.지능 * .1f;
                        상태.진영들[유닛.팀].채집완료 = true;
                    }
                    return;
                }
            }
            else if (!유닛.이동중) return;
        }
        if (길찾기.이동(경로들[번호], 유닛.위치, 목적, 이동거리, out Vector2 다음))
            유닛.이동중 = false;
        유닛위치갱신(유닛, 다음);
    }

    static void 유닛위치갱신(전장대상 유닛, Vector2 다음)
    {
        if (Mathf.Abs(다음.x - 유닛.위치.x) > .005f) 유닛.왼쪽 = 다음.x < 유닛.위치.x;
        유닛.위치 = 다음;
    }

    int 가까운적(전장대상 유닛, float 사거리, bool 건물제외 = false)
    {
        int 찾음 = -1;
        float 거리 = 사거리 * 사거리;
        for (int i = 왕번호(1 - 유닛.팀); i < 왕번호(1 - 유닛.팀) + 진영칸; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null || !대상.생존 || (건물제외 && 대상.건물)) continue;
            float 후보 = (대상.위치 - 유닛.위치).sqrMagnitude;
            if (후보 >= 거리) continue;
            거리 = 후보;
            찾음 = i;
        }
        return 찾음;
    }

    void 적진행(float 초)
    {
        if ((적명령시간 -= 초) > 0) return;
        적명령시간 = 2.5f;
        const int 팀 = 1;
        var 회관 = 상태.대상들[회관번호(팀)];
        float 방향 = -Mathf.Sign(회관.위치.x);
        if (수량(상태, 팀, 대상종류.제재소) == 0) 건설(팀, 0, 회관.위치 + new Vector2(방향 * 2.9f, 1.8f));
        if (수량(상태, 팀, 대상종류.훈련소) == 0) 건설(팀, 1, 회관.위치 + new Vector2(방향 * 3, -1.8f));
        int 주민수 = 수량(상태, 팀, 대상종류.주민);
        if (주민수 < 4) 모집(팀, 0);
        else 모집(팀, 1);
        for (int i = 왕번호(팀) + 6; i < 왕번호(팀) + 12; i++)
        {
            var 주민 = 상태.대상들[i];
            if (주민 == null || !주민.생존 || 주민.채집터 >= 0) continue;
            int 재화 = (i - 왕번호(팀) - 6) % 5;
            int 가장가까움 = -1;
            float 거리 = float.MaxValue;
            for (int j = 0; j < 자원들.Length; j++)
            {
                if (자원들[j].재화번호 != 재화) continue;
                float 후보 = Vector2.Distance(회관.위치, 자원들[j].오브젝트.position);
                if (후보 >= 거리) continue;
                가장가까움 = j;
                거리 = 후보;
            }
            if (가장가까움 >= 0) 주민.채집터 = 가장가까움;
        }
        var 왕 = 상태.대상들[왕번호(팀)];
        int 표적 = 가까운적(왕, 6);
        var 목적 = 상태.대상들[회관번호(0)].위치;
        bool 공격 = 상태.시간 >= 205 + 적웨이브 * 76 && 수량(상태, 팀, 대상종류.병사) >= 4;
        if (공격)
        {
            적웨이브++;
            for (int i = 왕번호(팀) + 12; i < 전체칸; i++)
            {
                var 병사 = 상태.대상들[i];
                if (병사 != null && 병사.생존) 이동지정(병사, 제한(목적 + new Vector2(i % 4 * .4f, i % 3 * .4f)));
            }
        }
        if (!왕.생존) return;
        if (왕.체력 < 왕.최대체력 * .7f) 시전(팀, 왕, 2, 왕.위치);
        if (표적 >= 0)
        {
            왕.표적 = 표적;
            var 위치 = 상태.대상들[표적].위치;
            시전(팀, 왕, 0, 위치);
            시전(팀, 왕, 3, 위치);
        }
        else if (적웨이브 > 0 && 수량(상태, 팀, 대상종류.병사) >= 3)
            이동지정(왕, 목적);
    }
}

