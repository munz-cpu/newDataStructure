using System;
using System.Collections.Generic;
using UnityEngine;

internal static class 전투음량설정
{
    const string 효과음키 = "DivineWar.EffectsVolume";
    const string 배경음악키 = "DivineWar.MusicVolume";

    public static float 효과음 { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat(효과음키, 1));
    public static float 배경음악 { get; private set; } = Mathf.Clamp01(PlayerPrefs.GetFloat(배경음악키, 1));

    public static void 효과음변경(float 음량)
    {
        효과음 = Mathf.Clamp01(음량);
        PlayerPrefs.SetFloat(효과음키, 효과음);
    }

    public static void 배경음악변경(float 음량)
    {
        배경음악 = Mathf.Clamp01(음량);
        PlayerPrefs.SetFloat(배경음악키, 배경음악);
    }
}

internal sealed class 배경음악재생
{
    const float 페이드시간 = 2.4f;
    const float 시작페이드시간 = 1.4f;
    readonly Dictionary<AudioClip[], 곡순서> 순서들 = new();
    readonly System.Random 난수 = new();
    readonly float 기본음량;
    AudioSource 현재, 이전;
    AudioClip[] 현재목록;
    float 페이드경과, 페이드길이, 목록재생시간;

    public 배경음악재생(AudioSource 첫채널, AudioSource 둘째채널)
    {
        현재 = 첫채널;
        이전 = 둘째채널;
        기본음량 = 첫채널.volume;
        정지();
    }

    public void 갱신(AudioClip[] 목록, float 초, AudioClip[] 유지목록 = null, float 최소유지시간 = 0)
    {
        목록재생시간 += 초;
        float 음량 = 기본음량 * 전투음량설정.배경음악;
        if (페이드경과 < 페이드길이)
        {
            페이드경과 = Mathf.Min(페이드길이, 페이드경과 + 초);
            float 비율 = Mathf.SmoothStep(0, 1, 페이드경과 / 페이드길이);
            현재.volume = 음량 * 비율;
            이전.volume = 음량 * (1 - 비율);
            if (페이드경과 < 페이드길이) return;
            이전.Stop();
            이전.clip = null;
        }
        else 현재.volume = 음량;

        if (목록 == null || 목록.Length == 0) return;
        if (유지목록 != null && 현재목록 == 유지목록 && 목록재생시간 < 최소유지시간)
            목록 = 현재목록;
        var 곡 = 현재.clip;
        if (목록 == 현재목록 && 곡 != null && 현재.isPlaying
            && 곡.samples - 현재.timeSamples > 곡.frequency * 페이드시간) return;

        if (!순서들.TryGetValue(목록, out var 순서))
        {
            순서 = new 곡순서(목록);
            순서들.Add(목록, 순서);
        }
        var 다음곡 = 순서.다음(난수);
        if (다음곡 == null) return;

        bool 이어재생 = 현재.isPlaying;
        (현재, 이전) = (이전, 현재);
        if (현재목록 != 목록) 목록재생시간 = 0;
        현재목록 = 목록;
        현재.clip = 다음곡;
        현재.volume = 0;
        현재.Play();
        페이드경과 = 0;
        페이드길이 = 이어재생 ? 페이드시간 : 시작페이드시간;
    }

    public void 정지()
    {
        현재.Stop();
        이전.Stop();
        현재.clip = 이전.clip = null;
        현재.volume = 이전.volume = 기본음량;
        페이드경과 = 페이드길이 = 목록재생시간 = 0;
        현재목록 = null;
    }

    internal sealed class 곡순서
    {
        readonly AudioClip[] 곡들;
        AudioClip 마지막곡;
        int 다음번호;

        public 곡순서(AudioClip[] 목록)
        {
            곡들 = Array.FindAll(목록, 곡 => 곡 != null);
            다음번호 = 곡들.Length;
        }

        public AudioClip 다음(System.Random 난수)
        {
            if (곡들.Length == 0) return null;
            if (다음번호 == 곡들.Length)
            {
                for (int i = 곡들.Length - 1; i > 0; i--)
                {
                    int 상대 = 난수.Next(i + 1);
                    (곡들[i], 곡들[상대]) = (곡들[상대], 곡들[i]);
                }
                if (곡들.Length > 1 && 곡들[0] == 마지막곡)
                {
                    int 상대 = 난수.Next(1, 곡들.Length);
                    (곡들[0], 곡들[상대]) = (곡들[상대], 곡들[0]);
                }
                다음번호 = 0;
            }
            마지막곡 = 곡들[다음번호++];
            return 마지막곡;
        }
    }
}
