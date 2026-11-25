using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Multiplayer;
using UnityEngine;

public sealed class 참가연결 : IDisposable
{
    const ushort 프로토콜 = 22;
    const string 버전키 = "divinewarProtocol";
    const string 명령채널 = "dw.command";
    const string 상태채널 = "dw.state";
    const string 확인채널 = "dw.ack";
    const int 최대명령길이 = 2048;
    static readonly SemaphoreSlim 세션차례 = new(1, 1);
    static readonly UTF8Encoding 인코딩 = new(false, true);

    sealed class 참가자
    {
        public ulong 번호;
        public uint 보낸순번, 확인순번;
        public float 마지막송신;
        public bool 송신가능 => 보낸순번 == 확인순번;
    }

    readonly NetworkManager 관리자;
    readonly 참가자[] 팀원 = new 참가자[2];
    readonly Queue<(int 팀, string 내용)> 명령함 = new();
    ISession 세션;
    uint 받은순번;
    bool 서버접속, 닫힘;
    string 최신상태;
    Task 종료작업;

    public bool 전용서버 { get; }
    public bool 방주인 { get; }
    public bool 서버권한 => 전용서버 || 방주인;
    public int 원격팀시작 => 방주인 ? 1 : 0;
#if DIVINEWAR_LOCAL_QA
    const string 로컬코드 = "LOCALQA";
    public string 코드 => 로컬코드;
#else
    public string 코드 => 세션?.Code;
#endif
    public string 오류 { get; private set; }
    bool 통신중 => !닫힘 && 오류 == null && 관리자 && 관리자.IsListening;
    public bool 연결됨 => 통신중 && (서버권한
        ? (방주인 || 팀원[0] != null) && 팀원[1] != null
        : 서버접속);

    public bool 상태전송가능
    {
        get
        {
            if (!서버권한 || !연결됨) return false;
            bool 보낼수있음 = false;
            foreach (var 참가자 in 팀원)
            {
                if (참가자 == null) continue;
                if (!참가자.송신가능 && Time.realtimeSinceStartup - 참가자.마지막송신 > 20f)
                {
                    오류 = "참가자의 전장 수신 응답이 없습니다.";
                    return false;
                }
                보낼수있음 |= 참가자.송신가능;
            }
            return 보낼수있음;
        }
    }

    참가연결(NetworkManager 관리자, bool 전용서버, bool 방주인)
    {
        this.관리자 = 관리자;
        this.전용서버 = 전용서버;
        this.방주인 = 방주인;
        관리자.NetworkConfig.ProtocolVersion = 프로토콜;
        관리자.OnClientStarted += 통신등록;
        관리자.OnClientConnectedCallback += 접속;
        관리자.OnClientDisconnectCallback += 해제;
        관리자.OnTransportFailure += 중단;
    }

#if DIVINEWAR_DEDICATED
    public static Task<참가연결> 서버열기Async(CancellationToken 토큰) => 시작(null, false, 토큰);
#endif
    public static Task<참가연결> 방열기Async(CancellationToken 토큰) => 시작(null, true, 토큰);

    public static Task<참가연결> 참가Async(string 코드, CancellationToken 토큰)
    {
        if (string.IsNullOrWhiteSpace(코드)) throw new FormatException("참가 코드를 입력하세요.");
        return 시작(코드.Trim().ToUpperInvariant(), false, 토큰);
    }

    static async Task<참가연결> 시작(string 코드, bool 방주인, CancellationToken 토큰)
    {
        await 세션차례.WaitAsync(토큰);
        참가연결 연결 = null;
        try
        {
            var 관리자 = NetworkManager.Singleton;
            if (!관리자 || 관리자.IsListening)
                throw new InvalidOperationException("전장 네트워크가 준비되지 않았습니다.");
            연결 = new 참가연결(관리자, 코드 == null && !방주인, 방주인);
#if DIVINEWAR_LOCAL_QA
            로컬시험시작(연결, 코드);
#else
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var 옵션 = new InitializationOptions();
#if !DIVINEWAR_PRODUCTION
                string 환경 = Environment.GetEnvironmentVariable("DIVINEWAR_UGS_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(환경)) 옵션.SetEnvironmentName(환경.Trim());
#endif
                string 프로필 = Environment.GetEnvironmentVariable("DIVINEWAR_UGS_PROFILE");
                if (!string.IsNullOrEmpty(프로필)) 옵션.SetProfile(프로필);
                await UnityServices.InitializeAsync(옵션);
            }
            토큰.ThrowIfCancellationRequested();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            토큰.ThrowIfCancellationRequested();

            var 전송 = 관리자.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
            if (!전송) throw new InvalidOperationException("Unity Transport가 필요합니다.");
            전송.UseWebSockets = true;
            var 네트워크 = new NetworkOptions { RelayProtocol = RelayProtocol.WSS };
            if (연결.서버권한)
            {
                var 옵션 = new SessionOptions
                {
                    MaxPlayers = 연결.방주인 ? 2 : 3,
                    IsPrivate = true,
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        [버전키] = new SessionProperty(프로토콜.ToString(), VisibilityPropertyOptions.Member)
                    }
                }.WithRelayNetwork().WithNetworkOptions(네트워크);
                연결.세션 = await MultiplayerService.Instance.CreateSessionAsync(옵션);
            }
            else
            {
                연결.세션 = await MultiplayerService.Instance.JoinSessionByCodeAsync(코드,
                    new JoinSessionOptions().WithNetworkOptions(네트워크));
                if (연결.세션.MaxPlayers is not (2 or 3) || !연결.세션.IsPrivate ||
                    !연결.세션.Properties.TryGetValue(버전키, out var 버전) || 버전.Value != 프로토콜.ToString())
                    throw new InvalidOperationException("상대와 게임 버전이 다릅니다.");
            }
            연결.세션.Deleted += 연결.중단;
            연결.세션.SessionHostChanged += 연결.서버변경;
            연결.세션.RemovedFromSession += 연결.중단;
#endif
            토큰.ThrowIfCancellationRequested();
            return 연결;
        }
        catch
        {
            if (연결 == null) 세션차례.Release();
            else await 연결.닫기Async();
            throw;
        }
    }

