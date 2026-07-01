using System.Security.Cryptography;

namespace SsiSdk.Internal;

internal static class IdGenerator
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int IdSize = 20;

    public static string GenerateRequestId()
    {
        var mask = (1 << (int)Math.Ceiling(Math.Log2(Alphabet.Length))) - 1;
        var step = (int)Math.Ceiling(1.6 * mask * IdSize / (double)Alphabet.Length);

        Span<byte> buffer = stackalloc byte[step];
        Span<char> result = stackalloc char[IdSize];
        var count = 0;

        while (count < IdSize)
        {
            RandomNumberGenerator.Fill(buffer);
            foreach (var b in buffer)
            {
                var idx = b & mask;
                if (idx < Alphabet.Length)
                {
                    result[count++] = Alphabet[idx];
                    if (count == IdSize) break;
                }
            }
        }

        return new string(result);
    }

    public static string TodayDateStr() => DateTime.Now.ToString("yyyy/MM/dd");
    public static string BeginningOfDay() => DateTime.Now.ToString("yyyy/MM/dd") + " 00:00:00";
    public static string EndOfDay() => DateTime.Now.ToString("yyyy/MM/dd") + " 23:59:59";
}
