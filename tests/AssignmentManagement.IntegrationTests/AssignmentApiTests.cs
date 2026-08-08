using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AssignmentManagement.IntegrationTests;

public sealed class AssignmentApiTests : ApiTestBase
{
    public AssignmentApiTests(ApiFixture fixture) : base(fixture)
    {
    }

    private static string NewTitle(string what) => $"{what}-{Guid.NewGuid():N}";

    // ---- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_ByAllocatedTeacher_ReturnsDraftAssignment()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);

        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Algebra"));

        assignment.Status.Should().Be("Draft");
        assignment.TeacherId.Should().Be(Fixture.TeacherId);
        assignment.ClassId.Should().Be(Fixture.ClassId);
        assignment.SubjectId.Should().Be(Fixture.SubjectId);
    }

    [Fact]
    public async Task Create_ByTeacherWithoutAllocation_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);

        var response = await teacher.PostAsJsonAsync("/api/assignments", new
        {
            classId = Fixture.ClassId,
            subjectId = Fixture.SubjectId,
            title = NewTitle("Unallocated"),
            description = "Should be rejected.",
            deadline = DateTimeOffset.UtcNow.AddDays(5),
            maximumMarks = 100,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ByStudent_ReturnsForbidden()
    {
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var response = await student.PostAsJsonAsync("/api/assignments", new
        {
            classId = Fixture.ClassId,
            subjectId = Fixture.SubjectId,
            title = NewTitle("AsStudent"),
            description = "Should be rejected.",
            deadline = DateTimeOffset.UtcNow.AddDays(5),
            maximumMarks = 100,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Update_ByOwner_UpdatesDraft()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Editable"));

        var response = await teacher.PutAsJsonAsync($"/api/assignments/{assignment.Id}", new
        {
            classId = Fixture.ClassId,
            subjectId = Fixture.SubjectId,
            title = "Updated Title",
            description = "Updated description.",
            deadline = DateTimeOffset.UtcNow.AddDays(10),
            maximumMarks = 50,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadAsAsync<AssignmentDto>(response);
        updated.Title.Should().Be("Updated Title");
        updated.MaximumMarks.Should().Be(50);
        updated.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Update_ByAnotherTeacher_ReturnsForbidden()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Owned"));

        var intruder = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);

        var response = await intruder.PutAsJsonAsync($"/api/assignments/{assignment.Id}", new
        {
            classId = Fixture.ClassId,
            subjectId = Fixture.SubjectId,
            title = "Hijacked",
            description = "Should be rejected.",
            deadline = DateTimeOffset.UtcNow.AddDays(10),
            maximumMarks = 100,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_PublishedAssignment_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Immutable"));

        var publish = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);
        publish.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await teacher.PutAsJsonAsync($"/api/assignments/{assignment.Id}", new
        {
            classId = Fixture.ClassId,
            subjectId = Fixture.SubjectId,
            title = "Too Late",
            description = "Should be rejected.",
            deadline = DateTimeOffset.UtcNow.AddDays(10),
            maximumMarks = 100,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Publish --------------------------------------------------------------

    [Fact]
    public async Task Publish_ThenPublishAgain_ReturnsConflictOnSecondPublish()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Publishable"));

        var first = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Publish_ByAnotherTeacher_ReturnsForbidden()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("NotYours"));

        var intruder = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);

        var response = await intruder.PostAsync($"/api/assignments/{assignment.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Delete ---------------------------------------------------------------

    [Fact]
    public async Task Delete_ByOwner_ReturnsNoContent()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Deletable"));

        var response = await teacher.DeleteAsync($"/api/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_PublishedAssignment_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("PublishedLocked"));
        var publish = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);
        publish.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await teacher.DeleteAsync($"/api/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_ByAnotherTeacher_ReturnsForbidden()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("OwnedDelete"));

        var intruder = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);

        var response = await intruder.DeleteAsync($"/api/assignments/{assignment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Visibility -----------------------------------------------------------

    [Fact]
    public async Task Student_SeesOnlyPublishedAssignmentsForOwnClass()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);

        var draft = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("HiddenDraft"));
        var published = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("VisiblePublished"));
        await teacher.PostAsync($"/api/assignments/{published.Id}/publish", null);

        var otherClassPublished = await CreateAssignmentAsync(
            teacher, Fixture.OtherClassId, Fixture.SubjectId, NewTitle("OtherClass"));
        await teacher.PostAsync($"/api/assignments/{otherClassPublished.Id}/publish", null);

        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var response = await student.GetAsync("/api/assignments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignments = await ReadAsAsync<List<AssignmentDto>>(response);
        assignments.Should().Contain(a => a.Id == published.Id);
        assignments.Should().NotContain(a => a.Id == draft.Id);
        assignments.Should().NotContain(a => a.Id == otherClassPublished.Id);
    }

    [Fact]
    public async Task Student_ViewingDraftAssignment_ReturnsForbidden()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var draft = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("DraftSecret"));

        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var response = await student.GetAsync($"/api/assignments/{draft.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_ListSeesOnlyOwnAssignments()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var mine = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("Mine"));

        var otherTeacher = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);
        var response = await otherTeacher.GetAsync("/api/assignments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignments = await ReadAsAsync<List<AssignmentDto>>(response);
        assignments.Should().NotContain(a => a.Id == mine.Id);
    }

    [Fact]
    public async Task Admin_ListSeesAllAssignments()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var mine = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, NewTitle("VisibleToAdmin"));

        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);
        var response = await admin.GetAsync("/api/assignments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assignments = await ReadAsAsync<List<AssignmentDto>>(response);
        assignments.Should().Contain(a => a.Id == mine.Id);
    }
}
