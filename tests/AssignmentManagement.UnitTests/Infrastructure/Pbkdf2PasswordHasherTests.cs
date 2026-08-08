using AssignmentManagement.Infrastructure.Authentication;
using FluentAssertions;

namespace AssignmentManagement.UnitTests.Infrastructure;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_produces_self_describing_format()
    {
        var hash = _hasher.Hash("P@ssw0rd!");

        hash.Should().StartWith("$pbkdf2-sha256$100000$");
        hash.Split('$').Should().HaveCount(5);
    }

    [Fact]
    public void Verify_returns_true_for_correct_password()
    {
        var hash = _hasher.Hash("correct horse battery");

        _hasher.Verify("correct horse battery", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _hasher.Hash("correct horse battery");

        _hasher.Verify("wrong password", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$pbkdf2-sha256$0$c2FsdA==$a2V5")]
    [InlineData("$unknown$100000$c2FsdA==$a2V5")]
    public void Verify_returns_false_for_malformed_hashes(string malformed)
    {
        _hasher.Verify("anything", malformed).Should().BeFalse();
    }

    [Fact]
    public void Hash_round_trip_persists_across_instances()
    {
        var hash = _hasher.Hash("S3cure!password");

        new Pbkdf2PasswordHasher().Verify("S3cure!password", hash).Should().BeTrue();
    }
}
