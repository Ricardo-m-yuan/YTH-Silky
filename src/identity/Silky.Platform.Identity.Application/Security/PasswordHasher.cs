using System.Security.Cryptography;
using System.Text;

namespace Silky.Platform.Identity.Application.Security;

/// <summary>
/// PBKDF2 密码摘要。不用明文存储，也不用 MD5/SHA1。
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static (string Hash, string Salt) Hash(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);
        return (Convert.ToBase64String(key), Convert.ToBase64String(saltBytes));
    }

    public static bool Verify(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);
        return CryptographicOperations.FixedTimeEquals(key, Convert.FromBase64String(hash));
    }

    /// <summary>为 SQL 种子脚本生成一对摘要（开发自用）</summary>
    public static string GenerateForSeed(string password)
    {
        var (hash, salt) = Hash(password);
        return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(hash + "|" + salt))}";
    }
}
