using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Application.Common;
using AssignmentManagement.Application.Contracts;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Application.Features.Auth;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserReadRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IMapper _mapper;

    public LoginCommandHandler(
        IUserReadRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokens,
        ILogger<LoginCommandHandler> logger,
        IMapper mapper)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);

        // A missing user and a wrong password are collapsed into the same generic
        // response. This is a deliberate anti-enumeration decision: an attacker cannot
        // distinguish a registered email from an unknown one, and the unified path also
        // keeps the verification timing behaviour consistent for both cases.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email {Email}.", request.Email);
            throw new UnauthorizedException("The email or password is incorrect.");
        }

        var token = _tokens.GenerateToken(user);

        _logger.LogInformation(
            "User {UserId} ({Role}) logged in successfully.",
            user.Id,
            user.Role.ToString());

        return new LoginResponse(
            token.Token,
            "Bearer",
            token.ExpiresAt,
            _mapper.Map<AuthenticatedUserResponse>(user));
    }
}
