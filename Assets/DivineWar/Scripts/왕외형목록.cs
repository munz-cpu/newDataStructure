using System;
using UnityEngine;

[CreateAssetMenu(menuName = "DivineWar/왕 외형 목록")]
public sealed class 왕외형목록 : ScriptableObject
{
    [Serializable]
    public sealed class 배정
    {
        public 왕직업 직업;
        public string 에셋이름;
        public Sprite[] 대기, 이동, 공격;
    }

    public 배정[] 직업들 = Array.Empty<배정>();
    [Serializable]
    public sealed class 소환배정
    {
        public 소환종류 종류;
        public Sprite[] 대기, 이동, 공격;
    }
    public 소환배정[] 소환물들 = Array.Empty<소환배정>();
    public Sprite[] 검사회전, 검사구르기;
    static 왕외형목록 읽은목록;
    public static 왕외형목록 가져오기() => 읽은목록 != null ? 읽은목록 : 읽은목록 = Resources.Load<왕외형목록>("Classes/PlayerAppearances");

    public static Sprite[] 검사기술외형(장착기술 기술) => 기술 switch
    {
        장착기술.회전베기 => 가져오기()?.검사회전,
        장착기술.앞구르기 => 가져오기()?.검사구르기,
        _ => null
    };

    public 배정 전용외형(왕직업 직업)
    {
        직업 = 직업 switch
        {
            왕직업.추적자 or 왕직업.애쉬 => 왕직업.아처,
            왕직업.트린다미어 or 왕직업.헤카림 => 왕직업.검사,
            왕직업.예언자 or 왕직업.소라카 or 왕직업.아지르
                or 왕직업.이블린 or 왕직업.그라가스 or 왕직업.하이머딩거 or 왕직업.아이번 => 왕직업.수호자,
            _ => 직업
        };
        foreach (var 외형 in 직업들) if (외형.직업 == 직업) return 외형;
        return null;
    }

    public 배정 표시외형(왕직업 직업) => 전용외형(직업) ?? (직업들.Length > 0 ? 직업들[0] : null);
    public 소환배정 소환외형(소환종류 종류)
    {
        foreach (var 외형 in 소환물들) if (외형.종류 == 종류) return 외형;
        return null;
    }
    public string 배정안내(왕직업 직업) => 전용외형(직업) is { } 외형
        ? $"외형: {외형.에셋이름}" : "전용 에셋 부족 · 공용 기사 임시 외형";
}
