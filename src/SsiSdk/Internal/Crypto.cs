using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SsiSdk.Internal;

internal static class Crypto
{
    private static readonly byte[] Sha256DerPrefix =
    [
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86,
        0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05,
        0x00, 0x04, 0x20,
    ];

    /// <summary>RSA PKCS#1 v1.5 / SHA-256 signature, as hex (REST) or base64 (trading WebSocket).</summary>
    public static string Sign(string data, string privateKeyBase64, string encoding = "hex")
    {
        if (encoding != "hex" && encoding != "base64")
            throw new ValidationException($"signature encoding must be 'hex' or 'base64', got {encoding}");
        if (string.IsNullOrWhiteSpace(privateKeyBase64))
            throw new ValidationException("private_key is required to sign this request");

        BigInteger n, d;
        BigInteger? e;
        try
        {
            var xml = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyBase64.Trim()));
            var root = XDocument.Parse(xml).Root ?? throw new FormatException();
            n = ReadInt(root, "Modulus") ?? throw new FormatException();
            d = ReadInt(root, "D") ?? throw new FormatException();
            e = ReadInt(root, "Exponent");
        }
        catch (Exception ex) when (ex is FormatException or XmlException or ArgumentException or DecoderFallbackException)
        {
            throw new ValidationException("private_key is not a valid base64-encoded XML RSA key");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        var digestInfo = new byte[Sha256DerPrefix.Length + hash.Length];
        Buffer.BlockCopy(Sha256DerPrefix, 0, digestInfo, 0, Sha256DerPrefix.Length);
        Buffer.BlockCopy(hash, 0, digestInfo, Sha256DerPrefix.Length, hash.Length);

        var keyLen = (int)((n.GetBitLength() + 7) / 8);
        var padLen = keyLen - digestInfo.Length - 3;
        if (padLen < 8)
            throw new ValidationException("private_key is not a valid base64-encoded XML RSA key");

        var padded = new byte[keyLen];
        padded[1] = 0x01;
        for (var i = 2; i < 2 + padLen; i++) padded[i] = 0xff;
        Buffer.BlockCopy(digestInfo, 0, padded, 3 + padLen, digestInfo.Length);
        var m = new BigInteger(padded, isUnsigned: true, isBigEndian: true);

        BigInteger s;
        if (e is null)
        {
            s = BigInteger.ModPow(m, d, n);
        }
        else
        {
            var r = RandomUnit(n);
            var blinded = (m * BigInteger.ModPow(r, e.Value, n)) % n;
            s = (BigInteger.ModPow(blinded, d, n) * ModInverse(r, n)) % n;
            if (BigInteger.ModPow(s, e.Value, n) != m)
                throw new CryptographicException("signing failed an internal consistency check");
        }

        var raw = s.ToByteArray(isUnsigned: true, isBigEndian: true);
        var sig = new byte[keyLen];
        Buffer.BlockCopy(raw, 0, sig, keyLen - raw.Length, raw.Length);
        return encoding == "hex" ? Convert.ToHexString(sig).ToLowerInvariant() : Convert.ToBase64String(sig);
    }

    private static BigInteger? ReadInt(XElement root, string name)
    {
        var el = root.Element(name);
        if (el is null || string.IsNullOrWhiteSpace(el.Value)) return null;
        return new BigInteger(Convert.FromBase64String(el.Value.Trim()), isUnsigned: true, isBigEndian: true);
    }

    private static BigInteger RandomUnit(BigInteger n)
    {
        var len = (int)((n.GetBitLength() + 7) / 8) + 8;
        var buf = new byte[len];
        while (true)
        {
            RandomNumberGenerator.Fill(buf);
            var r = new BigInteger(buf, isUnsigned: true, isBigEndian: true) % (n - 3) + 2;
            if (BigInteger.GreatestCommonDivisor(r, n).IsOne) return r;
        }
    }

    private static BigInteger ModInverse(BigInteger a, BigInteger n)
    {
        BigInteger t = 0, newT = 1, r = n, newR = a;
        while (!newR.IsZero)
        {
            var q = r / newR;
            (t, newT) = (newT, t - q * newT);
            (r, newR) = (newR, r - q * newR);
        }
        return t < 0 ? t + n : t;
    }
}
