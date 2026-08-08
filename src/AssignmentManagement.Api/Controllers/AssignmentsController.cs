using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Features.Assignments;
using AssignmentManagement.Application.Features.Submissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

/// <summary>
/// Assignments. Every authenticated user can list and view; teachers create, edit,
/// publish and delete their own; students only see published assignments for their class.
/// </summary>
[ApiController]
[Authorize]
[Route("api/assignments")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AssignmentsController(ISender sender) => _sender = sender;

    /// <summary>Returns the assignments visible to the caller.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAssignmentsQuery(), cancellationToken));

    /// <summary>Returns a single assignment if the caller may view it.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssignmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAssignmentByIdQuery(id), cancellationToken));

    /// <summary>Creates a draft assignment. Teachers only.</summary>
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> Create(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateAssignmentCommand(
                request.ClassId,
                request.SubjectId,
                request.Title,
                request.Description,
                request.Deadline,
                request.MaximumMarks),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Updates the caller's draft assignment. Teachers only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> Update(
        Guid id,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new UpdateAssignmentCommand(
                id,
                request.ClassId,
                request.SubjectId,
                request.Title,
                request.Description,
                request.Deadline,
                request.MaximumMarks),
            cancellationToken));

    /// <summary>Deletes the caller's draft assignment. Teachers only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteAssignmentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Publishes the caller's draft assignment. Teachers only.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Teacher")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new PublishAssignmentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Returns all submissions for an assignment. The assignment's teacher or an admin.</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SubmissionResponse>>> GetSubmissions(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAssignmentSubmissionsQuery(id), cancellationToken));
}
