using System;
using System.Collections.Generic;
using UnityEngine;

public enum 대상종류 { 왕, 회관, 제재소, 훈련소, 주민, 병사, 나무울타리, 돌울타리, 모닥불, 브루저훈련소, 아처훈련소, 아처타워, 지뢰, 점프패드, 시련보스, 깃발 }
public enum 명령종류 { 이동, 공격, 부대이동, 채집, 건설, 모집, 스킬, 방향이동, 왕채집, 주민추첨, 사냥, 고기굽기, 고기사용, 주민훈련, 시련활성화 }
public enum 왕직업 { 수호자, 추적자, 예언자, 트린다미어, 소라카, 애쉬, 아지르, 이블린, 그라가스, 하이머딩거, 헤카림 = 11, 아이번, 지휘관, 아처, 검사, 암살자 }

[Serializable]
public sealed class 전장명령
{
    public 명령종류 종류;
    public Vector2 위치;
    public int 값;
    public int 주민모집인원;
    public int[] 부대;
    public bool 세로;
}

[Serializable]
public sealed class 전장대상
{
    public int 팀;
    public 대상종류 종류;
    public Vector2 위치, 목적지;
    public float 체력, 최대체력, 공격대기, 효과시간, 방패시간, 재등장, 완공대기;
    public float 기절, 속박, 침묵, 공포, 매혹, 둔화시간, 둔화량, 약화;
    public float 불사, 무적, 보호량, 보호시간, 강화시간, 가속, 돌격, 공격강화;
    public float 패시브대기, 비전투, 저주남음, 저주성숙, 연속시간, 뿌리대기, 소환남음;
    public int 저주대상 = -1, 연속횟수, 소환공격수;
    public Vector2 시선 = Vector2.right, 제어원점;
    public bool 은신, 장막안, 폭탄화살준비;
    public float 가드, 치유화살남음;
    internal float 사기남음, 복수남음, 복수오라남음, 호위회복남음, 분노남음, 발악남음, 사거리남음;
    internal float 멀티샷남음, 정지시간, 수혈남음, 위장남음, 다음출혈;
    internal int 출혈횟수, 연속타수, 강타칸 = -1;
    internal 전장대상 출혈시전자;
    internal Vector2 이전집중위치;
    internal float 여왕남음, 여왕공속, 독화살남음, 트릭샷남음;
    public 소환종류 소환;
    [NonSerialized] public 전장대상 저주참조, 뿌리참조;
    public bool 속박중 => 기절 > 0 || 속박 > 0 || 공포 > 0 || 매혹 > 0;
    public int 표적 = -1, 채집터 = -1;
    internal int 재생대기채집터 = -1;
    public int 공격순번;
    public float 공격시각, 왕공격동작길이, 왕공격반경;
    public Vector2 공격방향;
    public 장착기술 왕기술동작;
    public float 왕기술시각;
    public int 사냥표적;
    public int 훈련소 = -1;
    public int 훈련대기수;
    public float 훈련잔여합계;
    public 병과 병과;
    public float 훈련남음, 도약시간, 도약대기;
    public Vector2 도약출발;
    public int 힘, 민첩, 지능;
    public bool 이동중, 왼쪽, 세로, 해골병사;
    internal bool 대형복귀, 지정공격;
    public bool 생존 => 체력 > 0;
    public bool 울타리 => 종류 is 대상종류.나무울타리 or 대상종류.돌울타리;
    public bool 함정 => 종류 is 대상종류.지뢰 or 대상종류.점프패드;
    public bool 건물 => 울타리 || 종류 is 대상종류.회관 or 대상종류.제재소 or 대상종류.훈련소 or 대상종류.모닥불 or 대상종류.깃발 || 전장규칙.시설인가(this);
}

[Serializable]
public sealed class 진영상태
{
    const float 알림표시시간 = 3.5f;
    public int[] 재화 = new int[5];
    public int[] 고기 = new int[전장식량.종류수];
    public int 조리중인고기 = -1;
    public float 조리남은시간, 음식회복시간;
    public Vector2 음식회복위치;
    public int[] 자원별채집횟수;
    public float[] 쿨다운 = new float[4];
    public 왕기술효과[] 기술효과;
    public 왕직업 직업;
    public int 장착구성 = 왕직업목록.기본장착;
    public int 심부름;
    public string 알림;
    public int 알림번호;
    public bool 경고알림;
    public float 알림시간, 제재시간, 모집대기, 스킬표시시간, 스킬반경;
    public Vector2 스킬위치;
    public bool 채집완료, 제재완료, 모집완료, 진격완료, 스킬왕위치표시;
    public int 모집종류 = -1;
    public int 주민투자, 주민모집인원, 주민추첨결과번호, 주민추첨인원;
    public string 주민추첨결과;
    public int 훈련대기인원;
    public int 시련유물;
    public int 깃발한도증가, 울타리한도증가;
    public float 훈련남은시간;
    public float 스태미나 = 전장규칙.최대스태미나;
    internal float 스태미나회복대기;
    internal bool 스태미나고갈;

    internal void 알림보내기(string 문장, bool 경고 = false)
    {
        if (문장 == null) return;
        알림 = 문장;
        알림번호++;
        경고알림 = 경고;
        알림시간 = 알림표시시간;
    }
}

[Serializable]
public sealed class 전장상태
{
    public 전장대상[] 대상들;
    public 짐승상태[] 짐승들;
    public 시련상태[] 시련들 = Array.Empty<시련상태>();
    public 진영상태[] 진영들;
    public int[] 자원잔량;
    public float[] 자원재생완료시각;
    public List<Vector2> 청록왕경로, 적색왕경로;
    public List<스킬지대> 지대들 = new();
    public float 시간;
    public int 승자 = -1;
}

[Serializable]
public struct 자원지점
{
    public Transform 오브젝트;
    public int 재화번호;
    [Min(0)] public int 최대수량;
    [NonSerialized] public bool 숲나무;
    [NonSerialized] public Vector3 숲위치;

    public Vector3 위치 => 숲나무 ? 숲위치 : 오브젝트.position;
    public int 채집한도
    {
        get
        {
            int 기본한도 = 최대수량 > 0 ? 최대수량 : 숲나무 ? 24 : 재화번호 < 2 ? 60 : 36;
            return (int)Math.Min(기본한도 * 3L, int.MaxValue);
        }
    }
}

public sealed partial class 전장규칙
{
    public const float 최대스태미나 = 100f;
    const float 달리기배율 = 1.45f, 초당스태미나소모 = 22f, 초당스태미나회복 = 26f;
    const float 스태미나회복지연 = .75f, 달리기재개스태미나 = 20f;
    public const int 병력제한 = 48;
    public const int 주민제한 = 24;
    public const int 병사시작칸 = 6 + 주민제한;
    public const int 진영칸 = 병사시작칸 + 병력제한;
    public const float 주민훈련시간 = 6f;
    public const int 울타리시작칸 = 진영칸 * 2;
    public const int 울타리제한 = 16;
    internal const int 모닥불시작칸 = 울타리시작칸 + 울타리제한 * 2;
    public const int 전체칸 = 깃발시작칸 + 깃발제한 * 2;
    internal const int 나무울타리건설 = 4, 돌울타리건설 = 5, 모닥불건설 = 6;
    internal const float 울타리건설시간 = 3f, 모닥불건설시간 = 3f;
    public const float 채집거리 = 1.38f;
    const float 연속채집반경 = 6f;
    public const float 자원고갈시간 = 12f, 자원재생시간 = 4f;
    internal const float 주민추첨시간 = 1.4f;
    const float 대형간격 = 1f;
    const float 병사간격 = .8f;

    internal readonly struct 주민투자단계
    {
        public readonly int 나무, 돌, 최소보정, 최대보정;

        public 주민투자단계(int 나무, int 돌, int 최소보정, int 최대보정)
        {
            this.나무 = 나무;
            this.돌 = 돌;
            this.최소보정 = 최소보정;
            this.최대보정 = 최대보정;
        }

        public (int 나무, int 돌) 총비용(int 인원) => (나무 * 인원, 돌 * 인원);
    }

    static readonly 주민투자단계[] 주민투자단계들 =
    {
        // 멀티플레이 명령과 진행 중인 추첨은 배열 인덱스를 사용한다.
        new(25, 0, -1, 1),
        new(50, 10, 0, 2),
        new(85, 25, 1, 3),
        new(15, 0, -2, 1),
        new(38, 4, -1, 2),
        new(67, 17, 0, 3),
        new(110, 35, 1, 4)
    };
    internal static IReadOnlyList<주민투자단계> 주민투자목록 => 주민투자단계들;
    public static readonly string[] 재화이름 = { "나무", "돌", "구리", "철", "미네랄" };
    public static readonly string[] 스킬이름 = { "징벌", "돌진", "회복", "신벌" };
    public static readonly int[][] 비용 =
    {
        new[] { 35, 15, 0, 0, 0 }, new[] { 60, 35, 0, 0, 0 },
        new[] { 25, 0, 0, 0, 0 }, new[] { 15, 8, 4, 3, 1 },
        new[] { 8, 0, 0, 0, 0 }, new[] { 0, 10, 0, 0, 0 },
        new[] { 20, 8, 0, 0, 0 },
        new[] { 65, 30, 0, 0, 0 }, new[] { 65, 30, 0, 0, 0 },
        new[] { 80, 0, 0, 0, 0 }, new[] { 15, 5, 3, 0, 0 }, new[] { 25, 10, 0, 2, 0 }, new[] { 35, 10, 0, 0, 0 }
    };
    static readonly float[] 스킬시간 = { 5, 8, 12, 30 };
    public static float 스킬쿨다운길이(int 번호) => 스킬시간[번호];
    public static float 스킬쿨다운길이(왕직업 직업, int 번호) => 왕직업목록.신규(직업) ? 왕직업목록.스킬(직업, 번호).쿨다운 : 스킬시간[번호];

