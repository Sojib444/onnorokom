using AssignmentManagement.Application.Contracts;
using AssignmentManagement.Application.Features.Classes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

/// <summary>Classes (courses). Listing is available to any authenticated user; mutations require Admin.</summary>
[ApiController]
[Authorize]
[Route("api/classes")]
public sealed class ClassesController : ControllerBase
{
    private readonly ISender _sender;

    public ClassesController(ISender sender) => _sender = sender;

    /// <summary>Returns all classes, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetClassesQuery(), cancellationToken));

    /// <summary>Creates a class. Administrators only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClassResponse>> Create(
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateClassCommand(request.Name, request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Returns a single class by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var classes = await _sender.Send(new GetClassesQuery(), cancellationToken);
        var klass = classes.FirstOrDefault(c => c.Id == id);

        if (klass is null)
        {
            return NotFound();
        }

        return Ok(klass);
    }

    /// <summary>Updates a class. Administrators only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassResponse>> Update(
        Guid id,
        UpdateClassRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(
            new UpdateClassCommand(id, request.Name, request.Description),
            cancellationToken));

    /// <summary>Deletes an unreferenced class. Administrators only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteClassCommand(id), cancellationToken);
        return NoContent();
    }
}
