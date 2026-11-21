using System;
using UnityEngine;

// 호스트가 보낸 상태로 짐승 또는 사냥 전리품을 그린다.
[Serializable]
public struct 짐승상태
{
    public int 등장번호, 종류, 방향;
    public Vector2 위치;
    public float 체력;
    public int 전리품팀;
    public bool 전리품 => 등장번호 != 0 && 체력 <= 0;
    public bool 사냥가능 => 등장번호 != 0 && 체력 > 0 && 종류 is 전장짐승.돼지 or 전장짐승.닭;
}

sealed class 전장짐승
{
    internal const int 전체칸 = 9;
    internal const int 종류수 = 3;
    internal const int 돼지 = 1, 닭 = 2;
    static readonly float[] 배회속도 = { 1.25f, .9f, 1.05f };
    static readonly float[] 도주속도 = { 3.7f, 2.45f, 2.6f };

    sealed class 행동
    {
        public readonly 격자길찾기.이동경로 경로 = new();
        public Vector2 목적지;
        public float 남은수명, 최대수명, 생각대기, 휴식;
        public float 전리품시간;
        public bool 이동중, 도주중;
    }

    readonly 행동[] 행동들 = new 행동[전체칸];
    readonly 전장상태 상태;
    readonly 격자길찾기 길찾기;
    readonly System.Random 난수;
    float 등장대기;
    int 등장번호;

    public 전장짐승(전장상태 상태, 격자길찾기 길찾기, int 시드)
    {
        this.상태 = 상태;
        this.길찾기 = 길찾기;
        난수 = new System.Random(시드 ^ 0x375f092d);
        상태.짐승들 = new 짐승상태[전체칸];
        for (int i = 0; i < 전체칸; i++) 행동들[i] = new 행동();

        // 개전 시 양쪽 마을과 전장 중앙에서 서로 다른 세 종류를 볼 수 있게 한다.
        int 첫종류 = 난수.Next(종류수);
        for (int i = 0; i < 종류수; i++)
        {
            var 중심 = i < 2 ? 상태.대상들[전장규칙.회관번호(i)].위치 : 길찾기.전장범위.center;
            if (!등장(i, (첫종류 + i) % 종류수, 중심)) 등장(i, (첫종류 + i) % 종류수);
        }
        등장대기 = 범위(5f, 9f);
    }

    public void 진행(float 초)
    {
        int 빈칸 = -1;
        for (int i = 0; i < 전체칸; i++)
        {
            ref var 짐승 = ref 상태.짐승들[i];
            if (짐승.전리품) 전리품갱신(ref 짐승, 행동들[i], 초);
            else if (짐승.등장번호 != 0) 행동갱신(ref 짐승, 행동들[i], 초);
            if (짐승.등장번호 == 0 && 빈칸 < 0) 빈칸 = i;
        }
        등장대기 = Mathf.Max(0, 등장대기 - 초);
        if (빈칸 >= 0 && 등장대기 <= 0)
            등장대기 = 등장(빈칸, 난수.Next(종류수)) ? 범위(5f, 9f) : 1f;
    }

    internal bool 사냥대상(int 등장번호, out int 칸, out 짐승상태 짐승)
    {
        for (int i = 0; i < 전체칸; i++)
        {
            var 후보 = 상태.짐승들[i];
            if (후보.등장번호 != 등장번호 || !후보.사냥가능) continue;
            칸 = i;
            짐승 = 후보;
            return true;
        }
        칸 = -1;
        짐승 = default;
        return false;
    }

    internal void 피해(int 칸, float 피해량, int 팀)
    {
        ref var 짐승 = ref 상태.짐승들[칸];
        짐승.체력 = Mathf.Max(0, 짐승.체력 - 피해량);
        if (!짐승.전리품) return;
        짐승.전리품팀 = 팀;
        행동들[칸].전리품시간 = 0;
    }

    void 전리품갱신(ref 짐승상태 전리품, 행동 행동, float 초)
    {
        행동.전리품시간 += 초;
        if (행동.전리품시간 < .65f) return;
        int 팀 = 전리품.전리품팀;
        if (가까운대상(전리품.위치, 1.45f, 팀: 팀) != null)
        {
            int 고기 = 전리품.종류 == 돼지 ? (int)고기종류.날돼지고기 : (int)고기종류.날닭고기;
            var 진영 = 상태.진영들[팀];
            진영.고기[고기]++;
            진영.알림 = $"{전장식량.이름[고기]} +1";
            진영.알림시간 = 3.5f;
            전리품 = default;
        }
        else if (행동.전리품시간 >= 18f) 전리품 = default;
    }

