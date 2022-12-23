using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace mailica.Services;

public static class DovecotHasher
{
    const int KEY_LENGTH = 20;
    const int SALT_LENGTH = 16;
    const int ITERATIONS = 5000;
    static readonly byte[] s_saltChars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(c => Convert.ToByte(c)).ToArray();
    public static bool Verify(string salt, string hash, string password)
    {
        var saltBytes = salt.Select(Convert.ToByte).ToArray();
        var keyBytes = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA1, ITERATIONS, KEY_LENGTH);

        return hash == Convert.ToHexString(keyBytes).ToLower();
    }
    public static (string Salt, string Hash) Hash(string password)
    {
        var saltBytes = new byte[SALT_LENGTH];
        for (int i = 0; i < SALT_LENGTH; i++)
            saltBytes[i] = s_saltChars[RandomNumberGenerator.GetInt32(SALT_LENGTH)];
        var keyBytes = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA1, ITERATIONS, KEY_LENGTH);

        return (Encoding.UTF8.GetString(saltBytes), Convert.ToHexString(keyBytes).ToLower());
    }
    public static string Password(string salt, string hash) => $"{{PBKDF2}}$1${salt}${ITERATIONS}${hash}";
}