using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssignmentManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.IntegrationTests;

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
}

/// <summary>
/// Base for integration tests. Provides authenticated HTTP clients, JSON helpers and
/// direct database access for seeding state that the public API cannot express (such as
/// an assignment whose deadline has passed).
/// </summary>
[Collection("Api")]
public abstract class ApiTestBase
{
    protected ApiTestBase(ApiFixture fixture) => Fixture = fixture;

    protected ApiFixture Fixture { get; }

    protected HttpClient AnonymousClient => Fixture.Factory.CreateClient();

    protected async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await LoginAsync(email, password);
        var client = Fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }

    protected async Task<LoginResponseDto> LoginAsync(string email, string password)
    {
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return await ReadAsAsync<LoginResponseDto>(response);
    }

    protected async Task<AssignmentDto> CreateAssignmentAsync(
        HttpClient client,
        Guid classId,
        Guid subjectId,
        string title,
        DateTimeOffset? deadline = null)
    {
        var response = await client.PostAsJsonAsync("/api/assignments", new
        {
            classId,
            subjectId,
            title,
            description = "Complete all exercises and show your working.",
            deadline = deadline ?? DateTimeOffset.UtcNow.AddDays(7),
            maximumMarks = 100,
        });

        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        return await ReadAsAsync<AssignmentDto>(response);
    }

    protected async Task<SubmissionDto> SubmitAsync(
        HttpClient client,
        Guid assignmentId,
        string answer)
    {
        var response = await SubmitAnswerAsync(client, assignmentId, answer);
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        return await ReadAsAsync<SubmissionDto>(response);
    }

    /// <summary>
    /// Submits an answer as a multipart form. The submissions endpoints bind
    /// <c>[FromForm]</c>, so plain JSON payloads are rejected with 400.
    /// </summary>
    protected async Task<HttpResponseMessage> SubmitAnswerAsync(
        HttpClient client,
        Guid assignmentId,
        string answer)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(answer), "answer");
        return await client.PostAsync($"/api/assignments/{assignmentId}/submissions", form);
    }

    /// <summary>Updates an answer as a multipart form (see <see cref="SubmitAnswerAsync"/>).</summary>
    protected async Task<HttpResponseMessage> UpdateAnswerAsync(
        HttpClient client,
        Guid submissionId,
        string answer)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(answer), "answer");
        return await client.PutAsync($"/api/submissions/{submissionId}", form);
    }

    protected IServiceScope NewScope() => Fixture.Factory.Services.CreateScope();

    /// <summary>
    /// Moves an assignment's deadline into the past, simulating an assignment that was
    /// created and published on time and simply aged past its deadline.
    /// </summary>
    protected static async Task ExpireDeadlineAsync(AppDbContext db, Guid assignmentId)
    {
        await db.Assignments
            .Where(a => a.Id == assignmentId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(a => a.Deadline, DateTimeOffset.UtcNow.AddMinutes(-5)));
    }

    protected static async Task<T> ReadAsAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<T>(content, options)
            ?? throw new InvalidOperationException($"Response body was empty or not {typeof(T).Name}.");
    }
}
