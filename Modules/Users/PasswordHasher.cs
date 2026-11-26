using System.Security.Cryptography;

namespace SkillIssue.Modules.Users;

public static class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(storedHash);

        string[] parts = storedHash.Split('$');

        if (
            parts.Length != 4
            || parts[0] != Algorithm
            || !int.TryParse(parts[1], out int iterations)
            || iterations <= 0
            || iterations > 1_000_000
        )
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        byte[] expectedHash = new byte[HashSize];

        if (
            !Convert.TryFromBase64String(parts[2], salt, out int saltLength)
            || saltLength != SaltSize
            || !Convert.TryFromBase64String(parts[3], expectedHash, out int hashLength)
            || hashLength != HashSize
        )
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
