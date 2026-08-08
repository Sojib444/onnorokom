using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Features.TeacherAssignments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

/// <summary>Teacher allocations. Most endpoints require the Admin role; teachers can read their own.</summary>
[ApiController]
[Authorize]
[Route("api/teacher-assignments")]
public sealed class TeacherAssignmentsController : ControllerBase
{
    private readonly ISender _sender;

    public TeacherAssignmentsController(ISender sender) => _sender = sender;

    /// <summary>Returns all teacher allocations with resolved names. Administrators only.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetTeacherAssignmentsQuery(), cancellationToken));

    /// <summary>
    /// Returns the authenticated teacher's own allocations. Powers the assignment
    /// authoring form, which only offers class/subject pairs the teacher may use.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Teacher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentResponse>>> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetMyTeacherAssignmentsQuery(), cancellationToken));

    /// <summary>Allocates a teacher to a class and subject. Administrators only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TeacherAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAssignmentResponse>> Create(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateTeacherAssignmentCommand(request.TeacherId, request.ClassId, request.SubjectId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Returns a single allocation by identifier. Administrators only.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TeacherAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAssignmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var all = await _sender.Send(new GetTeacherAssignmentsQuery(), cancellationToken);
        var allocation = all.FirstOrDefault(a => a.Id == id);

        if (allocation is null)
        {
            return NotFound();
        }

        return Ok(allocation);
    }

    /// <summary>Removes a teacher allocation. Administrators only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTeacherAssignmentCommand(id), cancellationToken);
        return NoContent();
    }
}
