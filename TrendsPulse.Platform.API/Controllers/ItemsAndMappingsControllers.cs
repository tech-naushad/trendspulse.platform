using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Application.Features.DataSourceMappings.Commands;
using TrendsPulse.Platform.Application.Features.DataSourceMappings.Queries;
using TrendsPulse.Platform.Application.Features.Items.Commands;
using TrendsPulse.Platform.Application.Features.Items.Queries;
using TrendsPulse.Platform.Domain.Enums;

namespace TrendsPulse.Platform.API.Controllers;

// ════════════════════════════════════════════
// ITEMS
// ════════════════════════════════════════════
[ApiController]
[Route("api/v1/items")]
[Authorize]
[Produces("application/json")]
public sealed class ItemsController : ControllerBase
{
    private readonly ISender _sender;
    public ItemsController(ISender sender) => _sender = sender;

    /// <summary>Paged item list. Filter by categoryId, status, search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<PagedResult<ItemSummaryDto>>), 200)]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid?       categoryId = null,
        [FromQuery] ItemStatus? status     = null,
        [FromQuery] string?     search     = null,
        [FromQuery] int         page       = 1,
        [FromQuery] int         pageSize   = 20,
        CancellationToken ct = default) =>
        Ok(await _sender.Send(
            new GetItemsQuery(categoryId, status, search, page, pageSize), ct));

    /// <summary>Get single item with all data source mappings.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<ItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new GetItemByIdQuery(id), ct));

    /// <summary>Get item by URL slug (e.g. gold-spot, bitcoin).</summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(ApiResult<ItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await _sender.Send(new GetItemBySlugQuery(slug), ct));

    /// <summary>All items in a category visible to the current tenant.</summary>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ItemSummaryDto>>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCategory(Guid categoryId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetItemsByCategoryQuery(categoryId), ct));

    /// <summary>Create item. Slug auto-generated. TenantAdmin required.</summary>
    [HttpPost]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<ItemDto>), 201)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(
        [FromBody] CreateItemCommand cmd, CancellationToken ct)
    {
        var result = await _sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Full update. TenantAdmin required.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<ItemDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateItemRequest body, CancellationToken ct)
    {
        var cmd = new UpdateItemCommand(
            id, body.Name, body.Description, body.Symbol, body.Unit,
            body.CustomUnitLabel, body.DecimalPrecision, body.Status,
            body.Visibility, body.Tags, body.ThumbnailUrl, body.CategoryId);
        return Ok(await _sender.Send(cmd, ct));
    }

    /// <summary>Change status only. TenantAdmin required.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<ItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PatchStatus(
        Guid id, [FromBody] PatchStatusRequest body, CancellationToken ct) =>
        Ok(await _sender.Send(new PatchItemStatusCommand(id, body.Status), ct));

    /// <summary>Soft-delete. System items blocked. TenantAdmin required.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new DeleteItemCommand(id), ct));
}

// Request body models (route id injected by controller)
public sealed record UpdateItemRequest(
    string Name, string? Description, string? Symbol,
    PriceUnit Unit, string? CustomUnitLabel, int DecimalPrecision,
    ItemStatus Status, ItemVisibility Visibility,
    string? Tags, string? ThumbnailUrl, Guid CategoryId);

public sealed record PatchStatusRequest(ItemStatus Status);

// ════════════════════════════════════════════
// DATA SOURCE MAPPINGS
// ════════════════════════════════════════════
[ApiController]
[Route("api/v1/items/{itemId:guid}/mappings")]
[Authorize]
[Produces("application/json")]
public sealed class DataSourceMappingsController : ControllerBase
{
    private readonly ISender _sender;
    public DataSourceMappingsController(ISender sender) => _sender = sender;

    /// <summary>All mappings for an item, primary listed first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<DataSourceMappingDto>>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByItem(Guid itemId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetMappingsByItemQuery(itemId), ct));

    /// <summary>Single mapping by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<DataSourceMappingDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid itemId, Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new GetMappingByIdQuery(id), ct));

    /// <summary>Add mapping. Setting IsPrimary demotes existing primary. TenantAdmin required.</summary>
    [HttpPost]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<DataSourceMappingDto>), 201)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(
        Guid itemId, [FromBody] CreateMappingRequest body, CancellationToken ct)
    {
        var cmd = new CreateMappingCommand(
            itemId, body.SourceType, body.ExternalIdentifier, body.IsPrimary,
            body.FetchFrequency, body.IsEnabled, body.AdditionalConfig,
            body.AvFunction, body.AvMarket, body.CustomEndpointUrl,
            body.CustomPriceJsonPath, body.CustomTimestampJsonPath, body.CustomHeaders);
        var result = await _sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById),
            new { itemId, id = result.Data!.Id }, result);
    }

    /// <summary>Update mapping config. TenantAdmin required.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<DataSourceMappingDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid itemId, Guid id, [FromBody] UpdateMappingRequest body, CancellationToken ct)
    {
        var cmd = new UpdateMappingCommand(
            id, body.SourceType, body.ExternalIdentifier, body.IsPrimary,
            body.FetchFrequency, body.IsEnabled, body.AdditionalConfig,
            body.AvFunction, body.AvMarket, body.CustomEndpointUrl,
            body.CustomPriceJsonPath, body.CustomTimestampJsonPath, body.CustomHeaders);
        return Ok(await _sender.Send(cmd, ct));
    }

    /// <summary>Toggle enabled/disabled without deleting. TenantAdmin required.</summary>
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<DataSourceMappingDto>), 200)]
    public async Task<IActionResult> Toggle(Guid itemId, Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new ToggleMappingCommand(id), ct));

    /// <summary>Soft-delete. Primary mapping cannot be deleted. TenantAdmin required.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Delete(Guid itemId, Guid id, CancellationToken ct) =>
        Ok(await _sender.Send(new DeleteMappingCommand(id), ct));
}

// Request body models
public sealed record CreateMappingRequest(
    DataSourceType SourceType, string ExternalIdentifier,
    bool IsPrimary, FetchFrequency FetchFrequency, bool IsEnabled,
    string? AdditionalConfig, string? AvFunction, string? AvMarket,
    string? CustomEndpointUrl, string? CustomPriceJsonPath,
    string? CustomTimestampJsonPath, string? CustomHeaders);

public sealed record UpdateMappingRequest(
    DataSourceType SourceType, string ExternalIdentifier,
    bool IsPrimary, FetchFrequency FetchFrequency, bool IsEnabled,
    string? AdditionalConfig, string? AvFunction, string? AvMarket,
    string? CustomEndpointUrl, string? CustomPriceJsonPath,
    string? CustomTimestampJsonPath, string? CustomHeaders);
