using System;
using System.Collections;
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
    public bool 디버그연습전;
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
    [SerializeField] Button 이전직업, 다음직업;
    [SerializeField] 스킬선택버튼[] 스킬칸버튼, 스킬후보버튼;
    [SerializeField] TMP_Text 장착설명;
    [SerializeField] Button 시작버튼, 디버그시작버튼, 방생성버튼, 참가버튼, 취소버튼, 멀티시작버튼;
    [SerializeField] TMP_Text 직업정보, 방상태;
    [SerializeField] 타이틀능력치 직업능력치;
    [SerializeField] TMP_InputField 참가코드;
    [SerializeField] NetworkManager 통신관리자;
    [SerializeField] AudioSource 배경음악재생기, 효과음재생기;
    [SerializeField] AudioSource 배경음악보조재생기;
    [SerializeField] AudioClip[] 타이틀음악;
    배경음악재생 음악;
    [SerializeField] AudioClip 클릭효과음, 직업교체효과음, 스킬장착효과음;

    Coroutine 등장, 아바타전환;
    Sprite[] 아바타대기;
    float 아바타대기시작, 아바타옆이동;
    장착기술? 마지막호버기술;
    Vector2 카드위치;
    참가연결 연결;
    const string 직업저장키 = "DivineWar.SelectedJob.V1";
    왕직업 고른직업 = 왕직업.지휘관;
    int 장착구성, 고른칸;
    static string 장착저장키(왕직업 직업) => $"DivineWar.Loadout.V1.{(int)직업}";
    CancellationTokenSource 연결시도;
    bool 이동중, 직업보냄, 연결안내완료;
    readonly (왕직업 직업, int 장착)?[] 참가준비 = new (왕직업, int)?[2];
    float 시작대기, 재시도시각;

    void Awake()
    {
        if (NetworkManager.Singleton == null) 통신관리자.gameObject.SetActive(true);
        if (전장입장.서버빌드)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 50;
            메뉴카드.transform.parent.gameObject.SetActive(false);
            return;
        }
        카드위치 = ((RectTransform)메뉴카드.transform).anchoredPosition;
        if (클릭효과음 != null) 클릭효과음.LoadAudioData();
        if (직업교체효과음 != null) 직업교체효과음.LoadAudioData();
        if (스킬장착효과음 != null) 스킬장착효과음.LoadAudioData();
        이전직업.onClick.AddListener(() => 직업넘기기(-1));
        다음직업.onClick.AddListener(() => 직업넘기기(1));
        for (int i = 0; i < 스킬칸버튼.Length; i++)
        {
            int 칸 = i;
            스킬칸버튼[i].onClick.AddListener(() =>
            {
                효과음재생(클릭효과음);
                고른칸 = 칸;
                장착갱신();
            });
            스킬칸버튼[i].호버시작 += () => 스킬호버(왕직업목록.후보번호(장착구성, 칸));
        }
        for (int i = 0; i < 스킬후보버튼.Length; i++)
        {
            int 후보 = i;
            스킬후보버튼[i].onClick.AddListener(() => 스킬장착(후보));
            스킬후보버튼[i].호버시작 += () => 스킬호버(후보);
        }
        시작버튼.onClick.AddListener(() => 연습전시작(디버그: false));
        if (디버그시작버튼 == null)
            디버그시작버튼 = 메뉴카드.transform.Find("디버그 플레이")?.GetComponent<Button>();
        if (디버그시작버튼 != null)
            디버그시작버튼.onClick.AddListener(() => 연습전시작(디버그: true));
        방생성버튼.onClick.AddListener(() =>
        {
            효과음재생(클릭효과음);
            연결시작(방생성: true);
        });
        참가버튼.onClick.AddListener(() =>
        {
            효과음재생(클릭효과음);
            연결시작();
        });
        취소버튼.onClick.AddListener(() =>
        {
            효과음재생(클릭효과음);
            방취소("");
        });
        if (멀티시작버튼 != null) 멀티시작버튼.onClick.AddListener(멀티전투시작);
    }

    void OnEnable()
    {
        if (!Application.isEditor) Application.runInBackground = true;
        if (전장입장.서버빌드) return;
        if (효과음재생기 != null) 효과음재생기.volume = 전투음량설정.효과음;
        if (배경음악재생기 != null && 배경음악보조재생기 != null)
            음악 = new 배경음악재생(배경음악재생기, 배경음악보조재생기);
        등장 = StartCoroutine(메뉴등장.재생((RectTransform)메뉴카드.transform, 메뉴카드, 카드위치));
        var 저장직업 = (왕직업)PlayerPrefs.GetInt(직업저장키, (int)왕직업.지휘관);
        직업고르기(왕직업목록.장착가능(저장직업) ? 저장직업 : 왕직업.지휘관);
        방취소("");
    }

    void OnDisable()
    {
        음악?.정지();
        음악 = null;
        if (효과음재생기 != null) 효과음재생기.Stop();
        if (등장 != null) StopCoroutine(등장);
        아바타전환정리();
        연결정리();
    }

    void Update()
    {
        if (이동중) return;
        음악?.갱신(타이틀음악, Time.unscaledDeltaTime);
        if (전장입장.서버빌드)
        {
            if (연결 == null && 연결시도 == null && Time.realtimeSinceStartup >= 재시도시각) 연결시작();
        }
        else 아바타갱신();
        if (연결 == null) return;
        if (연결.오류 != null) { 방취소(연결.오류); return; }
        if (연결.서버권한)
        {
            서버준비갱신();
            return;
        }
        if (연결.연결됨)
        {
            if (!연결안내완료)
            {
                연결안내완료 = true;
                방상태.text = "방에 입장했습니다. 방장이 시작할 때까지 직업과 스킬을 바꿀 수 있습니다.";
            }
            if (!직업보냄) 직업보냄 = 연결.보내기("J" + 준비부호((고른직업, 장착구성)));
        }
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
        bool 변경 = false;
        while (연결.받기(out int 팀, out string 문장))
        {
            if (팀 is 0 or 1 && 문장.Length >= 2 && 문장[0] == 'J' && 준비읽기(문장.Substring(1), out var 선택직업))
            {
                참가준비[팀] = 선택직업;
                변경 = true;
            }
        }
        if (!연결.연결됨)
        {
            if (연결.방주인 && 멀티시작버튼 != null) 멀티시작버튼.interactable = false;
            return;
        }
        if (연결.방주인)
        {
            참가준비[0] = (고른직업, 장착구성);
            bool 준비완료 = 참가준비[1].HasValue;
            if (멀티시작버튼 != null) 멀티시작버튼.interactable = 준비완료;
            if (준비완료 && (변경 || !연결안내완료))
            {
                연결안내완료 = true;
                방상태.text = $"참가 코드 {연결.코드} · 상대 준비 완료 ({참가준비[1].Value.직업}) · 대전 시작을 누르세요.";
            }
            return;
        }
        if (!참가준비[0].HasValue || !참가준비[1].HasValue)
        {
            if ((시작대기 += Time.unscaledDeltaTime) > 30f) 방취소("플레이어의 전장 준비 시간이 초과됐습니다.");
            return;
        }
        전장시작전송();
    }

    void 멀티전투시작()
    {
        if (연결 is not { 방주인: true, 연결됨: true } || !참가준비[1].HasValue) return;
        효과음재생(클릭효과음);
        참가준비[0] = (고른직업, 장착구성);
        전장시작전송();
    }

    void 전장시작전송()
    {
        var 청록 = 참가준비[0].Value;
        var 적색 = 참가준비[1].Value;
        for (int 팀 = 연결.원격팀시작; 팀 < 참가준비.Length; 팀++)
        {
            if (연결.팀에보내기(팀, $"R,{팀},{준비부호(청록)},{준비부호(적색)}")) continue;
            방취소("전장 시작 정보를 전달하지 못했습니다.");
            return;
        }
        전장이동(연결.방주인 ? 0 : -1, 청록, 적색);
    }

    void 연습전시작(bool 디버그)
    {
        if (연결 != null || 연결시도 != null) return;
        var 직업들 = 왕직업목록.선택;
        var 상대 = 직업들[UnityEngine.Random.Range(0, 직업들.Length)];
        전장이동(0, (고른직업, 장착구성), (상대, 왕직업목록.기본장착), 디버그연습전: 디버그);
    }

    void 전장이동(int 팀, (왕직업 직업, int 장착) 청록, (왕직업 직업, int 장착) 적색, bool 디버그연습전 = false)
    {
        if (이동중) return;
        이동중 = true;
        전장입장.대기 = new 전장입장
        {
            연결 = 연결, 청록직업 = 청록.직업, 적색직업 = 적색.직업,
            청록장착 = 청록.장착, 적색장착 = 적색.장착, 내팀 = 팀,
            디버그연습전 = 디버그연습전 && 연결 == null
        };
        연결 = null;
        SceneManager.LoadScene("Main");
    }

    void 직업넘기기(int 방향)
    {
        var 선택 = 왕직업목록.선택;
        int 번호 = Array.IndexOf(선택, 고른직업);
        var 직업 = 선택[(번호 + 방향 + 선택.Length) % 선택.Length];
        효과음재생(직업교체효과음);
        직업화면갱신(직업, 차오르기: true);
        if (타이틀문장 == null) return;
        if (아바타전환 != null) StopCoroutine(아바타전환);
        아바타전환 = StartCoroutine(아바타넘기기(직업, 방향));
    }

    void 직업고르기(왕직업 직업)
    {
        직업화면갱신(직업);
        아바타전환정리();
        아바타외형갱신(직업);
    }

    void 직업화면갱신(왕직업 직업, bool 차오르기 = false)
    {
        고른직업 = 직업;
        고른칸 = 0;
        마지막호버기술 = null;
        PlayerPrefs.SetInt(직업저장키, (int)직업);
        장착구성 = 왕직업목록.장착정리(직업, PlayerPrefs.GetInt(장착저장키(직업), 왕직업목록.기본장착));
        직업정보.text = 직업.ToString();
        var 능력치 = 전장규칙.능력치(대상종류.왕, 직업);
        직업능력치.표시(능력치.체력, 능력치.힘, 능력치.민첩, 차오르기);
        장착갱신();
        준비갱신전달();
    }

    void 아바타외형갱신(왕직업 직업)
    {
        if (타이틀문장 == null) return;
        아바타대기 = 왕외형목록.가져오기()?.표시외형(직업)?.대기;
        아바타대기시작 = Time.unscaledTime;
        타이틀문장.sprite = 아바타대기?.Length == 120 ? 아바타대기[30] : null;
    }

    void 아바타갱신()
    {
        if (타이틀문장 == null) return;
        if (타이틀카메라 != null)
            타이틀문장.transform.position = 타이틀카메라.ViewportToWorldPoint(new Vector3(.75f + 아바타옆이동, .47f, 10));
        if (아바타대기?.Length == 120)
        {
            int 프레임 = (int)((Time.unscaledTime - 아바타대기시작) * 12.5f) % 15;
            타이틀문장.sprite = 아바타대기[30 + 프레임];
        }
    }

    IEnumerator 아바타넘기기(왕직업 직업, int 방향)
    {
        const float 간격 = .045f;
        yield return 아바타전환구간(.13f, 0, -방향 * 간격);
        아바타외형갱신(직업);
        아바타옆이동 = 방향 * 간격;
        yield return 아바타전환구간(.23f, 1, 0);
        아바타전환 = null;
    }

    IEnumerator 아바타전환구간(float 길이, float 목표알파, float 목표이동)
    {
        if (타이틀문장 == null) yield break;
        float 시작알파 = 타이틀문장.color.a;
        float 시작이동 = 아바타옆이동;
        float 시작시각 = Time.unscaledTime;
        for (float 경과 = 0; 경과 < 길이; 경과 = Time.unscaledTime - 시작시각)
        {
            if (타이틀문장 == null) yield break;
            아바타알파(Mathf.SmoothStep(시작알파, 목표알파, 경과 / 길이));
            아바타옆이동 = Mathf.SmoothStep(시작이동, 목표이동, 경과 / 길이);
            yield return null;
        }
        아바타알파(목표알파);
        아바타옆이동 = 목표이동;
    }

    void 아바타알파(float 알파)
    {
        if (타이틀문장 == null) return;
        var 색 = 타이틀문장.color;
        색.a = 알파;
        타이틀문장.color = 색;
    }

    void 아바타전환정리()
    {
        if (아바타전환 != null) StopCoroutine(아바타전환);
        아바타전환 = null;
        아바타옆이동 = 0;
        아바타알파(1);
    }

    void 스킬장착(int 후보)
    {
        if ((uint)후보 >= 왕직업목록.장착후보수(고른직업)) return;
        int 변경구성 = 왕직업목록.장착변경(장착구성, 고른칸, 후보);
        효과음재생(변경구성 == 장착구성 ? 클릭효과음 : 스킬장착효과음);
        장착구성 = 변경구성;
        PlayerPrefs.SetInt(장착저장키(고른직업), 장착구성);
        PlayerPrefs.Save();
        장착갱신();
        준비갱신전달();
    }

    void 준비갱신전달()
    {
        if (연결 == null) return;
        if (연결.방주인) 참가준비[0] = (고른직업, 장착구성);
        else if (!연결.서버권한) 직업보냄 = false;
    }

    void 효과음재생(AudioClip 소리)
    {
        if (효과음재생기 != null && 소리 != null) 효과음재생기.PlayOneShot(소리);
    }

    void 장착갱신()
    {
        int 고른후보 = 왕직업목록.후보번호(장착구성, 고른칸);
        int 장착번호들 = 0;
        for (int i = 0; i < 스킬칸버튼.Length; i++)
        {
            int 후보 = 왕직업목록.후보번호(장착구성, i);
            var 기술 = 왕직업목록.후보(고른직업, 후보);
            장착번호들 |= 1 << 후보;
            스킬칸버튼[i].표시(왕스킬아이콘목록.가져오기(기술), true, i == 고른칸);
        }
        for (int i = 0; i < 스킬후보버튼.Length; i++)
        {
            var 버튼 = 스킬후보버튼[i];
            bool 표시 = i < 왕직업목록.장착후보수(고른직업);
            버튼.gameObject.SetActive(표시);
            if (!표시) continue;
            var 기술 = 왕직업목록.후보(고른직업, i);
            버튼.표시(왕스킬아이콘목록.가져오기(기술), (장착번호들 & (1 << i)) != 0, i == 고른후보);
        }
        스킬설명표시(마지막호버기술 ?? 왕직업목록.후보(고른직업, 고른후보));
    }

    void 스킬호버(int 후보)
    {
        var 기술 = 왕직업목록.후보(고른직업, 후보);
        마지막호버기술 = 기술;
        스킬설명표시(기술);
    }

    void 스킬설명표시(장착기술 기술)
    {
        var 설명 = 왕직업목록.정보(기술);
        var 대기 = 설명.쿨다운 > 0 ? $"  ·  재사용 대기 시간 {설명.쿨다운}초" : "";
        장착설명.text = $"<color=#FFD99B>{설명.이름}{대기}</color>\n{설명.설명}";
    }

    async void 연결시작(bool 방생성 = false)
    {
        if (연결시도 != null || 연결 != null || 이동중) return;
        using var 시도 = new CancellationTokenSource();
        연결시도 = 시도;
        if (!전장입장.서버빌드)
        {
            Application.runInBackground = true;
            버튼활성(false);
            취소버튼.gameObject.SetActive(true);
            방상태.text = 방생성 ? "방을 생성하는 중…" : "방에 연결하는 중…";
        }
        try
        {
#if DIVINEWAR_DEDICATED
            var 새연결 = await 참가연결.서버열기Async(시도.Token);
#else
            var 새연결 = 방생성
                ? await 참가연결.방열기Async(시도.Token)
                : await 참가연결.참가Async(참가코드.text, 시도.Token);
#endif
            if (시도.IsCancellationRequested)
            {
                새연결.Dispose();
                return;
            }
            연결 = 새연결;
            if (연결.전용서버) Debug.Log($"[DivineWar] JOIN_CODE={연결.코드}");
            else if (연결.방주인)
            {
                참가준비[0] = (고른직업, 장착구성);
                참가코드.text = 연결.코드;
                if (멀티시작버튼 != null)
                {
                    멀티시작버튼.gameObject.SetActive(true);
                    멀티시작버튼.interactable = false;
                }
                방상태.text = $"참가 코드 {연결.코드} · 상대를 기다리는 중…";
            }
            else 방상태.text = "방에 연결되었습니다. 잠시 후 입장합니다…";
        }
        catch (Exception 오류)
        {
            if (시도.IsCancellationRequested) return;
            Debug.LogWarning($"방 연결 실패 ({오류.GetType().Name})");
            방취소(방생성 ? "방을 생성할 수 없습니다. 연결을 확인하고 다시 시도해 주세요." : 오류안내(오류));
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
        if (연결?.방주인 == true) 참가코드.text = "";
        연결정리();
        시작대기 = 0;
        직업보냄 = false;
        연결안내완료 = false;
        Array.Clear(참가준비, 0, 참가준비.Length);
        if (전장입장.서버빌드)
        {
            Debug.LogWarning($"[DivineWar] {문장}");
            재시도시각 = Time.realtimeSinceStartup + 5f;
            return;
        }
        방상태.text = 문장;
        취소버튼.gameObject.SetActive(false);
        if (멀티시작버튼 != null)
        {
            멀티시작버튼.gameObject.SetActive(false);
            멀티시작버튼.interactable = false;
        }
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
        if (디버그시작버튼 != null) 디버그시작버튼.interactable = 켜짐;
        방생성버튼.interactable = 켜짐;
        참가버튼.interactable = 켜짐;
        참가코드.readOnly = !켜짐;
    }
}

