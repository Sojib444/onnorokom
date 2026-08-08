using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AssignmentManagement.IntegrationTests;

public sealed class AuthApiTests : ApiTestBase
{
    public AuthApiTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsBearerToken()
    {
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/auth/login", new { email = ApiFixture.AdminEmail, password = ApiFixture.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await ReadAsAsync<LoginResponseDto>(response);
        login.Token.Should().NotBeNullOrWhiteSpace();
        login.TokenType.Should().Be("Bearer");
        login.User.Email.Should().Be(ApiFixture.AdminEmail);
        login.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/auth/login", new { email = ApiFixture.AdminEmail, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/auth/login", new { email = "nobody@test.dev", password = "Whatever123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.GetAsync("/api/assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
