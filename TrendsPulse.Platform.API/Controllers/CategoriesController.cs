using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Application.Features.Categories.Commands;
using TrendsPulse.Platform.Application.Features.Categories.Queries;
using TrendsPulse.Platform.Domain.Enums;


namespace TrendsPulse.Platform.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;
    public CategoriesController(ISender sender) => _sender = sender;

    /// <summary>Get all categories visible to the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<CategoryDto>>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _sender.Send(new GetCategoriesQuery(), ct));

    /// <summary>Get a single category by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new GetCategoryByIdQuery(id), ct));

    /// <summary>Create a new category. TenantAdmin required.</summary>
    [HttpPost]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await _sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Update a category. TenantAdmin required.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<CategoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCategoryRequest body, CancellationToken ct)
    {
        var cmd = new UpdateCategoryCommand(
            id, body.Name, body.Description, body.IconCode,
            body.ColorHex, body.Type, body.DisplayOrder);
        return Ok(await _sender.Send(cmd, ct));
    }

    /// <summary>Soft-delete a category. TenantAdmin required.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new DeleteCategoryCommand(id), ct));
}

// Separate request body model — keeps the command clean of route params
public sealed record UpdateCategoryRequest(
    string       Name,
    string?      Description,
    string?      IconCode,
    string?      ColorHex,
    CategoryType Type,
    int          DisplayOrder);
