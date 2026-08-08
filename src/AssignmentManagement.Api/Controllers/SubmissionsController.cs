using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Features.Submissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

/// <summary>
/// Submissions. Students submit, revise and view their own; teachers grade, return and
/// view submissions for their own assignments; administrators can view and download.
/// </summary>
[ApiController]
[Authorize]
[Route("api/submissions")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISender _sender;

    public SubmissionsController(ISender sender) => _sender = sender;

    /// <summary>Returns the caller's own submissions, newest first. Students only.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubmissionResponse>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMySubmissionsQuery(), cancellationToken));

    /// <summary>Returns a single submission if the caller may view it.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetSubmissionByIdQuery(id), cancellationToken));

    /// <summary>Submits an answer, optionally with a file attachment. Students only.</summary>
    [HttpPost("/api/assignments/{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Submit(
        Guid assignmentId,
        [FromForm] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateSubmissionCommand(
                assignmentId,
                request.Answer,
                request.File?.FileName,
                request.File?.ContentType,
                request.File?.OpenReadStream()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Updates the caller's answer before the deadline. A new file replaces attachments. Students only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Update(
        Guid id,
        [FromForm] SubmitAnswerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new UpdateSubmissionCommand(
                id,
                request.Answer,
                request.File?.FileName,
                request.File?.ContentType,
                request.File?.OpenReadStream()),
            cancellationToken));

    /// <summary>Grades a submission. The assignment's teacher only.</summary>
    [HttpPost("{id:guid}/grade")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Grade(
        Guid id,
        GradeSubmissionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new GradeSubmissionCommand(id, request.Marks, request.Feedback),
            cancellationToken));

    /// <summary>Returns a submission to the student for revision. The assignment's teacher only.</summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Return(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ReturnSubmissionCommand(id), cancellationToken));

    /// <summary>Downloads a submission attachment, if the caller may access it.</summary>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await _sender.Send(
            new DownloadAttachmentQuery(id, attachmentId),
            cancellationToken);

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }
}

/// <summary>Multipart form body for submitting or updating an answer.</summary>
public sealed class SubmitAnswerRequest
{
    /// <summary>The answer text.</summary>
    [Required]
    public string Answer { get; set; } = string.Empty;

    /// <summary>Optional file attachment.</summary>
    public IFormFile? File { get; set; }
}