    readonly 자원지점[] 자원들;
    readonly List<int> 고갈자원들 = new();
    readonly List<Vector2> 도달확인경로 = new();
    readonly 격자길찾기 길찾기;
    readonly 격자길찾기.이동경로[] 경로들 = new 격자길찾기.이동경로[전체칸];
    readonly (Vector2 방향, float 만료, bool 달리기)[] 왕방향입력 = new (Vector2, float, bool)[2];
    readonly System.Random 난수;
    readonly 전장짐승 짐승들;
    // 실제 주민/건물 참조를 보관해 사망 후 같은 슬롯에 생긴 새 대상을 훈련하지 않는다.
    sealed class 훈련신청
    {
        public int 주민번호, 훈련소번호;
        public 전장대상 주민, 건물;
        public int 비용산정병력;
        public float 남은시간;
    }
    readonly Queue<훈련신청>[] 훈련대기열 = { new(), new() };
    public 전장상태 상태 { get; }
    public static int 왕번호(int 팀) => 팀 * 진영칸;
    public static int 회관번호(int 팀) => 왕번호(팀) + 1;
    internal static int 모닥불번호(int 팀) => 모닥불시작칸 + 팀;
    internal static bool 울타리건설인가(int 종류) => 종류 is 나무울타리건설 or 돌울타리건설;
    internal static bool 건설종류인가(int 종류) => 종류 is 0 or 1 or 모닥불건설 || 울타리건설인가(종류) || 시설건설인가(종류) || 종류 == 깃발건설;
    internal static float 건설시간(전장대상 대상) =>
        대상.울타리 ? 울타리건설시간 : 대상.종류 == 대상종류.모닥불 ? 모닥불건설시간 : 6f;

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
            왕직업.지휘관 => (8, 4, 0, 240),
            왕직업.아처 => (7, 6, 0, 180),
            왕직업.검사 => (9, 5, 0, 250),
            왕직업.트린다미어 => (9, 5, 2, 250),
            왕직업.소라카 => (3, 5, 10, 180),
            왕직업.애쉬 => (7, 6, 4, 180),
            왕직업.아지르 => (4, 5, 9, 190),
            왕직업.암살자 => (8, 8, 0, 175),
            왕직업.이블린 => (8, 8, 6, 175),
            왕직업.그라가스 => (8, 3, 6, 285),
            왕직업.하이머딩거 => (3, 5, 10, 175),
            왕직업.헤카림 => (8, 8, 4, 245),
            왕직업.아이번 => (3, 6, 9, 205),
            왕직업.수호자 => (8, 4, 4, 240),
            왕직업.추적자 => (6, 9, 4, 175),
            _ => (4, 5, 9, 195)
        };
    }

    public static (int 힘, int 민첩, int 지능, int 체력) 능력치(전장대상 유닛, 왕직업 직업 = 왕직업.수호자)
    {
        if (유닛.해골병사) return (11, 6, 3, 180);
        if ((유닛.종류 is 대상종류.주민 or 대상종류.병사 or 대상종류.왕) && 유닛.힘 > 0)
            return (유닛.힘, 유닛.민첩, 유닛.지능, (int)유닛.최대체력);
        return 능력치(유닛.종류, 직업);
    }

    internal static int 주민능력치보정(int 기본값, int 보정, int 단위 = 1)
        => Mathf.Max(1, 기본값 + 보정 * 단위);

    internal const int 주민체력보정단위 = 5;

    public static string 직업설명(왕직업 직업)
    {
        var (힘, 민첩, 지능, 체력) = 능력치(대상종류.왕, 직업);
        if (왕직업목록.장착가능(직업)) return $"체력 {체력}   힘 {힘}   민첩 {민첩}";
        if (왕직업목록.신규(직업))
        {
            string 설명 = $"{직업}  |  체력 {체력}  힘 {힘}  민첩 {민첩}  지능 {지능}\n{왕직업목록.패시브(직업)}";
            for (int i = 0; i < 4; i++) { var 기술 = 왕직업목록.스킬(직업, i); 설명 += $"\n{"QWER"[i]}  {기술.이름} · {기술.쿨다운}초"; }
            return 설명;
        }
        string 특기 = 직업 switch
        {
            왕직업.수호자 => "돌진 시 보호막",
            왕직업.추적자 => "빠른 이동과 긴 돌진",
            _ => "강한 회복과 넓은 신벌"
        };
        return $"힘 {힘}  ·  민첩 {민첩}  ·  지능 {지능}  ·  체력 {체력}   |   {특기}";
    }

    public 전장규칙(자원지점[] 자원들, 격자길찾기 길찾기, Vector2[] 거점들, 왕직업 청, 왕직업 적, int 시드,
        int 청장착 = 왕직업목록.기본장착, int 적장착 = 왕직업목록.기본장착)
    {
        this.자원들 = 자원들;
        this.길찾기 = 길찾기;
        for (int i = 0; i < 경로들.Length; i++) 경로들[i] = new 격자길찾기.이동경로();
        난수 = new System.Random(시드);
        상태 = new 전장상태
        {
            대상들 = new 전장대상[전체칸],
            진영들 = new[] { 새진영(청, 청장착), 새진영(적, 적장착) },
            자원잔량 = Array.ConvertAll(자원들, 지점 => 지점.채집한도),
            자원재생완료시각 = new float[자원들.Length]
        };
        var (청록거점, 적색거점) = 시작거점쌍선택(거점들);
        for (int 팀 = 0; 팀 < 2; 팀++)
        {
            Vector2 거점 = 팀 == 0 ? 청록거점 : 적색거점;
            float 안쪽 = -Mathf.Sign(거점.x);
            배치(회관번호(팀), 팀, 대상종류.회관, 거점, 1350);
            배치(왕번호(팀), 팀, 대상종류.왕, 거점 + new Vector2(안쪽 * 2, 0), 능력치(대상종류.왕, 상태.진영들[팀].직업).체력);
            배치(빈칸(상태, 팀, 대상종류.주민), 팀, 대상종류.주민, 거점 + new Vector2(안쪽, -1.8f), 능력치(대상종류.주민).체력);
        }
        짐승들 = new 전장짐승(상태, 길찾기, 시드);
        왕기술효과갱신();
    }

    (Vector2, Vector2) 시작거점쌍선택(Vector2[] 거점들)
    {
        if (길찾기.지형 != null)
        {
            const float 최소간격 = 38f;
            var 후보들 = new List<Vector2>();
            foreach (var 후보 in 길찾기.연결된칸중심들(거점들[0]))
            {
                if (!시작거점가능(후보) || !시작자원가까움(후보)) continue;
                float 안쪽 = -Mathf.Sign(후보.x);
                if (!길찾기.정적통행가능(후보 + new Vector2(안쪽, 0))
                    || !길찾기.정적통행가능(후보 + new Vector2(안쪽, -1))
                    || !길찾기.정적통행가능(후보 + new Vector2(안쪽 * 5, 0))
                    || !길찾기.정적통행가능(후보 - new Vector2(안쪽 * 3, 0))) continue;
                후보들.Add(후보);
            }

            while (후보들.Count > 1)
            {
                int 번호 = 난수.Next(후보들.Count);
                var 첫거점 = 후보들[번호];
                var 상대후보들 = 후보들.FindAll(후보 => (후보 - 첫거점).sqrMagnitude >= 최소간격 * 최소간격);
                if (상대후보들.Count > 0)
                {
                    var 상대거점 = 상대후보들[난수.Next(상대후보들.Count)];
                    return 난수.Next(2) == 0 ? (첫거점, 상대거점) : (상대거점, 첫거점);
                }

                후보들[번호] = 후보들[^1];
                후보들.RemoveAt(후보들.Count - 1);
            }
        }

        int 왼쪽팀 = 난수.Next(2);
        var 왼쪽 = 시작거점선택(거점들[0], 거점들[1], -1);
        var 오른쪽 = 시작거점선택(거점들[2], 거점들[3], 1);
        return 왼쪽팀 == 0 ? (왼쪽, 오른쪽) : (오른쪽, 왼쪽);
    }

    bool 시작자원가까움(Vector2 거점)
    {
        const float 채집터최대거리 = 34f;
        bool 나무 = false, 돌 = false;
        foreach (var 지점 in 자원들)
        {
            if (지점.숲나무 || (지점.위치 - (Vector3)거점).sqrMagnitude > 채집터최대거리 * 채집터최대거리) continue;
            나무 |= 지점.재화번호 == 0;
            돌 |= 지점.재화번호 == 1;
            if (나무 && 돌) return true;
        }
        return false;
    }

    Vector2 시작거점선택(Vector2 기준1, Vector2 기준2, int 방향)
    {
        const int 가로반경 = 20, 세로반경 = 14;
        const float 중앙여유 = 14;
        var 후보들 = new List<Vector2>();
        var 확인한칸 = new HashSet<Vector2>();
        후보추가(길찾기.중심정렬(기준1));
        후보추가(길찾기.중심정렬(기준2));
        return 후보들.Count > 0 ? 후보들[난수.Next(후보들.Count)] : 길찾기.중심정렬(기준1);

        void 후보추가(Vector2 기준)
        {
            for (int y = -세로반경; y <= 세로반경; y++)
            for (int x = -가로반경; x <= 가로반경; x++)
            {
                if (x * x * 세로반경 * 세로반경 + y * y * 가로반경 * 가로반경 >
                    가로반경 * 가로반경 * 세로반경 * 세로반경) continue;
                var 후보 = 길찾기.중심정렬(기준 + new Vector2(x, y));
                if (후보.x * 방향 < 중앙여유 || !확인한칸.Add(후보)) continue;
                if (시작거점가능(후보)) 후보들.Add(후보);
            }
        }
    }

    bool 시작거점가능(Vector2 거점)
    {
        float 안쪽 = -Mathf.Sign(거점.x);
        return 시작건물자리가능(거점)
            && 길찾기.정적통행가능(거점 + Vector2.down * 1.1f)
            && 길찾기.정적통행가능(거점 + new Vector2(안쪽 * 2, 0))
            && 길찾기.정적통행가능(거점 + new Vector2(안쪽, -1.8f))
            && 시작건물자리가능(길찾기.중심정렬(거점 + new Vector2(안쪽 * 2.9f, 1.8f)))
            && 시작건물자리가능(길찾기.중심정렬(거점 + new Vector2(안쪽 * 3, -1.8f)));
    }

    bool 시작건물자리가능(Vector2 위치) =>
        길찾기.건설지형가능(위치) && 자원지점과간격(자원들, 위치);

    static bool 자원지점과간격(자원지점[] 자원들, Vector2 위치)
    {
        foreach (var 지점 in 자원들)
            if (!지점.숲나무 && Vector2.Distance(지점.위치, 위치) < 1.55f)
                return false;
        return true;
    }

    진영상태 새진영(왕직업 직업, int 장착구성) => new()
    {
        직업 = 직업, 장착구성 = 왕직업목록.장착정리(직업, 장착구성), 재화 = new[] { 125, 75, 16, 10, 5 }, 알림 = "", 자원별채집횟수 = new int[자원들.Length]
    };

    전장대상 배치(int 번호, int 팀, 대상종류 종류, Vector2 위치, float 체력)
    {
        var 대상 = new 전장대상
        {
            팀 = 팀, 종류 = 종류, 위치 = 위치, 목적지 = 위치, 체력 = 체력, 최대체력 = 체력
        };
        if (종류 == 대상종류.왕 && 상태.진영들[팀].시련유물 > 0)
        {
            var 능력 = 능력치(종류, 상태.진영들[팀].직업);
            대상.힘 = 능력.힘 + 상태.진영들[팀].시련유물 * 4; 대상.민첩 = 능력.민첩; 대상.지능 = 능력.지능;
        }
        상태.대상들[번호] = 대상;
        길찾기.초기화(경로들[번호]);
        if (대상.건물 && !대상.함정) 길찾기.건물반영(번호, 위치, true);
        return 대상;
    }

    static int 빈칸(전장상태 상태, int 팀, 대상종류 종류)
    {
        int 시작 = 왕번호(팀) + (종류 switch
        {
            대상종류.제재소 => 2, 대상종류.훈련소 => 4, 대상종류.주민 => 6, _ => 병사시작칸
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

    Vector2 제한(Vector2 위치) => 길찾기.전장안쪽(위치);

    void 메시지(int 팀, string 문장, bool 경고 = false) => 상태.진영들[팀].알림보내기(문장, 경고);

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
        if (!지불가능(상태.진영들[팀].재화, 항목)) { 메시지(팀, "재화가 부족합니다.", 경고: true); return false; }
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
            case 명령종류.시련활성화:
                시련사용(팀, 내용.값);
                break;
            case 명령종류.이동:
                if (왕.생존)
                {
                    왕방향입력[팀].방향 = Vector2.zero;
                    이동지정(왕, 위치);
                }
                break;
            case 명령종류.방향이동:
                방향이동지정(팀, 왕, 내용.위치, 내용.값 == 1);
                break;
            case 명령종류.공격:
                bool 부대공격 = 내용.부대 is { Length: > 0 };
                if (!(부대공격 ? 적인가(팀, 내용.값) : 공격가능(왕, 내용.값))
                    || !표적보임(팀, 상태.대상들[내용.값])) break;
                if (부대공격)
                {
                    foreach (var 유닛 in 내부대(팀, 내용.부대))
                    {
                        이동지정(유닛, 상태.대상들[내용.값].위치);
                        유닛.표적 = 내용.값;
                        유닛.지정공격 = true;
                    }
                }
                else if (왕.생존)
                {
                    왕방향입력[팀] = default;
                    이동지정(왕, 상태.대상들[내용.값].위치);
                    왕.표적 = 내용.값;
                }
                break;
            case 명령종류.왕채집:
                if (왕.생존 && 채집가능(내용.값))
                {
                    왕방향입력[팀] = default;
                    채집지정(왕, 내용.값);
                }
                break;
            case 명령종류.채집:
                if (!채집가능(내용.값)) return;
                foreach (var 주민 in 내부대(팀, 내용.부대))
                {
                    if (주민.종류 != 대상종류.주민) continue;
                    채집지정(주민, 내용.값);
                }
                break;
            case 명령종류.부대이동:
                부대이동지정(내부대(팀, 내용.부대), 위치);
                break;
            case 명령종류.건설:
                건설(팀, 내용.값, 위치, 내용.세로);
                break;
            case 명령종류.모집:
                메시지(팀, "주민을 선택하고 아군 훈련소로 보내세요.");
                break;
            case 명령종류.주민훈련:
                주민훈련접수(팀, 내용.값, 내용.부대);
                break;
            case 명령종류.주민추첨:
                주민추첨시작(팀, 내용.값, 내용.주민모집인원);
                break;
            case 명령종류.사냥:
                if (!짐승들.사냥대상(내용.값, out _, out var 짐승)) break;
                if (내용.부대 != null)
                {
                    foreach (var 사냥꾼 in 내부대(팀, 내용.부대)) 사냥지정(사냥꾼, 짐승.위치);
                }
                else if (왕.생존)
                {
                    왕방향입력[팀] = default;
                    사냥지정(왕, 짐승.위치);
                }
                break;
            case 명령종류.고기굽기:
                메시지(팀, 전장식량.조리시작(상태, 팀, 내용.값), 경고: true);
                break;
            case 명령종류.고기사용:
                var 음식결과 = 전장식량.사용(상태, 팀, 내용.값);
                메시지(팀, 음식결과.문장, 음식결과.경고);
                break;
            case 명령종류.스킬:
                if (왕.생존) 시전(팀, 왕, 내용.값, 위치, 내용.부대);
                break;
        }

        void 사냥지정(전장대상 사냥꾼, Vector2 짐승위치)
        {
            이동지정(사냥꾼, 짐승위치);
            사냥꾼.사냥표적 = 내용.값;
        }
    }

    IEnumerable<전장대상> 내부대(int 팀, int[] 번호들)
    {
        if (번호들 == null || 번호들.Length > 병력제한 + 주민제한) yield break;
        foreach (int 번호 in 번호들)
        {
            if (번호 < 0 || 번호 >= 전체칸) continue;
            var 유닛 = 상태.대상들[번호];
            if (유닛 != null && 유닛.생존 && 유닛.팀 == 팀 && 유닛.훈련남음 <= 0
                && 유닛.종류 is 대상종류.주민 or 대상종류.병사) yield return 유닛;
        }
    }

    static void 이동지정(전장대상 유닛, Vector2 위치)
    {
        if (유닛.훈련남음 > 0) return;
        유닛.목적지 = 위치;
        유닛.표적 = -1;
        유닛.채집터 = -1;
        유닛.재생대기채집터 = -1;
        유닛.사냥표적 = 0;
        유닛.훈련소 = -1;
        유닛.이동중 = true;
        유닛.대형복귀 = false;
        유닛.지정공격 = false;
    }

    void 부대이동지정(IEnumerable<전장대상> 대상들, Vector2 위치)
    {
        var 부대 = new List<전장대상>();
        foreach (var 대상 in 대상들)
            if (!부대.Contains(대상)) 부대.Add(대상);
        if (부대.Count == 0) return;
        if (부대.Count == 1)
        {
            이동지정(부대[0], 위치);
            return;
        }

        int 열수 = Mathf.Min(4, Mathf.CeilToInt(Mathf.Sqrt(부대.Count)));
        int 행수 = (부대.Count + 열수 - 1) / 열수;
        var 반크기 = new Vector2(열수 - 1, 행수 - 1) * (대형간격 * .5f);
        var 최소 = 제한(길찾기.전장범위.min) + 반크기;
        var 최대 = 제한(길찾기.전장범위.max) - 반크기;
        var 중심 = Vector2.Min(Vector2.Max(위치, 최소), 최대);
        var 자리들 = new List<Vector2>(부대.Count);
        for (int i = 0; i < 부대.Count; i++)
        {
            int 행 = i / 열수;
            int 행인원 = Mathf.Min(열수, 부대.Count - 행 * 열수);
            var 간격 = new Vector2(i % 열수 - (행인원 - 1) * .5f, (행수 - 1) * .5f - 행) * 대형간격;
            자리들.Add(대형자리(제한(중심 + 간격), 자리들));
        }
        // 근처 자리부터 배정해 대형 안에서 이동 경로가 서로 교차하는 일을 줄인다.
        foreach (var 자리 in 자리들)
        {
            int 가까운번호 = 0;
            float 거리 = float.PositiveInfinity;
            for (int i = 0; i < 부대.Count; i++)
            {
                float 후보 = (부대[i].위치 - 자리).sqrMagnitude;
                if (후보 >= 거리) continue;
                가까운번호 = i;
                거리 = 후보;
            }
            var 유닛 = 부대[가까운번호];
            부대.RemoveAt(가까운번호);
            이동지정(유닛, 자리);
            유닛.대형복귀 = 유닛.종류 == 대상종류.병사 && 길찾기.통행가능(자리);
        }
    }

    Vector2 대형자리(Vector2 목표, List<Vector2> 자리들)
    {
        if (빈자리(목표)) return 목표;
        for (int 반경 = 1; 반경 <= 3; 반경++)
        {
            Vector2 최선 = 목표;
            float 거리 = float.PositiveInfinity;
            for (int y = -반경; y <= 반경; y++)
            for (int x = -반경; x <= 반경; x++)
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != 반경) continue;
                float 후보거리 = x * x + y * y;
                if (후보거리 >= 거리) continue;
                var 후보 = 목표 + new Vector2(x, y) * 대형간격;
                if (제한(후보) != 후보 || !빈자리(후보)) continue;
                최선 = 후보;
                거리 = 후보거리;
            }
            if (거리 < float.PositiveInfinity) return 최선;
        }
        return 목표;

        bool 빈자리(Vector2 후보)
        {
            if (!길찾기.통행가능(후보)) return false;
            foreach (var 자리 in 자리들)
                if ((자리 - 후보).sqrMagnitude < 병사간격 * 병사간격) return false;
            return true;
        }
    }

    void 채집지정(전장대상 유닛, int 번호)
    {
        이동지정(유닛, 자원들[번호].위치);
        유닛.채집터 = 번호;
    }

    bool 채집가능(int 번호) => 번호 >= 0 && 번호 < 자원들.Length && 상태.자원잔량[번호] > 0;

    bool 도달가능(Vector2 시작, Vector2 목표, float 거리)
    {
        if ((시작 - 목표).sqrMagnitude <= 거리 * 거리) return true;
        bool 목표통행가능 = 길찾기.통행가능(목표);
        var 목표칸중심 = 길찾기.중심정렬(목표);
        if (목표통행가능 && 길찾기.중심정렬(시작) == 목표칸중심) return true;
        길찾기.구하기(시작, 목표, 도달확인경로);
        if (도달확인경로.Count == 0) return false;
        var 끝 = 도달확인경로[^1];
        return (끝 - 목표).sqrMagnitude <= 거리 * 거리 || 목표통행가능 && 끝 == 목표칸중심;
    }

    int 다음채집터(Vector2 주민위치, 자원지점 고갈지점)
    {
        int 결과 = -1;
        float 최단거리 = float.PositiveInfinity;
        Vector2 고갈위치 = 고갈지점.위치;
        for (int i = 0; i < 자원들.Length; i++)
        {
            var 지점 = 자원들[i];
            if (지점.재화번호 != 고갈지점.재화번호 || !채집가능(i)) continue;
            Vector2 위치 = 지점.위치;
            if ((위치 - 고갈위치).sqrMagnitude > 연속채집반경 * 연속채집반경) continue;
            float 거리 = (위치 - 주민위치).sqrMagnitude;
            if (거리 >= 최단거리) continue;
            if (지점.숲나무 && 거리 > 채집거리 * 채집거리 && !길찾기.채집인접칸있음(위치)) continue;
            if (!도달가능(주민위치, 위치, 채집거리)) continue;
            결과 = i;
            최단거리 = 거리;
        }
        return 결과;
    }

    void 자원채집(전장대상 유닛, int 요청량)
    {
        int 번호 = 유닛.채집터;
        var 지점 = 자원들[번호];
        int 수량 = Mathf.Min(상태.자원잔량[번호], 요청량);
        상태.자원잔량[번호] -= 수량;
        var 진영 = 상태.진영들[유닛.팀];
        진영.재화[지점.재화번호] += 수량;
        진영.자원별채집횟수[번호]++;
        진영.채집완료 = true;
        if (상태.자원잔량[번호] > 0) return;

        상태.자원재생완료시각[번호] = 상태.시간 + 자원고갈시간 + 자원재생시간;
        고갈자원들.Add(번호);
        if (지점.숲나무) 길찾기.숲나무반영(지점.위치, false);
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 == null || 대상.채집터 != 번호) continue;
            if (대상 is { 생존: true, 종류: 대상종류.주민, 훈련소: < 0 })
            {
                int 다음 = 다음채집터(대상.위치, 지점);
                if (다음 >= 0)
                {
                    채집지정(대상, 다음);
                    continue;
                }
                대상.재생대기채집터 = 번호;
            }
            대상.채집터 = -1;
            대상.이동중 = false;
        }
    }

    void 자원재생()
    {
        for (int i = 고갈자원들.Count - 1; i >= 0; i--)
        {
            int 번호 = 고갈자원들[i];
            if (상태.시간 < 상태.자원재생완료시각[번호]) continue;
            var 지점 = 자원들[번호];
            상태.자원잔량[번호] = 지점.채집한도;
            상태.자원재생완료시각[번호] = 0;
            if (지점.숲나무) 길찾기.숲나무반영(지점.위치, true);
            고갈자원들.RemoveAt(i);
        }
    }

    void 방향이동지정(int 팀, 전장대상 왕, Vector2 방향, bool 달리기)
    {
        if (!왕.생존 || Mathf.Abs(방향.x) > 1 || Mathf.Abs(방향.y) > 1) return;
        if (방향 == Vector2.zero && !달리기)
        {
            왕방향입력[팀] = default;
            return;
        }
        if (방향 != Vector2.zero && (왕방향입력[팀].만료 <= 상태.시간 || 왕방향입력[팀].방향 == Vector2.zero))
        {
            왕.이동중 = false;
            왕.표적 = -1;
            왕.채집터 = -1;
            왕.사냥표적 = 0;
            길찾기.초기화(경로들[왕번호(팀)]);
        }
        // 입력 갱신이 끊겨도 왕이 계속 움직이지 않게 한다.
        왕방향입력[팀] = (방향.normalized, 상태.시간 + .36f, 달리기);
    }

    void 건설(int 팀, int 종류, Vector2 위치, bool 세로 = false)
    {
        위치 = 길찾기.중심정렬(위치);
        string 오류 = 건설오류(상태, 길찾기, 자원들, 팀, 종류, 위치, out int 칸);
        if (오류 != null) { 메시지(팀, 오류, 경고: true); return; }
        var (건물, 체력) = 종류 switch
        {
            0 => (대상종류.제재소, 210),
            1 => (대상종류.훈련소, 320),
            깃발건설 => (대상종류.깃발, 180),
            나무울타리건설 => (대상종류.나무울타리, 125),
            모닥불건설 => (대상종류.모닥불, 140),
            >= 브루저건설 and <= 점프건설 => (시설종류(종류), 종류 == 타워건설 ? 420 : 종류 >= 지뢰건설 ? 90 : 320),
            _ => (대상종류.돌울타리, 240)
        };
        비용차감(팀, 종류);
        if (종류 == 타워건설)
        {
            var 아처 = 타워재료(상태, 팀);
            아처.체력 = 0;
            아처.이동중 = false;
            아처.공격순번++;
        }
        var 새건물 = 배치(칸, 팀, 건물, 위치, 체력);
        새건물.세로 = 새건물.울타리 && 세로;
        새건물.완공대기 = 건설시간(새건물);
    }

    internal static bool 건설가능(전장상태 상태, 격자길찾기 길찾기, 자원지점[] 자원들,
        int 팀, int 종류, Vector2 위치, out Vector2 배치위치)
    {
        배치위치 = 길찾기.중심정렬(길찾기.전장안쪽(위치));
        return 상태.승자 < 0 && 건설오류(상태, 길찾기, 자원들, 팀, 종류, 배치위치, out _) == null;
    }

    static string 건설오류(전장상태 상태, 격자길찾기 길찾기, 자원지점[] 자원들,
        int 팀, int 종류, Vector2 위치, out int 칸)
    {
        칸 = -1;
        bool 울타리 = 울타리건설인가(종류);
        bool 모닥불 = 종류 == 모닥불건설;
        if (!건설종류인가(종류)) return "지을 수 없는 건물입니다.";
        int 정원 = 건설정원(상태, 팀, 종류);
        if (건설수량(상태, 팀, 종류) >= 정원)
            return 울타리 ? $"울타리는 두 종류를 합쳐 최대 {정원}칸입니다."
                : 종류 == 깃발건설 ? $"깃발은 최대 {정원}개입니다. 골렘 시련 보상으로 한도를 늘릴 수 있습니다."
                : "이 종류의 건설 정원에 도달했습니다.";
        if (울타리)
        {
            int 시작 = 울타리시작칸 + 팀 * 울타리제한;
            for (int i = 시작; i < 시작 + 울타리제한; i++)
                if (상태.대상들[i] is not { 생존: true }) { 칸 = i; break; }
        }
        else if (모닥불)
        {
            int 번호 = 모닥불번호(팀);
            if (상태.대상들[번호] is not { 생존: true }) 칸 = 번호;
        }
        else if (종류 == 깃발건설)
        {
            for (int i = 깃발시작칸 + 팀 * 깃발제한; i < 깃발시작칸 + (팀 + 1) * 깃발제한; i++)
                if (상태.대상들[i] is not { 생존: true }) { 칸 = i; break; }
        }
        else if (시설건설인가(종류)) 칸 = 빈시설칸(상태, 팀, 종류);
        else 칸 = 빈칸(상태, 팀, 종류 == 0 ? 대상종류.제재소 : 대상종류.훈련소);
        var 회관 = 상태.대상들[회관번호(팀)];
        if (칸 < 0) return 모닥불 ? "모닥불은 진영당 하나만 지을 수 있습니다."
            : 울타리 ? $"울타리는 두 종류를 합쳐 최대 {울타리제한}칸입니다." : "이 종류의 건설 정원에 도달했습니다.";
        if (종류 == 타워건설 && 타워재료(상태, 팀) == null) return "훈련을 마친 아처 병사 1명이 필요합니다. (건설 시 소비)";
        if (!건설영역안(상태, 팀, 위치) || !길찾기.건설지형가능(위치))
            return "회관 또는 완공된 아군 깃발 반경 8 안의 빈 땅에 배치하세요.";
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 is not { 생존: true }) continue;
            if ((울타리 || 모닥불) && 길찾기.중심정렬(대상.위치) == 위치)
                return 울타리 ? "비어 있는 칸에 울타리를 배치하세요." : "비어 있는 칸에 모닥불을 배치하세요.";
            if (대상.건물 && !(울타리 && 대상.울타리)
                && Vector2.Distance(대상.위치, 위치) < (울타리 || 대상.울타리 ? 1f : 2.15f))
                return "다른 건물과 간격이 필요합니다.";
        }
        foreach (var 시련 in 상태.시련들)
            if (Vector2.Distance(위치, 시련.위치) < 2.4f) return "시련석 주변은 비워 두세요.";
        if (!자원지점과간격(자원들, 위치))
            return "자원 지점과 간격이 필요합니다.";
        return 지불가능(상태.진영들[팀].재화, 종류) ? null : "재화가 부족합니다.";
    }

    internal static int 양성예정병력(전장상태 상태, int 팀)
    {
        int 수 = 수량(상태, 팀, 대상종류.병사);
        int 시작 = 왕번호(팀) + 6, 끝 = 왕번호(팀) + 병사시작칸;
        for (int i = 시작; i < 끝; i++)
            if (상태.대상들[i] is { 생존: true, 종류: 대상종류.주민, 훈련소: >= 0 }) 수++;
        return 수;
    }

    // 기존 병력과 앞선 양성 예약 한 명당 기본 비용과 시간에 5%를 더한다.
    internal static int 병사훈련비용(int 재화번호, int 병력)
        => (비용[3][재화번호] * (20 + 병력) + 10) / 20;

    internal static float 병사훈련시간(int 병력) => 주민훈련시간 * (20 + 병력) / 20;

    void 주민훈련접수(int 팀, int 훈련소번호, int[] 번호들)
    {
        훈련대기정리(팀);
        훈련상태갱신(팀);
        if (훈련소번호 < 0 || 훈련소번호 >= 전체칸) return;
        var 건물 = 상태.대상들[훈련소번호];
        bool 전문 = 전문훈련소(건물);
        if (건물 is not { 생존: true, 완공대기: <= 0 } || 건물.팀 != 팀 || (!전문 && 건물.종류 != 대상종류.훈련소))
        { 메시지(팀, "완성된 아군 훈련소가 필요합니다.", 경고: true); return; }
        if (!훈련자리(건물, out var 자리))
        { 메시지(팀, "훈련소 입구에 통행 가능한 자리가 없습니다.", 경고: true); return; }
        int 접수 = 0;
        bool 후보있음 = false;
        string 거절사유 = null;
        int 병력 = 전문 ? 0 : 양성예정병력(상태, 팀);
        var 진영 = 상태.진영들[팀];
        foreach (var 주민 in 내부대(팀, 번호들))
        {
            if (!훈련가능유닛(팀, 건물, 주민)) continue;
            후보있음 = true;
            if (!전문 && 병력 >= 병력제한)
            { 거절사유 = "병력 정원이 가득 찼습니다."; break; }
            int 비용산정병력 = 전문 ? 0 : 병력;
            bool 지불가능 = true;
            for (int i = 0; i < 진영.재화.Length; i++)
                if (진영.재화[i] < 병사훈련비용(i, 비용산정병력)) { 지불가능 = false; break; }
            if (!지불가능) { 거절사유 = "재화가 부족합니다."; break; }
            for (int i = 0; i < 진영.재화.Length; i++)
                진영.재화[i] -= 병사훈련비용(i, 비용산정병력);
            이동지정(주민, 자리);
            주민.훈련소 = 훈련소번호;
            var 신청 = new 훈련신청
            {
                주민번호 = Array.IndexOf(상태.대상들, 주민), 훈련소번호 = 훈련소번호,
                주민 = 주민, 건물 = 건물, 비용산정병력 = 비용산정병력,
                남은시간 = 전문 ? 2f : 병사훈련시간(비용산정병력)
            };
            훈련대기열[팀].Enqueue(신청);
            병력++;
            접수++;
        }
        훈련상태갱신(팀);
        if (접수 > 0) 메시지(팀, 거절사유 == null ? $"유닛 {접수}명이 훈련소로 이동합니다." : $"{접수}명 훈련 접수 · {거절사유}", 경고: 거절사유 != null);
        else if (거절사유 != null) 메시지(팀, 거절사유, 경고: true);
        else if (!후보있음) 메시지(팀, 전문 ? "일반 훈련소에서 먼저 병사를 양성한 뒤 선택하세요." : "아직 훈련 대기 중이 아닌 주민을 선택하세요.", 경고: true);
    }

    bool 훈련자리(전장대상 건물, out Vector2 자리)
    {
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0) continue;
            자리 = 건물.위치 + new Vector2(x, y);
            if (길찾기.통행가능(자리)) return true;
        }
        자리 = default;
        return false;
    }

    void 훈련대기정리(int 팀)
    {
        var 큐 = 훈련대기열[팀];
        int 개수 = 큐.Count;
        for (int i = 0; i < 개수; i++)
        {
            var 신청 = 큐.Dequeue();
            if (신청.주민.생존 && 신청.주민.훈련소 == 신청.훈련소번호
                && ReferenceEquals(상태.대상들[신청.주민번호], 신청.주민)
                && 신청.건물.생존 && ReferenceEquals(상태.대상들[신청.훈련소번호], 신청.건물))
            {
                큐.Enqueue(신청);
                continue;
            }
            신청.건물.훈련대기수 = 0;
            신청.건물.훈련잔여합계 = 0;
            for (int j = 0; j < 비용[3].Length; j++)
                상태.진영들[팀].재화[j] += 병사훈련비용(j, 신청.비용산정병력);
            if (신청.주민.훈련소 == 신청.훈련소번호)
            {
                신청.주민.훈련소 = -1;
                신청.주민.훈련남음 = 0;
                신청.주민.이동중 = false;
            }
            메시지(팀, "주민 훈련이 취소되어 비용을 반환했습니다.", 경고: true);
        }
    }

    void 훈련상태갱신(int 팀)
    {
        var 큐 = 훈련대기열[팀];
        var 진영 = 상태.진영들[팀];
        진영.훈련대기인원 = 큐.Count;
        진영.훈련남은시간 = 큐.Count > 0 ? 큐.Peek().남은시간 : 0;
        foreach (var 신청 in 큐) 신청.건물.훈련대기수 = 0;
        // 같은 팀의 훈련 접수는 학교가 달라도 순서대로 처리한다.
        float 누적시간 = 0;
        foreach (var 신청 in 큐)
        {
            누적시간 += 신청.남은시간;
            신청.건물.훈련대기수++;
            신청.건물.훈련잔여합계 = 누적시간;
        }
    }

    void 주민훈련진행(int 팀, float 초)
    {
        훈련대기정리(팀);
        var 큐 = 훈련대기열[팀];
        if (큐.Count == 0) return;
        var 신청 = 큐.Peek();
        // 앞 주민이 도착할 때까지 뒷 주민도 기다린다. 훈련 완료도 접수 순서(FIFO)다.
        if ((신청.주민.위치 - 신청.건물.위치).sqrMagnitude > 2.25f) return;
        신청.주민.이동중 = false;
        if (전문훈련소(신청.건물)) 신청.주민.훈련남음 = 신청.남은시간;
        신청.남은시간 = Mathf.Max(0, 신청.남은시간 - 초);
        if (신청.남은시간 > 0) return;
        bool 기존병사 = 신청.주민.종류 == 대상종류.병사;
        int 칸 = 기존병사 ? 신청.주민번호 : 빈칸(상태, 팀, 대상종류.병사);
        if (칸 < 0) return;
        var 이전능력 = 능력치(신청.주민);
        bool 전문 = 전문훈련소(신청.건물);
        bool 궁수 = 신청.건물.종류 == 대상종류.아처훈련소;
        float 최대 = 전문 ? Mathf.Max(105, 신청.주민.최대체력) + (궁수 ? 20 : 60) : 능력치(대상종류.병사).체력;
        var 병사 = 배치(칸, 팀, 대상종류.병사, 신청.주민.위치, 최대);
        if (전문)
        {
            병사.병과 = 궁수 ? 병과.아처 : 병과.브루저;
            병사.힘 = Mathf.Max(5, 이전능력.힘) + (궁수 ? 2 : 4);
            병사.민첩 = Mathf.Max(5, 이전능력.민첩) + (궁수 ? 4 : 1);
            병사.지능 = 이전능력.지능;
        }
        병사.체력 *= Mathf.Clamp01(신청.주민.체력 / 신청.주민.최대체력);
        병사.왼쪽 = 신청.주민.왼쪽;
        신청.주민.훈련소 = -1;
        신청.주민.훈련남음 = 0;
        신청.주민.체력 = 0;
        신청.주민.이동중 = false;
        큐.Dequeue();
        신청.건물.훈련대기수 = 0;
        신청.건물.훈련잔여합계 = 0;
        var 진영 = 상태.진영들[팀];
        진영.모집완료 = true;
        메시지(팀, 전문 ? $"{병사.병과} 훈련 완료! 능력치가 강화됐습니다." : "훈련 완료! 주민이 병사로 전환됐습니다.");
    }

    internal static int 주민빈정원(전장상태 상태, int 팀)
        => 주민제한 - 수량(상태, 팀, 대상종류.주민);

    internal static int 주민추첨최대인원(전장상태 상태, int 팀, 주민투자단계 단계)
    {
        if (!상태.대상들[회관번호(팀)].생존 || 상태.진영들[팀].모집종류 >= 0) return 0;
        return 주민추첨구매가능인원(상태.진영들[팀].재화, 단계, 주민빈정원(상태, 팀));
    }

    static int 주민추첨구매가능인원(int[] 재화, 주민투자단계 단계, int 정원)
    {
        if (단계.나무 > 0) 정원 = Mathf.Min(정원, 재화[0] / 단계.나무);
        if (단계.돌 > 0) 정원 = Mathf.Min(정원, 재화[1] / 단계.돌);
        return Mathf.Max(0, 정원);
    }

    internal static string 주민추첨오류(전장상태 상태, int 팀, 주민투자단계 단계, int 인원)
    {
        var 진영 = 상태.진영들[팀];
        if (!상태.대상들[회관번호(팀)].생존) return "마을회관이 파괴됐습니다.";
        if (진영.모집종류 >= 0) return "다른 모집이 진행 중입니다.";
        int 빈정원 = 주민빈정원(상태, 팀);
        if (빈정원 == 0) return "주민 정원에 도달했습니다.";
        if (인원 < 1 || 인원 > 빈정원) return "모집할 인원을 다시 선택하세요.";
        return 인원 > 주민추첨구매가능인원(진영.재화, 단계, 빈정원) ? "자원이 부족합니다." : null;
    }

    void 주민추첨시작(int 팀, int 번호, int 인원)
    {
        if (번호 < 0 || 번호 >= 주민투자단계들.Length) return;
        var 단계 = 주민투자단계들[번호];
        string 오류 = 주민추첨오류(상태, 팀, 단계, 인원);
        if (오류 != null)
        {
            if (상태.진영들[팀].주민투자 == 0) 주민추첨보고(팀, -1, 오류);
            return;
        }

        var 진영 = 상태.진영들[팀];
        var 비용 = 단계.총비용(인원);
        진영.재화[0] -= 비용.나무;
        진영.재화[1] -= 비용.돌;
        진영.주민투자 = 번호 + 1;
        진영.주민모집인원 = 인원;
        진영.모집종류 = 0;
        진영.모집대기 = 주민추첨시간;
    }

    void 주민추첨보고(int 팀, int 인원, string 결과)
    {
        var 진영 = 상태.진영들[팀];
        진영.주민추첨인원 = 인원;
        진영.주민추첨결과 = 결과;
        진영.주민추첨결과번호++;
        메시지(팀, 결과, 경고: 인원 < 0);
    }

    void 주민추첨완료(int 팀)
    {
        var 진영 = 상태.진영들[팀];
        var 단계 = 주민투자단계들[진영.주민투자 - 1];
        int 인원 = Mathf.Max(1, 진영.주민모집인원);
        진영.주민투자 = 0;
        진영.주민모집인원 = 0;

        var 회관 = 상태.대상들[회관번호(팀)];
        if (!회관.생존 || 인원 > 주민빈정원(상태, 팀))
        {
            주민추첨환불(팀, 단계, 인원, "모집을 완료할 수 없어 자원을 반환했습니다.");
            return;
        }
        var 자리들 = new List<Vector2>(인원);
        for (int 반경 = 1; 반경 <= 4 && 자리들.Count < 인원; 반경++)
            for (int y = -반경; y <= 반경 && 자리들.Count < 인원; y++)
                for (int x = -반경; x <= 반경 && 자리들.Count < 인원; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != 반경) continue;
                    var 자리 = 회관.위치 + new Vector2(x, y);
                    if (!길찾기.통행가능(자리)) continue;
                    if (Array.Exists(상태.대상들, 대상 => 대상 is { 생존: true }
                        && (대상.위치 - 자리).sqrMagnitude < .36f)) continue;
                    자리들.Add(자리);
                }

        if (자리들.Count != 인원)
        {
            주민추첨환불(팀, 단계, 인원, "회관 주변에 자리가 부족해 자원을 반환했습니다.");
            return;
        }

        var 기본 = 능력치(대상종류.주민);
        foreach (var 자리 in 자리들)
        {
            int 체력 = 주민능력치보정(기본.체력, 주민보정추첨(단계), 주민체력보정단위);
            var 주민 = 배치(빈칸(상태, 팀, 대상종류.주민), 팀, 대상종류.주민, 자리, 체력);
            주민.힘 = 주민능력치보정(기본.힘, 주민보정추첨(단계));
            주민.민첩 = 주민능력치보정(기본.민첩, 주민보정추첨(단계));
            주민.지능 = 주민능력치보정(기본.지능, 주민보정추첨(단계));
        }
        주민추첨보고(팀, 인원, $"주민 {인원}명을 모집했습니다.");
    }

    int 주민보정추첨(주민투자단계 단계) => 난수.Next(단계.최소보정, 단계.최대보정 + 1);

    void 주민추첨환불(int 팀, 주민투자단계 단계, int 인원, string 결과)
    {
        var 재화 = 상태.진영들[팀].재화;
        var 비용 = 단계.총비용(인원);
        재화[0] += 비용.나무;
        재화[1] += 비용.돌;
        주민추첨보고(팀, -1, 결과);
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

    void 시전(int 팀, 전장대상 왕, int 번호, Vector2 위치, int[] 부대 = null)
    {
        if (왕직업목록.장착가능(상태.진영들[팀].직업))
        {
            장착스킬시전(팀, 왕, 번호, 위치, 부대);
            return;
        }
        if (직업시전(팀, 왕, 번호, 위치)) return;
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
            { 메시지(팀, "스킬 사거리 밖입니다.", 경고: true); return; }
            float 반경 = 번호 == 0 ? 1.7f : 2.58f + 능력.지능 * .18f;
            진영.스킬위치 = 위치;
            진영.스킬반경 = 반경;
            for (int i = 0; i < 전체칸; i++)
                if (적인가(팀, i) && Vector2.Distance(상태.대상들[i].위치, 위치) < 반경)
                    피해(i, 번호 == 0 ? 38 : 88, 팀);
        }
        if (왕.채집터 >= 0)
        {
            왕.채집터 = -1;
            왕.이동중 = false;
        }
        왕.효과시간 = .45f;
        진영.스킬왕위치표시 = 번호 is 1 or 2;
        진영.스킬표시시간 = .48f;
        진영.쿨다운[번호] = 스킬시간[번호];
    }

    bool 적인가(int 팀, int 번호)
    {
        if (번호 < 0 || 번호 >= 전체칸) return false;
        var 대상 = 상태.대상들[번호];
        return 대상 != null && 대상.생존 && 대상.팀 != 팀;
    }

    bool 표적보임(int 팀, 전장대상 대상) => 대상.훈련남음 <= 0 && 직업표적보임(팀, 대상);

    void 피해(int 번호, float 피해량, int 공격팀)
    {
        var 대상 = 상태.대상들[번호];
        if (!대상.생존 || 대상.훈련남음 > 0 || 상태.승자 >= 0) return;
        float 방어 = 대상.방패시간 > 0 ? .38f : 대상.종류 == 대상종류.회관 ? .63f : 1;
        if (대상.무적 > 0) return;
        대상.비전투 = 0; 대상.은신 = false;
        float 실제 = 피해량 * 방어;
        float 흡수 = Mathf.Min(대상.보호량, 실제); 대상.보호량 -= 흡수; 실제 -= 흡수;
        float 가드흡수 = Mathf.Min(대상.가드, 실제);
        대상.가드 -= 가드흡수;
        실제 -= 가드흡수;
        if (실제 > 0 && 패시브(대상, 장착기술.책임전가))
        {
            int 선택 = -1, 수 = 0;
            for (int i = 0; i < 전체칸; i++)
                if (상태.대상들[i] is { 생존: true, 종류: 대상종류.병사, 훈련남음: <= 0 } 병사
                    && 병사.팀 == 대상.팀 && 난수.Next(++수) == 0) 선택 = i;
            if (선택 >= 0)
            {
                실제 *= .5f;
                피해(선택, 실제 * .75f, 공격팀);
            }
        }
        대상.체력 = Mathf.Max(대상.불사 > 0 ? 1 : 0, 대상.체력 - 실제);
        대상.효과시간 = .16f;
        if (대상.생존) return;
        if (대상.종류 == 대상종류.병사) 병사사망(대상);
        if (대상.건물 && !대상.함정) 길찾기.건물반영(번호, 대상.위치, false);
        if (대상.종류 == 대상종류.회관) 상태.승자 = 공격팀;
        if (대상.종류 == 대상종류.왕)
        {
            왕방향입력[대상.팀] = default;
            돌진들[대상.팀] = null;
            대상.재등장 = 14;
        }
    }

    public void 진행(float 초, bool 혼자)
    {
        if (상태.승자 >= 0) return;
        if (!float.IsFinite(초) || 초 < 0) return;
        상태.시간 += 초;
        자원재생();
        기본공격진행(초);
        직업지대진행(초);
        for (int 팀 = 0; 팀 < 2; 팀++) 경제(팀, 초);
        Span<bool> 달린팀 = stackalloc bool[2] { false, false };
        for (int i = 0; i < 전체칸 && 상태.승자 < 0; i++)
        {
            var 대상 = 상태.대상들[i];
            if (대상 == null) continue;
            if (!대상.생존)
            {
                if (대상.종류 == 대상종류.왕 && (대상.재등장 -= 초) <= 0)
                {
                    var 회관 = 상태.대상들[회관번호(대상.팀)];
                    var 왕진영 = 상태.진영들[대상.팀];
                    왕진영.스태미나 = 최대스태미나;
                    왕진영.스태미나회복대기 = 0;
                    왕진영.스태미나고갈 = false;
                    배치(i, 대상.팀, 대상종류.왕, 회관.위치 + Vector2.down * 1.1f, 대상.최대체력);
                }
                continue;
            }
            직업상태진행(대상, 초);
            장착상태진행(대상, 초);
            if (대상.종류 == 대상종류.시련보스) continue;
            if (!대상.생존) continue;
            대상.공격대기 -= 초 * 장착공속(대상);
            대상.효과시간 -= 초;
            대상.방패시간 -= 초;
            대상.도약시간 = Mathf.Max(0, 대상.도약시간 - 초);
            대상.도약대기 = Mathf.Max(0, 대상.도약대기 - 초);
            if (대상.건물)
            {
                if (시설인가(대상)) { 시설진행(i, 대상, 초); continue; }
                if ((대상.울타리 || 대상.종류 is 대상종류.모닥불 or 대상종류.깃발) && 대상.완공대기 > 0)
                    대상.완공대기 = Mathf.Max(0, 대상.완공대기 - 초);
                if (대상.종류 == 대상종류.회관 && 대상.공격대기 <= 0)
                {
                    int 침입자 = 가까운적(대상, 4.5f, true);
                    if (침입자 >= 0) { 기본공격시작(대상, 침입자, 25); 대상.공격대기 = 1.4f; }
                }
                continue;
            }
            var 진영 = 상태.진영들[대상.팀];
            if (!진영.진격완료 && 대상.종류 == 대상종류.병사
                && Vector2.Distance(대상.위치, 상태.대상들[회관번호(1 - 대상.팀)].위치) < 8)
                진영.진격완료 = true;
            if (대상.종류 == 대상종류.왕 && 스킬돌진진행(대상, 초)) continue;
            if (제어진행(대상, 초)) continue;
            if (소환진행(대상, 초)) continue;
            var 이전위치 = 대상.위치;
            float 달리기시간 = 0;
            if (대상.종류 == 대상종류.왕 && !진영.스태미나고갈
                && 왕방향입력[대상.팀] is { 달리기: true } 입력 && 입력.만료 > 상태.시간)
                달리기시간 = Mathf.Min(초, 진영.스태미나 / 초당스태미나소모);
            유닛진행(i, 대상, 초, 달리기시간);
            벽차단(대상, 이전위치);
            if (달리기시간 > 0 && 대상.위치 != 이전위치)
            {
                달린팀[대상.팀] = true;
                진영.스태미나 = Mathf.Max(0, 진영.스태미나 - 달리기시간 * 초당스태미나소모);
                if (진영.스태미나 < .001f)
                {
                    진영.스태미나 = 0;
                    진영.스태미나고갈 = true;
                }
                진영.스태미나회복대기 = 스태미나회복지연;
            }
        }
        if (상태.승자 < 0)
        {
            for (int 팀 = 0; 팀 < 2; 팀++)
                if (!달린팀[팀]) 스태미나회복(팀, 초);
            시련진행(초);
            병사간격유지(초);
            짐승들.진행(초);
            if (혼자) 적진행(초);
        }
        왕기술효과갱신();
    }

    void 병사간격유지(float 초)
    {
        // 서로 밀려난 병사는 이동 명령으로 받은 자리가 비면 돌아간다.
        for (int i = 0; i < 병력제한 * 2; i++)
        {
            var 병사 = 병사슬롯(i);
            if (병사 is not { 생존: true, 대형복귀: true, 이동중: false, 표적: < 0 } || 병사.속박중 || 병사.소환 != 소환종류.없음) continue;
            var 방향 = 병사.목적지 - 병사.위치;
            if (방향.sqrMagnitude <= .0001f) continue;
            var 걸음 = Vector2.ClampMagnitude(방향, 2f * 초);
            if (복귀이동(i, 병사, 걸음)) continue;
            var 옆걸음 = new Vector2(-걸음.y, 걸음.x) * .9f;
            if (!복귀이동(i, 병사, 걸음 * .4f + 옆걸음))
                복귀이동(i, 병사, 걸음 * .4f - 옆걸음);
        }

        for (int a = 0; a < 병력제한 * 2; a++)
        {
            var 병사 = 병사슬롯(a);
            if (병사 is not { 생존: true } || 병사.속박중 || 병사.훈련소 >= 0 || 병사.소환 != 소환종류.없음) continue;
            for (int b = a + 1; b < 병력제한 * 2; b++)
            {
                var 이웃 = 병사슬롯(b);
                if (이웃 is not { 생존: true } || 이웃.속박중 || 이웃.훈련소 >= 0 || 이웃.소환 != 소환종류.없음) continue;
                var 차이 = 병사.위치 - 이웃.위치;
                float 거리제곱 = 차이.sqrMagnitude;
                if (거리제곱 >= 병사간격 * 병사간격) continue;

                float 거리 = Mathf.Sqrt(거리제곱);
                float 각도 = (a * 7 + b * 11) * (Mathf.PI / 8);
                var 방향 = 거리 > .0001f ? 차이 / 거리 : new Vector2(Mathf.Cos(각도), Mathf.Sin(각도));
                var 보정 = 방향 * Mathf.Min((병사간격 - 거리) * .5f, 3f * 초);
                var 다음 = 길찾기.방향이동(병사.위치, 제한(병사.위치 + 보정));
                var 이웃다음 = 길찾기.방향이동(이웃.위치, 제한(이웃.위치 - 보정));
                if ((다음 - 이웃다음).sqrMagnitude <= 거리제곱) continue;
                병사.위치 = 다음;
                이웃.위치 = 이웃다음;
            }
        }

        전장대상 병사슬롯(int 번호) =>
            상태.대상들[왕번호(번호 / 병력제한) + 진영칸 - 병력제한 + 번호 % 병력제한];

        bool 복귀이동(int 번호, 전장대상 병사, Vector2 걸음)
        {
            var 다음 = 길찾기.방향이동(병사.위치, 병사.위치 + 걸음);
            if ((병사.목적지 - 다음).sqrMagnitude >= (병사.목적지 - 병사.위치).sqrMagnitude) return false;
            for (int i = 0; i < 병력제한 * 2; i++)
            {
                if (i == 번호 || 병사슬롯(i) is not { 생존: true } 이웃) continue;
                float 거리제곱 = (다음 - 이웃.위치).sqrMagnitude;
                if (거리제곱 < 병사간격 * 병사간격 && 거리제곱 < (병사.위치 - 이웃.위치).sqrMagnitude) return false;
            }
            유닛위치갱신(병사, 다음);
            return true;
        }
    }

    void 경제(int 팀, float 초)
    {
        주민훈련진행(팀, 초);
        훈련상태갱신(팀);
        var 진영 = 상태.진영들[팀];
        진영.스킬표시시간 -= 초;
        진영.음식회복시간 = Mathf.Max(0, 진영.음식회복시간 - 초);
        var 조리결과 = 전장식량.조리진행(상태, 팀, 초);
        메시지(팀, 조리결과.문장, 조리결과.경고);
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
        진영.모집종류 = -1;
        if (진영.주민투자 > 0)
        {
            주민추첨완료(팀);
            return;
        }
        var 출구 = 상태.대상들[회관번호(팀)];
        var 유닛 = 대상종류.주민;
        int 칸 = 빈칸(상태, 팀, 유닛);
        if (칸 < 0) return;
        var 위치 = 출구.위치 + new Vector2((float)(난수.NextDouble() - .5) * 2.6f, -1.55f);
        배치(칸, 팀, 유닛, 제한(위치), 능력치(유닛).체력);
    }

    void 스태미나회복(int 팀, float 초)
    {
        var 진영 = 상태.진영들[팀];
        if (진영.스태미나 >= 최대스태미나 || !상태.대상들[왕번호(팀)].생존) return;
        float 회복시간 = Mathf.Max(0, 초 - 진영.스태미나회복대기);
        진영.스태미나회복대기 = Mathf.Max(0, 진영.스태미나회복대기 - 초);
        진영.스태미나 = Mathf.Min(최대스태미나, 진영.스태미나 + 회복시간 * 초당스태미나회복);
        if (진영.스태미나 >= 달리기재개스태미나) 진영.스태미나고갈 = false;
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

    void 유닛진행(int 번호, 전장대상 유닛, float 초, float 달리기시간)
    {
        bool 왕 = 유닛.종류 == 대상종류.왕;
        if (왕 && 왕공격동작중(유닛)) return;
        Vector2 목적 = 유닛.목적지;
        var 능력 = 능력치(유닛, 상태.진영들[유닛.팀].직업);
        float 이동거리 = (유닛.종류 switch
        {
            대상종류.왕 => 3.18f,
            대상종류.주민 => 1.98f,
            _ => 2.35f
        } + 능력.민첩 * .18f) * (초 + 달리기시간 * (달리기배율 - 1)) * 길찾기.이동속도배율(유닛.위치) * 직업이속(유닛);
        if (유닛.속박 > 0) 이동거리 = 0;
        if (유닛.훈련소 >= 0)
        {
            var 건물 = 상태.대상들[유닛.훈련소];
            if ((유닛.위치 - 건물.위치).sqrMagnitude > 2.25f)
            {
                길찾기.이동(경로들[번호], 유닛.위치, 유닛.목적지, 이동거리, out var 다음위치);
                유닛위치갱신(유닛, 다음위치);
            }
            else 유닛.이동중 = false;
            return;
        }
        if (왕 && 왕방향입력[유닛.팀].만료 > 상태.시간 && 왕방향입력[유닛.팀].방향 != Vector2.zero)
        {
            유닛위치갱신(유닛, 왕방향이동(유닛.위치, 왕방향입력[유닛.팀].방향, 이동거리));
            return;
        }
        if (유닛.사냥표적 != 0)
        {
            if (!짐승들.사냥대상(유닛.사냥표적, out int 짐승칸, out var 짐승))
            {
                유닛.사냥표적 = 0;
                유닛.이동중 = false;
                return;
            }

            유닛.목적지 = 짐승.위치;
            var 방향 = 짐승.위치 - 유닛.위치;
            float 사냥거리 = 왕 ? 왕사거리(유닛, 1.15f) : 1.15f;
            if (방향.sqrMagnitude <= 사냥거리 * 사냥거리)
            {
                if (유닛.공격대기 <= 0)
                {
                    if (왕 && 상태.진영들[유닛.팀].직업 == 왕직업.아이번) { 유닛.사냥표적 = 0; 유닛.이동중 = false; return; }
                    유닛.왼쪽 = 방향.x < 0;
                    유닛.공격대기 = 왕 ? .65f : .93f;
                    짐승기본공격시작(유닛, 짐승칸, (왕 ? 8 : 3) + 2 * 능력.힘);
                }
            }
            else
            {
                길찾기.이동(경로들[번호], 유닛.위치, 짐승.위치, 이동거리, out var 다음위치);
                유닛위치갱신(유닛, 다음위치);
            }
            return;
        }
        if (유닛.지정공격 && (!적인가(유닛.팀, 유닛.표적)
            || !표적보임(유닛.팀, 상태.대상들[유닛.표적])))
        {
            유닛.지정공격 = false;
            유닛.표적 = -1;
            유닛.이동중 = false;
        }
        if (유닛.종류 == 대상종류.병사 && !유닛.지정공격) 유닛.표적 = 가까운적(유닛, 유닛.병과 == 병과.아처 ? 5.5f : 4.1f);
        if (공격가능(유닛, 유닛.표적) && 표적보임(유닛.팀, 상태.대상들[유닛.표적]))
        {
            var 적 = 상태.대상들[유닛.표적];
            목적 = 적.위치;
            if (왕 || 유닛.지정공격) 유닛.목적지 = 목적;
            if (Vector2.Distance(유닛.위치, 목적) <= (왕 ? 왕사거리(유닛, 적.건물 ? 1.45f : .95f) : 유닛.병과 == 병과.아처 ? 5.5f : 적.건물 ? 1.45f : .95f))
            {
                if (유닛.공격대기 <= 0)
                {
                    float 힘 = (왕 ? 8 : 3) + 2 * 능력.힘;
                    기본공격시작(유닛, 유닛.표적, 힘);
                    유닛.공격대기 = 왕 ? (상태.진영들[유닛.팀].직업 == 왕직업.애쉬 && 유닛.강화시간 > 0 ? .32f : .65f) : .93f;
                }
                return;
            }
        }
        else
        {
            유닛.표적 = -1;
            if (유닛.채집터 >= 0 && !채집가능(유닛.채집터))
            {
                유닛.채집터 = -1;
                유닛.이동중 = false;
            }
            if (채집가능(유닛.재생대기채집터))
                채집지정(유닛, 유닛.재생대기채집터);
            if (유닛.채집터 >= 0)
            {
                var 지점 = 자원들[유닛.채집터];
                목적 = 지점.위치;
                if ((유닛.위치 - 목적).sqrMagnitude <= 채집거리 * 채집거리)
                {
                    유닛.이동중 = false;
                    if (유닛.공격대기 <= 0)
                    {
                        int 채집량 = 왕 ? 1 : 3;
                        자원채집(유닛, 지점.재화번호 < 2 ? 채집량 * 2 : 채집량);
                        유닛.공격대기 = 왕 ? 2.2f : 3.6f - 능력.지능 * .1f;
                    }
                    return;
                }
            }
            else if (!유닛.이동중) return;
        }
        if (길찾기.이동(경로들[번호], 유닛.위치, 목적, 이동거리, out Vector2 다음))
        {
            유닛.이동중 = false;
            if (유닛.채집터 >= 0 && (다음 - 목적).sqrMagnitude > 채집거리 * 채집거리)
                유닛.채집터 = -1;
        }
        유닛위치갱신(유닛, 다음);
    }

    static void 유닛위치갱신(전장대상 유닛, Vector2 다음)
    {
        if (Mathf.Abs(다음.x - 유닛.위치.x) > .005f) 유닛.왼쪽 = 다음.x < 유닛.위치.x;
        if ((다음 - 유닛.위치).sqrMagnitude > .00001f) 유닛.시선 = (다음 - 유닛.위치).normalized;
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
            if (후보 >= 거리 || !표적보임(유닛.팀, 대상)) continue;
            거리 = 후보;
            찾음 = i;
        }
        for (int i = 시련시작칸; i < 전체칸; i++)
        {
            if (!적인가(유닛.팀, i)) continue;
            var 대상 = 상태.대상들[i];
            float 후보 = (대상.위치 - 유닛.위치).sqrMagnitude;
            if (후보 >= 거리 || !표적보임(유닛.팀, 대상)) continue;
            거리 = 후보; 찾음 = i;
        }
        return 찾음;
    }
}
