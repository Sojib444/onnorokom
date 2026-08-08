using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AssignmentManagement.IntegrationTests;

public sealed class AccessControlApiTests : ApiTestBase
{
    public AccessControlApiTests(ApiFixture fixture) : base(fixture)
    {
    }

    // ---- Role-based access ----------------------------------------------------

    [Fact]
    public async Task Anonymous_AccessingUserManagement_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_CanListUsers_ReturnsOk()
    {
        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        var response = await admin.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await ReadAsAsync<List<UserDto>>(response);
        users.Should().Contain(u => u.Email == ApiFixture.AdminEmail);
    }

    [Fact]
    public async Task Teacher_ListingUsers_ReturnsForbidden()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);

        var response = await teacher.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_ListingUsers_ReturnsForbidden()
    {
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var response = await student.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_CreatingUser_ReturnsForbidden()
    {
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var response = await student.PostAsJsonAsync("/api/users", new
        {
            fullName = "Rogue",
            email = "rogue@test.dev",
            password = "Whatever123!",
            role = "Admin",
            classId = (Guid?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanCreateAndDeleteUser_ReturnsNoContent()
    {
        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);
        var email = $"fresh.{Guid.NewGuid():N}@test.dev";

        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Fresh User",
            email,
            password = "FreshPass123!",
            role = "Teacher",
            classId = (Guid?)null,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await ReadAsAsync<UserDto>(create);
        user.Email.Should().Be(email);
        user.Role.Should().Be("Teacher");

        var login = await LoginAsync(email, "FreshPass123!");
        login.User.Id.Should().Be(user.Id);

        var delete = await admin.DeleteAsync($"/api/users/{user.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_CreatingDuplicateEmail_ReturnsConflict()
    {
        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Duplicate",
            email = ApiFixture.TeacherEmail,
            password = "Whatever123!",
            role = "Teacher",
            classId = (Guid?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Admin_DeletingTeacherWithAssignments_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, "Protects-Teacher");

        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);
        var response = await admin.DeleteAsync($"/api/users/{Fixture.TeacherId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Teacher_CanListOwnAllocations()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);

        var response = await teacher.GetAsync("/api/teacher-assignments/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var allocations = await ReadAsAsync<List<TeacherAssignmentDto>>(response);
        allocations.Should().Contain(a => a.ClassId == Fixture.ClassId && a.SubjectId == Fixture.SubjectId);
        allocations.Should().OnlyContain(a => a.TeacherId == Fixture.TeacherId);
    }

    [Fact]
    public async Task Student_ListingTeacherAllocations_ReturnsForbidden()
    {
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var response = await student.GetAsync("/api/teacher-assignments/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanAllocateTeacherToNewClassAndSubject()
    {
        var admin = await AuthenticatedClientAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        var createClass = await admin.PostAsJsonAsync("/api/classes", new
        {
            name = $"Class-{Guid.NewGuid():N}",
            description = "Allocation target.",
        });
        createClass.StatusCode.Should().Be(HttpStatusCode.Created);
        var klass = await ReadAsAsync<ClassDto>(createClass);

        var createSubject = await admin.PostAsJsonAsync("/api/subjects", new
        {
            name = "Physics",
            code = $"PHY-{Guid.NewGuid():N}"[..10],
        });
        createSubject.StatusCode.Should().Be(HttpStatusCode.Created);
        var subject = await ReadAsAsync<SubjectDto>(createSubject);

        var allocate = await admin.PostAsJsonAsync("/api/teacher-assignments", new
        {
            teacherId = Fixture.OtherTeacherId,
            classId = klass.Id,
            subjectId = subject.Id,
        });
        allocate.StatusCode.Should().Be(HttpStatusCode.Created);

        // The newly allocated teacher can now create an assignment for that pair.
        var otherTeacher = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);
        var assignment = await CreateAssignmentAsync(
            otherTeacher, klass.Id, subject.Id, "Now-Allocated");
        assignment.TeacherId.Should().Be(Fixture.OtherTeacherId);
    }
}
