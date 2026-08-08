using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Features.Subjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

/// <summary>Subjects. Listing is available to any authenticated user; mutations require Admin.</summary>
[ApiController]
[Authorize]
[Route("api/subjects")]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISender _sender;

    public SubjectsController(ISender sender) => _sender = sender;

    /// <summary>Returns all subjects, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetSubjectsQuery(), cancellationToken));

    /// <summary>Returns a single subject by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subjects = await _sender.Send(new GetSubjectsQuery(), cancellationToken);
        var subject = subjects.FirstOrDefault(s => s.Id == id);

        if (subject is null)
        {
            return NotFound();
        }

        return Ok(subject);
    }

    /// <summary>Creates a subject. Administrators only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateSubjectCommand(request.Name, request.Code),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Updates a subject. Administrators only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new UpdateSubjectCommand(id, request.Name, request.Code),
            cancellationToken));

    /// <summary>Deletes an unreferenced subject. Administrators only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteSubjectCommand(id), cancellationToken);
        return NoContent();
    }
}
