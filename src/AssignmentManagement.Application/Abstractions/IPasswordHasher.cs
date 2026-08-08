namespace AssignmentManagement.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords. Implementations must never store or return plaintext.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted, iterated hash of the given password.</summary>
    string Hash(string password);

    /// <summary>Returns true when the password matches the stored hash.</summary>
    bool Verify(string password, string storedHash);
}
