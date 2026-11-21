using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class 전장상태압축
{
    public const int 최대원본길이 = 262144;
    public const int 최대압축길이 = 최대원본길이 + 1024;
    static readonly UTF8Encoding 인코딩 = new(false, true);

    public static byte[] 포장(string 내용)
    {
        if (string.IsNullOrEmpty(내용) || 인코딩.GetByteCount(내용) > 최대원본길이)
            throw new InvalidDataException("전장 상태 크기가 전송 범위를 벗어났습니다.");
        var 원본 = 인코딩.GetBytes(내용);
        using var 출력 = new MemoryStream();
        using (var 압축 = new DeflateStream(출력, CompressionLevel.Fastest, true))
            압축.Write(원본, 0, 원본.Length);
        if (출력.Length > 최대압축길이) throw new InvalidDataException("압축된 전장 상태가 너무 큽니다.");
        return 출력.ToArray();
    }

    public static string 풀기(byte[] 압축, int 원본길이)
    {
        if (압축 == null || 압축.Length == 0 || 압축.Length > 최대압축길이 ||
            원본길이 <= 0 || 원본길이 > 최대원본길이)
            throw new InvalidDataException("전장 상태 크기를 확인할 수 없습니다.");

        var 원본 = new byte[원본길이];
        using var 입력 = new MemoryStream(압축, false);
        using var 해제 = new DeflateStream(입력, CompressionMode.Decompress);
        for (int 위치 = 0; 위치 < 원본길이;)
        {
            int 개수 = 해제.Read(원본, 위치, 원본길이 - 위치);
            if (개수 == 0) throw new InvalidDataException("전장 상태가 중간에 끊겼습니다.");
            위치 += 개수;
        }
        if (해제.ReadByte() != -1) throw new InvalidDataException("전장 상태 길이가 일치하지 않습니다.");
        return 인코딩.GetString(원본);
    }
}
