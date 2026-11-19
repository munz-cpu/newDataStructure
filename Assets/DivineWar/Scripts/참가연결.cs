using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class 참가연결 : IDisposable
{
    public const int 포트 = 27841;
    const int 최대길이 = 65536;
    readonly CancellationTokenSource 종료 = new();
    readonly ConcurrentQueue<string> 수신함 = new();
    readonly ConcurrentQueue<string> 송신함 = new();
    readonly bool 호스트;
    readonly byte[] 열쇠;
    string 다음상태;
    TcpListener 대기열;
    TcpClient 상대;
    volatile bool 연결중;
    volatile bool 닫힘;
    volatile bool 검증됨;
    volatile string 오류문구;
    Task 실행작업;
    public bool 연결됨 => 검증됨 && !닫힘;
    public string 오류 => 오류문구;
    public string 코드 { get; }

    참가연결(bool 호스트, byte[] 열쇠, IPAddress 주소)
    {
        this.호스트 = 호스트;
        this.열쇠 = 열쇠;
        if (호스트)
        {
            대기열 = new TcpListener(IPAddress.Any, 포트);
            대기열.Start(3);
            var 데이터 = new byte[13];
            데이터[0] = 1;
            주소.GetAddressBytes().CopyTo(데이터, 1);
            열쇠.CopyTo(데이터, 5);
            코드 = Convert.ToBase64String(데이터).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        실행작업 = Task.Run(() => 실행(주소));
    }

    public static 참가연결 방열기(string 외부주소)
    {
        IPAddress 주소;
        if (string.IsNullOrWhiteSpace(외부주소))
        {
            var 후보 = from 장치 in NetworkInterface.GetAllNetworkInterfaces()
                     where 장치.OperationalStatus == OperationalStatus.Up && 장치.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     let 속성 = 장치.GetIPProperties()
                     from 주소정보 in 속성.UnicastAddresses
                     where 주소정보.Address.AddressFamily == AddressFamily.InterNetwork && 주소정보.Address.GetAddressBytes()[0] != 169
                     orderby 속성.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork) descending,
                         장치.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 장치.NetworkInterfaceType == NetworkInterfaceType.Ethernet descending
                     select 주소정보.Address;
            주소 = 후보.FirstOrDefault() ?? IPAddress.Loopback;
        }
        else if (!IPAddress.TryParse(외부주소.Trim(), out 주소) || 주소.AddressFamily != AddressFamily.InterNetwork
                 || 주소.Equals(IPAddress.Any) || 주소.Equals(IPAddress.Broadcast))
            throw new FormatException("연결 주소에는 IPv4 주소를 입력하세요.");
        byte[] 열쇠 = new byte[8];
        using var 난수 = RandomNumberGenerator.Create();
        난수.GetBytes(열쇠);
        return new 참가연결(true, 열쇠, 주소);
    }

    public static 참가연결 참가(string 코드)
    {
        코드 = 코드.Trim();
        if (코드.Length != 18) throw new FormatException("참가 코드 18자를 확인하세요.");
        var 데이터 = Convert.FromBase64String(코드.Replace('-', '+').Replace('_', '/') + "==");
        if (데이터.Length != 13 || 데이터[0] != 1) throw new FormatException("이 버전에서 사용할 수 없는 참가 코드입니다.");
        return new 참가연결(false, 데이터.Skip(5).ToArray(), new IPAddress(데이터.Skip(1).Take(4).ToArray()));
    }

    public bool 받기(out string 내용) => 수신함.TryDequeue(out 내용);

    public void 보내기(string 내용)
    {
        if (!연결됨 || 송신함.Count >= 96) return;
        송신함.Enqueue(내용);
    }

    public void 상태보내기(string 내용)
    {
        if (연결됨) Interlocked.Exchange(ref 다음상태, 내용);
    }

    async Task 실행(IPAddress 주소)
    {
        try
        {
            if (호스트)
            {
                while (!종료.IsCancellationRequested && !검증됨)
                {
                    var 후보 = await 대기열.AcceptTcpClientAsync();
                    상대 = 후보;
                    try
                    {
                        후보.NoDelay = true;
                        var 통로 = 후보.GetStream();
                        using var 제한 = CancellationTokenSource.CreateLinkedTokenSource(종료.Token);
                        제한.CancelAfter(3500);
                        var 인증 = await 읽기(통로, 8, 제한.Token);
                        if (!같은열쇠(인증)) { 후보.Close(); continue; }
                        await 통로.WriteAsync(new byte[] { 1 }, 0, 1, 제한.Token);
                        검증됨 = true;
                    }
                    catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException)
                    {
                        후보.Close();
                        if (종료.IsCancellationRequested) return;
                    }
                }
                대기열.Stop();
            }
            else
            {
                상대 = new TcpClient(AddressFamily.InterNetwork);
                연결중 = true;
                var 연결 = 상대.ConnectAsync(주소, 포트);
                if (await Task.WhenAny(연결, Task.Delay(5500, 종료.Token)) != 연결)
                {
                    _ = 연결.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    throw new IOException();
                }
                await 연결;
                상대.NoDelay = true;
                var 통로 = 상대.GetStream();
                using var 제한 = CancellationTokenSource.CreateLinkedTokenSource(종료.Token);
                제한.CancelAfter(4000);
                await 통로.WriteAsync(열쇠, 0, 열쇠.Length, 제한.Token);
                if ((await 읽기(통로, 1, 제한.Token))[0] != 1) throw new IOException();
                검증됨 = true;
            }
            if (종료.IsCancellationRequested || 상대 == null) return;
            연결중 = false;
            var 스트림 = 상대.GetStream();
            var 읽기작업 = 수신(스트림);
            var 쓰기작업 = 송신(스트림);
            var 먼저 = await Task.WhenAny(읽기작업, 쓰기작업);
            닫힘 = true;
            상대.Close();
            try { await Task.WhenAll(읽기작업, 쓰기작업); }
            catch (Exception) when (먼저.IsCompleted) { }
            if (!종료.IsCancellationRequested) 오류문구 = "상대와의 연결이 끊겼습니다.";
        }
        catch (Exception ex) when (ex is SocketException or IOException or ObjectDisposedException or OperationCanceledException or InvalidOperationException)
        {
            if (!종료.IsCancellationRequested)
                오류문구 = 연결중 ? "방에 연결할 수 없습니다. 참가 코드와 호스트 연결을 확인하세요." : "연결을 유지할 수 없습니다.";
        }
        finally
        {
            닫힘 = true;
            대기열?.Stop();
            상대?.Close();
        }
    }

    bool 같은열쇠(byte[] 인증)
    {
        int 차이 = 0;
        for (int i = 0; i < 열쇠.Length; i++) 차이 |= 열쇠[i] ^ 인증[i];
        return 차이 == 0;
    }

    async Task 수신(NetworkStream 통로)
    {
        var 헤더 = new byte[4];
        while (!종료.IsCancellationRequested)
        {
            await 채우기(통로, 헤더, 종료.Token);
            int 길이 = 헤더[0] << 24 | 헤더[1] << 16 | 헤더[2] << 8 | 헤더[3];
            if (길이 <= 0 || 길이 > (호스트 ? 2048 : 최대길이) || 수신함.Count >= 128) throw new IOException();
            var 바이트 = await 읽기(통로, 길이, 종료.Token);
            수신함.Enqueue(Encoding.UTF8.GetString(바이트));
        }
    }

    async Task 송신(NetworkStream 통로)
    {
        while (!종료.IsCancellationRequested && !닫힘)
        {
            if (!송신함.TryDequeue(out string 내용)) 내용 = Interlocked.Exchange(ref 다음상태, null);
            if (내용 == null)
            {
                await Task.Delay(12, 종료.Token);
                continue;
            }
            var 바이트 = Encoding.UTF8.GetBytes(내용);
            int 길이 = 바이트.Length;
            if (길이 > 최대길이) throw new IOException();
            var 헤더 = new[] { (byte)(길이 >> 24), (byte)(길이 >> 16), (byte)(길이 >> 8), (byte)길이 };
            await 통로.WriteAsync(헤더, 0, 헤더.Length, 종료.Token);
            await 통로.WriteAsync(바이트, 0, 길이, 종료.Token);
        }
    }

    static async Task<byte[]> 읽기(NetworkStream 통로, int 길이, CancellationToken 토큰)
    {
        var 버퍼 = new byte[길이];
        await 채우기(통로, 버퍼, 토큰);
        return 버퍼;
    }

    static async Task 채우기(NetworkStream 통로, byte[] 버퍼, CancellationToken 토큰)
    {
        int 읽은수 = 0;
        while (읽은수 < 버퍼.Length)
        {
            int 수 = await 통로.ReadAsync(버퍼, 읽은수, 버퍼.Length - 읽은수, 토큰);
            if (수 == 0) throw new EndOfStreamException();
            읽은수 += 수;
        }
    }

    public void Dispose()
    {
        if (종료.IsCancellationRequested) return;
        종료.Cancel();
        대기열?.Stop();
        상대?.Close();
        _ = 실행작업.ContinueWith(_ => 종료.Dispose(), TaskScheduler.Default);
    }
}