#if DIVINEWAR_LOCAL_QA
    static void 로컬시험시작(참가연결 연결, string 코드)
    {
        if (코드 != null && 코드 != 로컬코드) throw new FormatException("로컬 검증용 참가 코드가 잘못되었습니다.");
        ushort 포트 = 17741;
        string 설정 = Environment.GetEnvironmentVariable("DIVINEWAR_QA_PORT");
        if (!string.IsNullOrEmpty(설정) && (!ushort.TryParse(설정, out 포트) || 포트 == 0))
            throw new FormatException("로컬 검증용 포트가 잘못되었습니다.");

        var 전송 = 연결.관리자.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        if (!전송) throw new InvalidOperationException("Unity Transport가 필요합니다.");
        전송.SetConnectionData("127.0.0.1", 포트, "127.0.0.1");
        if (연결.서버권한)
        {
            if (!연결.관리자.StartHost()) throw new InvalidOperationException("로컬 검증 서버를 열 수 없습니다.");
            return;
        }
        if (!연결.관리자.StartClient()) throw new InvalidOperationException("로컬 검증 서버에 접속할 수 없습니다.");
    }
#endif

    void 통신등록()
    {
        var 통신 = 관리자.CustomMessagingManager;
        통신.RegisterNamedMessageHandler(명령채널, 명령수신);
        if (서버권한) 통신.RegisterNamedMessageHandler(확인채널, 확인수신);
        else 통신.RegisterNamedMessageHandler(상태채널, 상태수신);
    }

    void 접속(ulong 번호)
    {
        if (서버권한)
        {
            if (번호 == NetworkManager.ServerClientId || 팀찾기(번호) >= 0) return;
            int 팀 = Array.FindIndex(팀원, 원격팀시작, 참가자 => 참가자 == null);
            if (팀 >= 0 && 오류 == null) 팀원[팀] = new 참가자 { 번호 = 번호 };
            else 관리자.DisconnectClient(번호);
        }
        else if (번호 == 관리자.LocalClientId) 서버접속 = true;
    }

    int 팀찾기(ulong 번호)
    {
        for (int 팀 = 0; 팀 < 팀원.Length; 팀++)
            if (팀원[팀]?.번호 == 번호) return 팀;
        return -1;
    }

    void 해제(ulong 번호)
    {
        if (!서버권한)
        {
            // 다른 참가자의 알림과 내 서버 연결 종료를 구분한다.
            if (번호 == 관리자.LocalClientId || 번호 == NetworkManager.ServerClientId) 중단();
        }
        else if (팀찾기(번호) >= 0) 중단();
    }

    void 서버변경(string _) => 중단();
    void 중단() => 오류 ??= "전장 연결이 끊겼습니다.";

    public bool 보내기(string 내용) => !서버권한 && 연결됨 && 명령전송(NetworkManager.ServerClientId, 내용);

    public bool 팀에보내기(int 팀, string 내용) => 서버권한 && 통신중 &&
        팀 >= 0 && 팀 < 팀원.Length && 팀원[팀] != null && 명령전송(팀원[팀].번호, 내용);

    bool 명령전송(ulong 번호, string 내용)
    {
        if (string.IsNullOrEmpty(내용) || 내용[0] == 'S' || 인코딩.GetByteCount(내용) > 최대명령길이) return false;
        var 바이트 = 인코딩.GetBytes(내용);
        using var 버퍼 = new FastBufferWriter(바이트.Length, Allocator.Temp);
        버퍼.WriteBytesSafe(바이트);
        관리자.CustomMessagingManager.SendNamedMessage(명령채널, 번호, 버퍼, NetworkDelivery.ReliableFragmentedSequenced);
        return true;
    }

    public bool 받기(out int 팀, out string 내용)
    {
        if (명령함.Count > 0)
        {
            (팀, 내용) = 명령함.Dequeue();
            return true;
        }
        팀 = -1;
        내용 = 최신상태;
        최신상태 = null;
        return 내용 != null;
    }

    void 명령수신(ulong 보낸이, FastBufferReader 버퍼)
    {
        int 팀 = 서버권한 ? 팀찾기(보낸이) : -1;
        if (!통신중 || (서버권한 ? 팀 < 0 : 보낸이 != NetworkManager.ServerClientId || !서버접속)) return;
        int 길이 = 버퍼.Length - 버퍼.Position;
        if (길이 < 1 || 길이 > 최대명령길이 || 명령함.Count >= 128)
        {
            오류 = "상대의 명령을 처리할 수 없습니다.";
            return;
        }
        var 바이트 = new byte[길이];
        버퍼.ReadBytesSafe(ref 바이트, 길이);
        try
        {
            string 문장 = 인코딩.GetString(바이트);
            if (문장[0] != 'S') 명령함.Enqueue((팀, 문장));
        }
        catch (DecoderFallbackException) { 오류 = "명령의 문자 형식이 잘못되었습니다."; }
    }

    public void 상태보내기(string 내용)
    {
        if (!상태전송가능) return;
        try
        {
            byte[] 압축 = 전장상태압축.포장(내용);
            int 원본길이 = 인코딩.GetByteCount(내용);
            foreach (var 참가자 in 팀원)
            {
                if (참가자 == null || !참가자.송신가능) continue;
                using var 버퍼 = new FastBufferWriter(8 + 압축.Length, Allocator.Temp);
                버퍼.WriteValueSafe(++참가자.보낸순번);
                버퍼.WriteValueSafe(원본길이);
                버퍼.WriteBytesSafe(압축);
                관리자.CustomMessagingManager.SendNamedMessage(상태채널, 참가자.번호, 버퍼, NetworkDelivery.ReliableFragmentedSequenced);
                참가자.마지막송신 = Time.realtimeSinceStartup;
            }
        }
        catch (InvalidDataException ex) { 오류 = ex.Message; }
    }

    void 상태수신(ulong 보낸이, FastBufferReader 버퍼)
    {
        if (보낸이 != NetworkManager.ServerClientId || !연결됨 || 서버권한) return;
        int 길이 = 버퍼.Length - 버퍼.Position - 8;
        if (길이 <= 0 || 길이 > 전장상태압축.최대압축길이)
        {
            오류 = "전장 상태의 크기가 잘못되었습니다.";
            return;
        }

        버퍼.ReadValueSafe(out uint 순번);
        버퍼.ReadValueSafe(out int 원본길이);
        if (순번 != unchecked(받은순번 + 1)) return;
        var 바이트 = new byte[길이];
        버퍼.ReadBytesSafe(ref 바이트, 길이);
        try
        {
            string 문장 = 전장상태압축.풀기(바이트, 원본길이);
            if (문장[0] != 'S') throw new InvalidDataException();
            최신상태 = 문장;
            받은순번 = 순번;
            using var 응답 = new FastBufferWriter(sizeof(uint), Allocator.Temp);
            응답.WriteValueSafe(순번);
            관리자.CustomMessagingManager.SendNamedMessage(확인채널, 보낸이, 응답);
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is DecoderFallbackException)
        {
            오류 = "전장 상태를 읽을 수 없습니다.";
        }
    }

    void 확인수신(ulong 보낸이, FastBufferReader 버퍼)
    {
        int 팀 = 팀찾기(보낸이);
        if (팀 < 0 || 버퍼.Length - 버퍼.Position != sizeof(uint)) return;
        버퍼.ReadValueSafe(out uint 순번);
        if (순번 == 팀원[팀].보낸순번) 팀원[팀].확인순번 = 순번;
    }

    public void Dispose() => _ = 닫기Async();

    public Task 닫기Async() => 종료작업 ??= 종료();

    async Task 종료()
    {
        닫힘 = true;
        try
        {
            if (관리자)
            {
                관리자.OnClientStarted -= 통신등록;
                관리자.OnClientConnectedCallback -= 접속;
                관리자.OnClientDisconnectCallback -= 해제;
                관리자.OnTransportFailure -= 중단;
                var 통신 = 관리자.CustomMessagingManager;
                통신?.UnregisterNamedMessageHandler(명령채널);
                통신?.UnregisterNamedMessageHandler(서버권한 ? 확인채널 : 상태채널);
            }
            if (세션 != null)
            {
                세션.Deleted -= 중단;
                세션.SessionHostChanged -= 서버변경;
                세션.RemovedFromSession -= 중단;
                if (세션.State != SessionState.Deleted && 세션.IsMember)
                {
                    if (서버권한 && 세션.IsHost) await 세션.AsHost().DeleteAsync();
                    else await 세션.LeaveAsync();
                }
            }
        }
        catch (Exception ex) { Debug.LogWarning($"전장 세션 정리 실패 ({ex.GetType().Name})"); }
        finally
        {
            try
            {
                if (관리자 && 관리자.IsListening && !관리자.ShutdownInProgress) 관리자.Shutdown();
                while (관리자 && 관리자.ShutdownInProgress && Application.isPlaying) await Task.Yield();
            }
            finally { 세션차례.Release(); }
        }
    }
}
