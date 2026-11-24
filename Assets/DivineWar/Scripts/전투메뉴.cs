using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

static class 메뉴등장
{
    public static IEnumerator 재생(RectTransform 카드, CanvasGroup 페이드대상, Vector2 기본위치)
    {
        const float 거리 = 16;
        const float 재생시간 = .26f;
        float 시작 = Time.unscaledTime;
        while (true)
        {
            float 진행 = Mathf.Clamp01((Time.unscaledTime - 시작) / 재생시간);
            float 남은비율 = 1 - 진행;
            남은비율 = 남은비율 * 남은비율 * 남은비율;
            카드.anchoredPosition = 기본위치 - Vector2.up * (거리 * 남은비율);
            페이드대상.alpha = 1 - 남은비율;
            if (진행 >= 1) yield break;
            yield return null;
        }
    }
}

public sealed class 전투메뉴 : MonoBehaviour
{
    [SerializeField] CanvasGroup 모달;
    [SerializeField] RectTransform 메뉴카드;
    [SerializeField] GameObject 이동설정;
    [SerializeField] Button 재개버튼, 나가기버튼;
    [SerializeField] 메뉴버튼 클릭이동버튼, 키보드이동버튼, 주민자동해제버튼;
    [SerializeField] TMP_Text 제목표시, 본문표시, 주민자동해제문구;
    [SerializeField] 메뉴버튼 피해표시버튼;
    [SerializeField] TMP_Text 피해표시문구;

    Coroutine 등장;
    Vector2 카드위치;
    public event Action 재개, 나가기, 주민자동해제전환;
    public event Action 피해표시전환;
    public event Action<bool> 이동방식선택;

    void Awake()
    {
        카드위치 = 메뉴카드.anchoredPosition;
        주민자동해제버튼.onClick.AddListener(주민자동해제선택);
        피해표시버튼.onClick.AddListener(() => 피해표시전환?.Invoke());
        클릭이동버튼.onClick.AddListener(클릭이동선택);
        키보드이동버튼.onClick.AddListener(키보드이동선택);
        재개버튼.onClick.AddListener(재개선택);
        나가기버튼.onClick.AddListener(나가기선택);
        숨기기();
    }

    void OnDisable() => 숨기기();

    void 클릭이동선택() => 이동방식선택?.Invoke(false);
    void 키보드이동선택() => 이동방식선택?.Invoke(true);
    void 주민자동해제선택() => 주민자동해제전환?.Invoke();
    void 재개선택() => 재개?.Invoke();
    void 나가기선택() => 나가기?.Invoke();

    public void 이동방식표시(bool 키보드이동)
    {
        클릭이동버튼.선택표시(!키보드이동);
        키보드이동버튼.선택표시(키보드이동);
    }

    public void 주민자동해제표시(bool 켜짐)
    {
        주민자동해제버튼.선택표시(켜짐);
        주민자동해제문구.text = 켜짐 ? "주민 명령 후 자동 해제  ·  켜짐" : "주민 명령 후 자동 해제  ·  꺼짐";
    }

    public void 피해표시갱신(bool 켜짐)
    {
        피해표시버튼.선택표시(켜짐);
        피해표시문구.text = 켜짐 ? "피해 숫자  ·  켜짐" : "피해 숫자  ·  꺼짐";
    }

    public void 보이기(string 제목, string 본문, bool 재개가능)
    {
        제목표시.text = 제목;
        본문표시.text = 본문;
        재개버튼.gameObject.SetActive(재개가능);
        이동설정.SetActive(재개가능);
        주민자동해제버튼.gameObject.SetActive(재개가능);
        피해표시버튼.gameObject.SetActive(재개가능);
        모달.gameObject.SetActive(true);
        if (등장 != null) StopCoroutine(등장);
        등장 = StartCoroutine(메뉴등장.재생(메뉴카드, 모달, 카드위치));
    }

    public void 숨기기()
    {
        if (등장 != null) StopCoroutine(등장);
        등장 = null;
        if (모달 != null) 모달.gameObject.SetActive(false);
    }
}

