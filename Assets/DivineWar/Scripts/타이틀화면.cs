using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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

[RequireComponent(typeof(UIDocument))]
public sealed class 타이틀화면 : MonoBehaviour
{
    [SerializeField] StyleSheet 화면스타일;
    [SerializeField] Camera 타이틀카메라;
    [SerializeField] SpriteRenderer 타이틀문장;

    readonly Button[] 직업버튼 = new Button[3];
    Button 시작버튼, 호스트버튼, 참가버튼, 복사버튼, 취소버튼;
    Label 직업정보, 방상태;
    TextField 참가코드, 연결주소;
    참가연결 연결;
    왕직업 고른직업;
    bool 손님, 이동중;
    float 시작대기;

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        var 뿌리 = GetComponent<UIDocument>().rootVisualElement;
        뿌리.styleSheets.Add(화면스타일);
        전투글꼴.적용(뿌리);
        시작버튼 = 뿌리.Q<Button>("solo");
        호스트버튼 = 뿌리.Q<Button>("host");
        참가버튼 = 뿌리.Q<Button>("join");
        복사버튼 = 뿌리.Q<Button>("copy");
        취소버튼 = 뿌리.Q<Button>("cancel");
        직업정보 = 뿌리.Q<Label>("class-info");
        방상태 = 뿌리.Q<Label>("room-status");
        참가코드 = 뿌리.Q<TextField>("code");
        연결주소 = 뿌리.Q<TextField>("address");
        for (int i = 0; i < 직업버튼.Length; i++)
        {
            int 번호 = i;
            직업버튼[i] = 뿌리.Q<Button>("class-" + i);
            직업버튼[i].clicked += () => 직업고르기((왕직업)번호);
        }
        시작버튼.clicked += () => 전장이동((왕직업)UnityEngine.Random.Range(0, 3));
        호스트버튼.clicked += 방열기;
        참가버튼.clicked += 방참가;
        취소버튼.clicked += () => 방취소("");
        복사버튼.clicked += 코드복사;
        직업고르기(고른직업);
        방취소("");
    }

    void OnDisable()
    {
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
        for (int i = 0; i < 직업버튼.Length; i++) 직업버튼[i].EnableInClassList("chosen", i == (int)직업);
    }

    void 방열기()
    {
        try
        {
            연결 = 참가연결.방열기(연결주소.value);
            손님 = false;
            시작대기 = 0;
            버튼활성(false);
            참가코드.SetValueWithoutNotify(연결.코드);
            방상태.text = "참가 코드를 상대에게 공유하세요.";
            복사버튼.style.display = DisplayStyle.Flex;
            취소버튼.style.display = DisplayStyle.Flex;
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
            연결 = 참가연결.참가(참가코드.value);
            손님 = true;
            버튼활성(false);
            방상태.text = "호스트에 연결하는 중…";
            취소버튼.style.display = DisplayStyle.Flex;
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
        복사버튼.style.display = DisplayStyle.None;
        취소버튼.style.display = DisplayStyle.None;
        버튼활성(true);
    }

    void 버튼활성(bool 켜짐)
    {
        시작버튼.SetEnabled(켜짐);
        호스트버튼.SetEnabled(켜짐);
        참가버튼.SetEnabled(켜짐);
        연결주소.SetEnabled(켜짐);
        foreach (var 버튼 in 직업버튼) 버튼.SetEnabled(켜짐);
    }
}

