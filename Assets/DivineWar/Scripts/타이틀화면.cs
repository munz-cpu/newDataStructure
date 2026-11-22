using System;
using System.Threading;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class 전장입장
{
    public static bool 서버빌드
    {
#if DIVINEWAR_DEDICATED
        get => true;
#else
        get => false;
#endif
    }

    public 참가연결 연결;
    public 왕직업 청록직업, 적색직업;
    public int 청록장착 = 왕직업목록.기본장착, 적색장착 = 왕직업목록.기본장착;
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
    [SerializeField] 메뉴버튼[] 직업버튼, 스킬칸버튼, 스킬후보버튼;
    [SerializeField] TMP_Text 장착설명;
    [SerializeField] Button 시작버튼, 참가버튼, 취소버튼;
    [SerializeField] TMP_Text 직업정보, 방상태;
    [SerializeField] TMP_InputField 참가코드;
    [SerializeField] NetworkManager 통신관리자;

    Coroutine 등장;
    Vector2 카드위치;
    참가연결 연결;
    const string 직업저장키 = "DivineWar.SelectedJob.V1";
    왕직업 고른직업 = 왕직업.지휘관;
    int 장착구성, 고른칸;
    static string 장착저장키(왕직업 직업) => $"DivineWar.Loadout.V1.{(int)직업}";
    CancellationTokenSource 연결시도;
    bool 이동중, 직업보냄;
    readonly (왕직업 직업, int 장착)?[] 참가준비 = new (왕직업, int)?[2];
    float 시작대기, 재시도시각;

    void Awake()
    {
        if (NetworkManager.Singleton == null) 통신관리자.gameObject.SetActive(true);
        if (전장입장.서버빌드)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 50;
            메뉴카드.gameObject.SetActive(false);
            장착설명.transform.parent.gameObject.SetActive(false);
            return;
        }
        카드위치 = ((RectTransform)메뉴카드.transform).anchoredPosition;
        for (int i = 0; i < 직업버튼.Length; i++)
        {
            int 번호 = i;
            직업버튼[i].onClick.AddListener(() => 직업고르기(왕직업목록.선택[번호]));
        }
        for (int i = 0; i < 스킬칸버튼.Length; i++)
        {
            int 칸 = i;
            스킬칸버튼[i].onClick.AddListener(() => { 고른칸 = 칸; 장착갱신(); });
        }
        for (int i = 0; i < 스킬후보버튼.Length; i++)
        {
            int 후보 = i;
            스킬후보버튼[i].onClick.AddListener(() => 스킬장착(후보));
        }
        시작버튼.onClick.AddListener(() => 전장이동(0, (고른직업, 장착구성),
            (왕직업목록.선택[UnityEngine.Random.Range(0, 왕직업목록.선택.Length)], 왕직업목록.기본장착)));
        참가버튼.onClick.AddListener(연결시작);
        취소버튼.onClick.AddListener(() => 방취소(""));
    }

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        if (전장입장.서버빌드) return;
        등장 = StartCoroutine(메뉴등장.재생((RectTransform)메뉴카드.transform, 메뉴카드, 카드위치));
        var 저장직업 = (왕직업)PlayerPrefs.GetInt(직업저장키, (int)왕직업.지휘관);
        직업고르기(왕직업목록.장착가능(저장직업) ? 저장직업 : 왕직업.지휘관);
        방취소("");
    }

    void OnDisable()
    {
        if (등장 != null) StopCoroutine(등장);
        연결정리();
    }

    void Update()
    {
        if (이동중) return;
        if (전장입장.서버빌드)
        {
            if (연결 == null && 연결시도 == null && Time.realtimeSinceStartup >= 재시도시각) 연결시작();
        }
        else 타이틀문장.transform.position = 타이틀카메라.ViewportToWorldPoint(new Vector3(.75f, .51f, 10));
        if (연결 == null) return;
        if (연결.오류 != null) { 방취소(연결.오류); return; }
        if (연결.전용서버)
        {
            서버준비갱신();
            return;
        }
        if (연결.연결됨 && !직업보냄)
            직업보냄 = 연결.보내기("J" + 준비부호((고른직업, 장착구성)));
        while (연결.받기(out int 팀, out string 문장))
        {
            var 값 = 문장.Split(',');
            if (팀 != -1 || 값.Length != 4 || 값[0] != "R"
                || !int.TryParse(값[1], out int 내진영) || 내진영 is < 0 or > 1
                || !준비읽기(값[2], out var 청) || !준비읽기(값[3], out var 적)) continue;
            전장이동(내진영, 청, 적);
            return;
        }
    }

    static string 준비부호((왕직업 직업, int 장착) 준비) => $"{(int)준비.직업}:{준비.장착}";

    static bool 준비읽기(string 값, out (왕직업 직업, int 장착) 준비)
    {
        준비 = default;
        var 항목 = 값.Split(':');
        if (항목.Length != 2 || !int.TryParse(항목[0], out int 번호) || !int.TryParse(항목[1], out int 장착)) return false;
        var 직업 = (왕직업)번호;
        if (!왕직업목록.유효장착(직업, 장착)) return false;
        준비 = (직업, 장착);
        return true;
    }

    void 서버준비갱신()
    {
        while (연결.받기(out int 팀, out string 문장))
        {
            if (팀 is 0 or 1 && 문장.Length >= 2 && 문장[0] == 'J' && 준비읽기(문장.Substring(1), out var 선택직업))
                참가준비[팀] ??= 선택직업;
        }
        if (!연결.연결됨) return;
        if (!참가준비[0].HasValue || !참가준비[1].HasValue)
        {
            if ((시작대기 += Time.unscaledDeltaTime) > 30f) 방취소("플레이어의 전장 준비 시간이 초과됐습니다.");
            return;
        }
        var 청록 = 참가준비[0].Value;
        var 적색 = 참가준비[1].Value;
        for (int 팀 = 0; 팀 < 2; 팀++)
        {
            if (연결.팀에보내기(팀, $"R,{팀},{준비부호(청록)},{준비부호(적색)}")) continue;
            방취소("전장 시작 정보를 전달하지 못했습니다.");
            return;
        }
        전장이동(-1, 청록, 적색);
    }

    void 전장이동(int 팀, (왕직업 직업, int 장착) 청록, (왕직업 직업, int 장착) 적색)
    {
        if (이동중) return;
        이동중 = true;
        전장입장.대기 = new 전장입장
        {
            연결 = 연결, 청록직업 = 청록.직업, 적색직업 = 적색.직업,
            청록장착 = 청록.장착, 적색장착 = 적색.장착, 내팀 = 팀
        };
        연결 = null;
        SceneManager.LoadScene("Main");
    }

    void 직업고르기(왕직업 직업)
    {
        고른직업 = 직업;
        고른칸 = 0;
        PlayerPrefs.SetInt(직업저장키, (int)직업);
        장착구성 = 왕직업목록.장착정리(직업, PlayerPrefs.GetInt(장착저장키(직업), 왕직업목록.기본장착));
        직업정보.text = $"{직업}  ·  {전장규칙.직업설명(직업)}";
        var 목록 = 왕외형목록.가져오기();
        var 외형 = 목록?.표시외형(직업);
        if (외형?.대기?.Length == 120)
        {
            타이틀문장.sprite = 외형.대기[30];
            타이틀문장.transform.localScale = Vector3.one * 7f;
        }
        for (int i = 0; i < 직업버튼.Length; i++)
        {
            직업버튼[i].선택표시(왕직업목록.선택[i] == 직업);
            var 글 = 직업버튼[i].GetComponentInChildren<TMP_Text>();
            글.text = 왕직업목록.선택[i].ToString();
            글.enableAutoSizing = true; 글.fontSizeMin = 12; 글.fontSizeMax = 22;
            글.rectTransform.offsetMin = Vector2.zero; 글.rectTransform.offsetMax = Vector2.zero;
            foreach (var 그림 in 직업버튼[i].GetComponentsInChildren<Image>())
                if (그림.sprite != null && 그림.sprite.name.Contains("emblem")) 그림.enabled = false;
        }
        직업정보.enableAutoSizing = true; 직업정보.fontSizeMin = 12; 직업정보.fontSizeMax = 19;
        장착갱신();
    }

    void 스킬장착(int 후보)
    {
        if ((uint)후보 >= 왕직업목록.장착후보수(고른직업)) return;
        장착구성 = 왕직업목록.장착변경(장착구성, 고른칸, 후보);
        PlayerPrefs.SetInt(장착저장키(고른직업), 장착구성);
        PlayerPrefs.Save();
        장착갱신();
    }

    void 장착갱신()
    {
        int 고른후보 = 왕직업목록.후보번호(장착구성, 고른칸);
        for (int i = 0; i < 스킬칸버튼.Length; i++)
        {
            int 후보 = 왕직업목록.후보번호(장착구성, i);
            var 기술 = 왕직업목록.정보(왕직업목록.후보(고른직업, 후보));
            string 키 = i == 1 ? "W / F" : 왕직업목록.입력표시(i, false);
            스킬칸버튼[i].GetComponentInChildren<TMP_Text>().text = $"<color=#FFD99B>{키}</color>\n{기술.이름}";
            스킬칸버튼[i].선택표시(i == 고른칸);
        }
        for (int i = 0; i < 스킬후보버튼.Length; i++)
        {
            var 버튼 = 스킬후보버튼[i];
            bool 표시 = i < 왕직업목록.장착후보수(고른직업);
            버튼.gameObject.SetActive(표시);
            if (!표시) continue;
            버튼.GetComponentInChildren<TMP_Text>().text = 왕직업목록.정보(왕직업목록.후보(고른직업, i)).이름;
            버튼.선택표시(i == 고른후보);
        }
        var 설명 = 왕직업목록.정보(왕직업목록.후보(고른직업, 고른후보));
        장착설명.text = $"<color=#FFD99B>{설명.이름}  ·  쿨다운 {설명.쿨다운}초</color>\n{설명.설명}";
    }

    async void 연결시작()
    {
        if (연결시도 != null || 연결 != null || 이동중) return;
        using var 시도 = new CancellationTokenSource();
        연결시도 = 시도;
        if (!전장입장.서버빌드)
        {
            버튼활성(false);
            취소버튼.gameObject.SetActive(true);
            방상태.text = "전용 서버에 연결하는 중…";
        }
        try
        {
#if DIVINEWAR_DEDICATED
            var 새연결 = await 참가연결.서버열기Async(시도.Token);
#else
            var 새연결 = await 참가연결.참가Async(참가코드.text.Trim(), 시도.Token);
#endif
            if (시도.IsCancellationRequested)
            {
                새연결.Dispose();
                return;
            }
            연결 = 새연결;
            if (연결.전용서버) Debug.Log($"[DivineWar] JOIN_CODE={연결.코드}");
            else 방상태.text = "두 플레이어가 모이면 전투가 시작됩니다.";
        }
        catch (Exception 오류)
        {
            if (시도.IsCancellationRequested) return;
            Debug.LogWarning($"방 연결 실패 ({오류.GetType().Name})");
            방취소(오류안내(오류));
        }
        finally
        {
            if (연결시도 == 시도) 연결시도 = null;
        }
    }

    static string 오류안내(Exception 오류)
    {
        for (var 원인 = 오류; 원인 != null; 원인 = 원인.InnerException)
        {
            if (원인 is LobbyServiceException { Reason: LobbyExceptionReason.LobbyFull }
                || 원인 is SessionException { Error: SessionError.Unknown } && 원인.Message.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
                return "방이 가득 찼습니다.";
            if (원인 is FormatException or SessionException { Error: SessionError.InvalidSessionIdentifier or SessionError.InvalidParameter })
                return "참가 코드를 다시 확인해 주세요.";
            if (원인 is SessionException { Error: SessionError.SessionNotFound or SessionError.SessionDeleted })
                return "방을 찾을 수 없습니다. 코드를 확인해 주세요.";
            if (원인 is InvalidOperationException { Message: "상대와 게임 버전이 다릅니다." }) return 원인.Message;
        }
        return "온라인 방에 연결할 수 없습니다. 네트워크를 확인하고 다시 시도해 주세요.";
    }

    void 방취소(string 문장)
    {
        연결정리();
        시작대기 = 0;
        직업보냄 = false;
        if (전장입장.서버빌드)
        {
            Debug.LogWarning($"[DivineWar] {문장}");
            Array.Clear(참가준비, 0, 참가준비.Length);
            재시도시각 = Time.realtimeSinceStartup + 5f;
            return;
        }
        방상태.text = 문장;
        취소버튼.gameObject.SetActive(false);
        버튼활성(true);
    }

    void 연결정리()
    {
        연결시도?.Cancel();
        연결시도 = null;
        연결?.Dispose();
        연결 = null;
    }

    void 버튼활성(bool 켜짐)
    {
        시작버튼.interactable = 켜짐;
        참가버튼.interactable = 켜짐;
        참가코드.readOnly = !켜짐;
        foreach (var 버튼 in 직업버튼) 버튼.interactable = 켜짐;
        foreach (var 버튼 in 스킬칸버튼) 버튼.interactable = 켜짐;
        foreach (var 버튼 in 스킬후보버튼) 버튼.interactable = 켜짐;
    }
}

