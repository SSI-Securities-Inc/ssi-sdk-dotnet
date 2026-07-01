using System.Numerics;
using System.Security.Cryptography;
using System.Text;
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

    public static string Sign(string data, string privateKeyBase64)
    {
        var keyBytes = Convert.FromBase64String(privateKeyBase64);
        var xml = Encoding.UTF8.GetString(keyBytes);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        var modBytes = Convert.FromBase64String(root.Element("Modulus")!.Value);
        var dBytes = Convert.FromBase64String(root.Element("D")!.Value);

        var n = new BigInteger(modBytes, isUnsigned: true, isBigEndian: true);
        var d = new BigInteger(dBytes, isUnsigned: true, isBigEndian: true);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));

        var digestInfo = new byte[Sha256DerPrefix.Length + hash.Length];
        Buffer.BlockCopy(Sha256DerPrefix, 0, digestInfo, 0, Sha256DerPrefix.Length);
        Buffer.BlockCopy(hash, 0, digestInfo, Sha256DerPrefix.Length, hash.Length);

        var keyLen = (n.GetBitLength() + 7) / 8;
        var padLen = (int)keyLen - digestInfo.Length - 3;

        var padded = new byte[keyLen];
        padded[0] = 0x00;
        padded[1] = 0x01;
        for (var i = 2; i < 2 + padLen; i++)
            padded[i] = 0xff;
        padded[2 + padLen] = 0x00;
        Buffer.BlockCopy(digestInfo, 0, padded, 3 + padLen, digestInfo.Length);

        var m = new BigInteger(padded, isUnsigned: true, isBigEndian: true);
        var s = BigInteger.ModPow(m, d, n);

        var sigBytes = s.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (sigBytes.Length < (int)keyLen)
        {
            var full = new byte[keyLen];
            Buffer.BlockCopy(sigBytes, 0, full, (int)keyLen - sigBytes.Length, sigBytes.Length);
            sigBytes = full;
        }

        return Convert.ToHexString(sigBytes).ToLowerInvariant();
    }
}
