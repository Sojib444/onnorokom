using System.Security.Cryptography;
using AssignmentManagement.Application.Abstractions;

namespace AssignmentManagement.Infrastructure.Authentication;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hasher (100,000 iterations, 128-bit salt, 256-bit key).
/// This is the same KDF family ASP.NET Core Identity uses, implemented directly so no
/// additional dependency is required. The self-describing hash format keeps a salt and
/// iteration count with every value, which allows the cost factor to be raised later
/// without invalidating existing hashes.
/// </summary>
/// <remarks>
/// Hash format: <c>$pbkdf2-sha256$&lt;iterations&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;</c>
/// Never log or expose hashes; they authenticate users and are sensitive credentials.
/// </remarks>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const string Prefix = "$pbkdf2-sha256$";

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join('$', Prefix.TrimEnd('$'), Iterations,
            Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(storedHash);

        var segments = storedHash.Split('$');

        // Format: "$pbkdf2-sha256$<iterations>$<salt>$<hash>"; splitting on '$' yields a
        // leading empty segment, so the algorithm name lives at index 1.
        if (segments.Length != 5 ||
            segments[0].Length != 0 ||
            !segments[1].Equals("pbkdf2-sha256", StringComparison.Ordinal) ||
            !int.TryParse(segments[2], out var iterations) ||
            iterations <= 0)
        {
            return false;
        }

        var expected = Convert.FromBase64String(segments[4]);
        var salt = Convert.FromBase64String(segments[3]);

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