    bool 등장(int 번호, int 종류, Vector2? 주변 = null)
    {
        var 지도 = 길찾기.전장범위;
        for (int 시도 = 0; 시도 < 32; 시도++)
        {
            Vector2 후보 = 주변 is Vector2 중심
                ? 중심 + 임의방향() * 범위(3.4f, 5.2f)
                : new Vector2(범위(지도.xMin + 2f, 지도.xMax - 2f), 범위(지도.yMin + 2f, 지도.yMax - 2f));
            var 위치 = 길찾기.중심정렬(길찾기.전장안쪽(후보));
            if (!길찾기.통행가능(위치) || 가까운대상(위치, 2.2f, true) != null) continue;
            if (다른짐승인접(위치)) continue;

            상태.짐승들[번호] = new 짐승상태
            {
                등장번호 = ++등장번호, 종류 = 종류, 방향 = 난수.Next(8), 위치 = 위치,
                체력 = 종류 == 돼지 ? 24f : 종류 == 닭 ? 12f : 36f
            };
            var 행동 = 행동들[번호];
            행동.남은수명 = 범위(42f, 68f);
            행동.최대수명 = 행동.남은수명 + 12f;
            행동.생각대기 = 0;
            행동.휴식 = 범위(.6f, 2f);
            행동.이동중 = 행동.도주중 = false;
            길찾기.초기화(행동.경로);
            return true;
        }
        return false;
    }

    void 행동갱신(ref 짐승상태 짐승, 행동 행동, float 초)
    {
        행동.남은수명 -= 초;
        행동.최대수명 -= 초;
        행동.휴식 -= 초;
        행동.생각대기 -= 초;
        if (행동.생각대기 <= 0)
        {
            행동.생각대기 = 범위(.28f, .48f);
            if (행동.남은수명 <= 0 && (행동.최대수명 <= 0 || 가까운대상(짐승.위치, 5f) == null))
            {
                짐승 = default;
                return;
            }

            var 위협 = 가까운대상(짐승.위치, 2.25f);
            if (위협 != null)
            {
                var 반대쪽 = 짐승.위치 - 위협.위치;
                if (!행동.도주중 || Vector2.Dot(행동.목적지 - 짐승.위치, 반대쪽) <= 0)
                    목적정하기(짐승.위치, 행동, 반대쪽);
            }
            else if (!행동.이동중 && 행동.휴식 <= 0)
                목적정하기(짐승.위치, 행동, null);
        }
        if (!행동.이동중) return;

        float 속도 = 행동.도주중 ? 도주속도[짐승.종류] : 배회속도[짐승.종류];
        float 이동거리 = 속도 * 길찾기.이동속도배율(짐승.위치) * 초;
        if (길찾기.이동(행동.경로, 짐승.위치, 행동.목적지, 이동거리, out var 다음))
        {
            행동.이동중 = 행동.도주중 = false;
            행동.휴식 = 범위(.8f, 3f);
        }
        var 이동 = 다음 - 짐승.위치;
        if (이동.sqrMagnitude > .000001f)
        {
            짐승.위치 = 다음;
            짐승.방향 = (Mathf.RoundToInt(-Mathf.Atan2(이동.y, 이동.x) * Mathf.Rad2Deg / 45f) + 8) % 8;
        }
    }

    void 목적정하기(Vector2 위치, 행동 행동, Vector2? 도주방향)
    {
        bool 도주 = 도주방향.HasValue;
        for (int 시도 = 0; 시도 < 10; 시도++)
        {
            var 방향 = 임의방향();
            if (도주방향 is Vector2 반대쪽 && 반대쪽.sqrMagnitude > .001f)
                방향 = (반대쪽.normalized + 방향 * .86f).normalized;
            var 후보 = 위치 + 방향 * (도주 ? 범위(2.3f, 4.3f) : 범위(1.3f, 3.8f));
            var 목적 = 길찾기.중심정렬(길찾기.전장안쪽(후보));
            var 이동 = 목적 - 위치;
            if (이동.sqrMagnitude < .5f || !길찾기.통행가능(목적)) continue;
            if (도주방향 is Vector2 위협반대 && Vector2.Dot(이동, 위협반대) < -.001f) continue;

            행동.목적지 = 목적;
            행동.이동중 = true;
            행동.도주중 = 도주;
            길찾기.초기화(행동.경로);
            return;
        }
        행동.이동중 = 행동.도주중 = false;
        행동.휴식 = .5f;
    }

    전장대상 가까운대상(Vector2 위치, float 반경, bool 건물포함 = false, int 팀 = -1)
    {
        전장대상 가까운 = null;
        float 거리 = 반경 * 반경;
        foreach (var 대상 in 상태.대상들)
        {
            if (대상 == null || !대상.생존 || (!건물포함 && 대상.건물) || (팀 >= 0 && 대상.팀 != 팀)) continue;
            float 후보 = (대상.위치 - 위치).sqrMagnitude;
            if (후보 >= 거리) continue;
            거리 = 후보;
            가까운 = 대상;
        }
        return 가까운;
    }

    bool 다른짐승인접(Vector2 위치)
    {
        foreach (var 짐승 in 상태.짐승들)
            if (짐승.등장번호 != 0 && (짐승.위치 - 위치).sqrMagnitude < 1.44f) return true;
        return false;
    }

    Vector2 임의방향()
    {
        float 각도 = 범위(0, 2f * Mathf.PI);
        return new Vector2(Mathf.Cos(각도), Mathf.Sin(각도));
    }

    float 범위(float 최소, float 최대) => Mathf.Lerp(최소, 최대, (float)난수.NextDouble());
}
