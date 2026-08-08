using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.IntegrationTests;

public sealed class SubmissionApiTests : ApiTestBase
{
    public SubmissionApiTests(ApiFixture fixture) : base(fixture)
    {
    }

    private async Task<(HttpClient Teacher, AssignmentDto Assignment)> PublishedAssignmentForMainClassAsync(
        string title)
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var assignment = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, title);
        var publish = await teacher.PostAsync($"/api/assignments/{assignment.Id}/publish", null);
        publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        return (teacher, assignment);
    }

    // ---- Submit ---------------------------------------------------------------

    [Fact]
    public async Task Student_SubmitsAnswer_ReturnsCreatedSubmission()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Submit-Me");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var submission = await SubmitAsync(student, assignment.Id, "x = 4, verified by substitution.");

        submission.AssignmentId.Should().Be(assignment.Id);
        submission.StudentId.Should().Be(Fixture.StudentId);
        submission.Status.Should().Be("Submitted");
    }

    [Fact]
    public async Task Student_SubmitsTwice_ReturnsConflict()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Submit-Twice");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        await SubmitAsync(student, assignment.Id, "First attempt.");

        var response = await SubmitAnswerAsync(student, assignment.Id, "Second attempt.");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Student_SubmitsToDraft_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var draft = await CreateAssignmentAsync(
            teacher, Fixture.ClassId, Fixture.SubjectId, "Submit-To-Draft");

        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var response = await SubmitAnswerAsync(student, draft.Id, "Too early.");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Student_SubmitsToOtherClassAssignment_ReturnsConflict()
    {
        var teacher = await AuthenticatedClientAsync(ApiFixture.TeacherEmail, ApiFixture.TeacherPassword);
        var otherClass = await CreateAssignmentAsync(
            teacher, Fixture.OtherClassId, Fixture.SubjectId, "Submit-Other-Class");
        await teacher.PostAsync($"/api/assignments/{otherClass.Id}/publish", null);

        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var response = await SubmitAnswerAsync(student, otherClass.Id, "Wrong class.");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Student_SubmitsAfterDeadline_ReturnsConflict()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Submit-Late");
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ExpireDeadlineAsync(db, assignment.Id);

        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var response = await SubmitAnswerAsync(student, assignment.Id, "Missed it.");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Student_UpdatesOwnAnswerBeforeDeadline_ReturnsUpdatedSubmission()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Update-Me");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "Original answer.");

        var response = await UpdateAnswerAsync(student, submission.Id, "Revised answer.");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadAsAsync<SubmissionDto>(response);
        updated.Answer.Should().Be("Revised answer.");
        updated.Status.Should().Be("Submitted");
    }

    [Fact]
    public async Task Student_UpdatesSomeoneElsesSubmission_ReturnsForbidden()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Update-Others");
        var first = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(first, assignment.Id, "My answer.");

        var second = await AuthenticatedClientAsync(
            ApiFixture.SecondStudentEmail, ApiFixture.SecondStudentPassword);
        var response = await UpdateAnswerAsync(second, submission.Id, "Not mine.");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_UpdatesAfterDeadline_ReturnsConflict()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Update-Late");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "On-time answer.");

        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ExpireDeadlineAsync(db, assignment.Id);

        var response = await UpdateAnswerAsync(student, submission.Id, "Late revision.");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ---- Grading --------------------------------------------------------------

    [Fact]
    public async Task Teacher_GradesSubmission_ReturnsGradedSubmission()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("Grade-Me");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "My working.");

        var response = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = 85.5m, feedback = "Well structured." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var graded = await ReadAsAsync<SubmissionDto>(response);
        graded.Marks.Should().Be(85.5m);
        graded.Feedback.Should().Be("Well structured.");
        graded.Status.Should().Be("Graded");
    }

    [Fact]
    public async Task Teacher_GradesAboveMaximumMarks_ReturnsConflict()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("Grade-High");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "Working.");

        var response = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = 101m });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Teacher_GradesWithNegativeMarks_ReturnsBadRequest()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("Grade-Negative");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "Working.");

        var response = await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = -1m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnauthorizedTeacher_GradesSubmission_ReturnsForbidden()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Grade-Intruder");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "Working.");

        var intruder = await AuthenticatedClientAsync(
            ApiFixture.OtherTeacherEmail, ApiFixture.OtherTeacherPassword);
        var response = await intruder.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = 90m });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_GradesSubmission_ReturnsForbidden()
    {
        var (_, assignment) = await PublishedAssignmentForMainClassAsync("Grade-ByStudent");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "Working.");

        var response = await student.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = 50m });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_ReturnsSubmissionForRevision_ClearsMarks()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("Return-Me");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        var submission = await SubmitAsync(student, assignment.Id, "First draft.");
        await teacher.PostAsJsonAsync(
            $"/api/submissions/{submission.Id}/grade", new { marks = 60m, feedback = "Try again." });

        var response = await teacher.PostAsync(
            $"/api/submissions/{submission.Id}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returned = await ReadAsAsync<SubmissionDto>(response);
        returned.Status.Should().Be("Returned");
        returned.Marks.Should().BeNull();
    }

    // ---- Listing --------------------------------------------------------------

    [Fact]
    public async Task Teacher_ListsSubmissionsForOwnAssignment()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("List-Submissions");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);
        await SubmitAsync(student, assignment.Id, "Answer one.");

        var second = await AuthenticatedClientAsync(
            ApiFixture.SecondStudentEmail, ApiFixture.SecondStudentPassword);
        await SubmitAsync(second, assignment.Id, "Answer two.");

        var response = await teacher.GetAsync($"/api/assignments/{assignment.Id}/submissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var submissions = await ReadAsAsync<List<SubmissionDto>>(response);
        submissions.Should().HaveCount(2);
    }

    // ---- Attachments ----------------------------------------------------------

    [Fact]
    public async Task Student_SubmitsWithAttachment_TeacherDownloadsFile()
    {
        var (teacher, assignment) = await PublishedAssignmentForMainClassAsync("Attach-File");
        var student = await AuthenticatedClientAsync(ApiFixture.StudentEmail, ApiFixture.StudentPassword);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Answer with attachment."), "answer");
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "solution.pdf");

        var submit = await student.PostAsync(
            $"/api/assignments/{assignment.Id}/submissions", form);
        submit.StatusCode.Should().Be(HttpStatusCode.Created);
        var submission = await ReadAsAsync<SubmissionDto>(submit);
        submission.Attachments.Should().ContainSingle().Which.FileName.Should().Be("solution.pdf");

        var attachment = submission.Attachments.Single();

        var download = await teacher.GetAsync(
            $"/api/submissions/{submission.Id}/attachments/{attachment.Id}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var downloaded = await download.Content.ReadAsByteArrayAsync();
        downloaded.Should().Equal(bytes);
    }
}
