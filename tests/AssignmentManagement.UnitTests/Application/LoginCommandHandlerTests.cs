using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Features.Auth;
using AssignmentManagement.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static AssignmentManagement.UnitTests.Application.TestData;

namespace AssignmentManagement.UnitTests.Application;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IUserReadRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _tokens = new();
    private readonly IMapper _mapper = CreateMapper();

    private static readonly DateTimeOffset Expiry = DateTimeOffset.UtcNow.AddHours(1);

    private LoginCommandHandler Handler() =>
        new(_users.Object, _passwordHasher.Object, _tokens.Object,
            NullLogger<LoginCommandHandler>.Instance, _mapper);

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndIdentity()
    {
        var user = ATeacher("Rafiq Ahmed", "teacher@school.edu");
        _users.Setup(r => r.GetByEmailAsync("teacher@school.edu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("secret123", user.PasswordHash)).Returns(true);
        _tokens.Setup(t => t.GenerateToken(user))
            .Returns(new TokenResult("signed-token", Expiry));

        var result = await Handler().Handle(
            new LoginCommand("teacher@school.edu", "secret123"), CancellationToken.None);

        result.Token.Should().Be("signed-token");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresAt.Should().Be(Expiry);
        result.User.Id.Should().Be(user.Id);
        result.User.FullName.Should().Be("Rafiq Ahmed");
        result.User.Role.Should().Be("Teacher");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ThrowsUnauthorized()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => Handler().Handle(
            new LoginCommand("nobody@school.edu", "secret123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*incorrect*");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var user = ATeacher();
        _users.Setup(r => r.GetByEmailAsync(user.Email.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var act = () => Handler().Handle(
            new LoginCommand(user.Email.Value, "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
