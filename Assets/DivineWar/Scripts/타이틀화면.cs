using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class 전장입장
{
    public 참가연결 연결;
    public 왕직업 청록직업, 적색직업;
    public int 내팀;
    public static 전장입장 대기;

    public static 전장입장 가져오기()
    {
        var 정보 = 대기;
        대기 = null;
        return 정보;
    }
}

public sealed class 타이틀화면 : MonoBehaviour
{
    [SerializeField] Camera 타이틀카메라;
    [SerializeField] SpriteRenderer 타이틀문장;
    [SerializeField] CanvasGroup 메뉴카드;
    [SerializeField] 메뉴버튼[] 직업버튼;
    [SerializeField] Button 시작버튼, 호스트버튼, 참가버튼, 복사버튼, 취소버튼;
    [SerializeField] TMP_Text 직업정보, 방상태;
    [SerializeField] TMP_InputField 참가코드, 연결주소;

    Coroutine 등장;
    Vector2 카드위치;
    참가연결 연결;
    왕직업 고른직업;
    bool 손님, 이동중;
    float 시작대기;

    void Awake()
    {
        카드위치 = ((RectTransform)메뉴카드.transform).anchoredPosition;
        for (int i = 0; i < 직업버튼.Length; i++)
        {
            int 번호 = i;
            직업버튼[i].onClick.AddListener(() => 직업고르기((왕직업)번호));
        }
        시작버튼.onClick.AddListener(() => 전장이동((왕직업)UnityEngine.Random.Range(0, 3)));
        호스트버튼.onClick.AddListener(방열기);
        참가버튼.onClick.AddListener(방참가);
        취소버튼.onClick.AddListener(() => 방취소(""));
        복사버튼.onClick.AddListener(코드복사);
    }

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        등장 = StartCoroutine(메뉴등장.재생((RectTransform)메뉴카드.transform, 메뉴카드, 카드위치));
        직업고르기(고른직업);
        방취소("");
    }

    void OnDisable()
    {
        if (등장 != null) StopCoroutine(등장);
        연결?.Dispose();
        연결 = null;
    }

    void Update()
    {
        타이틀문장.transform.position = 타이틀카메라.ViewportToWorldPoint(new Vector3(.75f, .51f, 10));
        if (연결 == null || 이동중) return;
        if (연결.오류 != null) { 방취소(연결.오류); return; }
        if (!연결.연결됨) return;
        if (손님)
        {
            연결.보내기("J" + (int)고른직업);
            전장이동(고른직업);
            return;
        }
        for (int i = 0; i < 32 && 연결.받기(out string 문장); i++)
        {
            if (문장.Length == 2 && 문장[0] == 'J' && 문장[1] is >= '0' and <= '2')
            {
                전장이동((왕직업)(문장[1] - '0'));
                return;
            }
        }
        if ((시작대기 += Time.unscaledDeltaTime) > 9)
            방취소("상대의 게임 시작 응답을 받지 못했습니다.");
    }

    void 전장이동(왕직업 적색직업)
    {
        if (이동중) return;
        이동중 = true;
        전장입장.대기 = new 전장입장
        {
            연결 = 연결, 청록직업 = 고른직업, 적색직업 = 적색직업, 내팀 = 손님 && 연결 != null ? 1 : 0
        };
        연결 = null;
        SceneManager.LoadScene("Main");
    }

    void 직업고르기(왕직업 직업)
    {
        고른직업 = 직업;
        직업정보.text = 전장규칙.직업설명(직업);
        for (int i = 0; i < 직업버튼.Length; i++) 직업버튼[i].선택표시(i == (int)직업);
    }

    void 방열기()
    {
        try
        {
            연결 = 참가연결.방열기(연결주소.text);
            손님 = false;
            시작대기 = 0;
            버튼활성(false);
            참가코드.SetTextWithoutNotify(연결.코드);
            방상태.text = "참가 코드를 상대에게 공유하세요.";
            복사버튼.gameObject.SetActive(true);
            취소버튼.gameObject.SetActive(true);
        }
        catch (Exception ex) when (ex is FormatException or System.Net.Sockets.SocketException or System.Net.NetworkInformation.NetworkInformationException)
        {
            방상태.text = ex is FormatException ? ex.Message : "방을 열 수 없습니다. TCP 27841 포트 사용 상태를 확인하세요.";
        }
    }

    void 방참가()
    {
        try
        {
            연결 = 참가연결.참가(참가코드.text);
            손님 = true;
            버튼활성(false);
            방상태.text = "호스트에 연결하는 중…";
            취소버튼.gameObject.SetActive(true);
        }
        catch (FormatException) { 방상태.text = "유효한 참가 코드 18자를 입력하세요."; }
    }

    void 코드복사()
    {
        if (연결 == null || 손님) return;
        GUIUtility.systemCopyBuffer = 연결.코드;
        방상태.text = "참가 코드를 복사했습니다. 상대를 기다리는 중…";
    }

    void 방취소(string 문장)
    {
        연결?.Dispose();
        연결 = null;
        시작대기 = 0;
        손님 = false;
        방상태.text = 문장;
        복사버튼.gameObject.SetActive(false);
        취소버튼.gameObject.SetActive(false);
        버튼활성(true);
    }

    void 버튼활성(bool 켜짐)
    {
        시작버튼.interactable = 켜짐;
        호스트버튼.interactable = 켜짐;
        참가버튼.interactable = 켜짐;
        연결주소.interactable = 켜짐;
        foreach (var 버튼 in 직업버튼) 버튼.interactable = 켜짐;
    }
}

