using System.Security.Claims;
using AssignmentManagement.Application.Abstractions;
using AssignmentManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AssignmentManagement.Infrastructure.Authentication;

/// <summary>
/// Reads the authenticated identity from the current HTTP context. The identity always
/// originates from the validated JWT, so application code can trust it without ever
/// accepting identity values from the request body.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User;
    }

    public Guid? UserId => TryParseGuid(_principal?.FindFirstValue(ClaimTypes.NameIdentifier));

    public string? Email => _principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(_principal?.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : null;

    public Guid? ClassId => TryParseGuid(_principal?.FindFirstValue("classId"));

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
