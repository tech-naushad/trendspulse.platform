using FluentValidation;
using MediatR;
using TrendsPulse.Platform.Application.Tests.Common;
using TrendsPulse.Platform.Application.Tests.Common.Exceptions;
using TrendsPulse.Platform.Application.Tests.Common.Interfaces;
using TrendsPulse.Platform.Application.Tests.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Interfaces;


namespace TrendsPulse.Platform.Application.Tests.Features.Items.Queries;

// ════════════════════════════════════════════
// GET ITEMS (PAGED)
// ════════════════════════════════════════════
public sealed record GetItemsQuery(
    Guid?      CategoryId,
    ItemStatus? Status,
    string?    Search,
    int        Page     = 1,
    int        PageSize = 20
) : IRequest<ApiResult<PagedResult<ItemSummaryDto>>>;

public sealed class GetItemsValidator : AbstractValidator<GetItemsQuery>
{
    public GetItemsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}

public sealed class GetItemsHandler
    : IRequestHandler<GetItemsQuery, ApiResult<PagedResult<ItemSummaryDto>>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetItemsHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<PagedResult<ItemSummaryDto>>> Handle(
        GetItemsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await _uow.Items.GetPagedAsync(
            _user.TenantId, request.CategoryId, request.Status,
            request.Search, request.Page, request.PageSize, ct);

        return ApiResult<PagedResult<ItemSummaryDto>>.Ok(new PagedResult<ItemSummaryDto>
        {
            Items      = items.Select(ItemMapper.ToSummaryDto),
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        });
    }
}

// ════════════════════════════════════════════
// GET ITEM BY ID
// ════════════════════════════════════════════
public sealed record GetItemByIdQuery(Guid Id)
    : IRequest<ApiResult<ItemDto>>, ICacheableQuery
{
    public string   CacheKey      => $"item:{Id}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class GetItemByIdHandler
    : IRequestHandler<GetItemByIdQuery, ApiResult<ItemDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetItemByIdHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<ItemDto>> Handle(
        GetItemByIdQuery request, CancellationToken ct)
    {
        var item = await _uow.Items.GetWithMappingsAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Item), request.Id);

        if (item.TenantId.HasValue
            && item.TenantId != _user.TenantId
            && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have access to this item.");

        return ApiResult<ItemDto>.Ok(ItemMapper.ToDto(item));
    }
}

// ════════════════════════════════════════════
// GET ITEM BY SLUG
// ════════════════════════════════════════════
public sealed record GetItemBySlugQuery(string Slug)
    : IRequest<ApiResult<ItemDto>>, ICacheableQuery
{
    public string   CacheKey      => $"item:slug:{Slug}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class GetItemBySlugHandler
    : IRequestHandler<GetItemBySlugQuery, ApiResult<ItemDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetItemBySlugHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<ItemDto>> Handle(
        GetItemBySlugQuery request, CancellationToken ct)
    {
        var item = await _uow.Items.GetBySlugAsync(request.Slug, ct)
            ?? throw new NotFoundException(nameof(Item), request.Slug);

        if (item.TenantId.HasValue
            && item.TenantId != _user.TenantId
            && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have access to this item.");

        var withMappings = await _uow.Items.GetWithMappingsAsync(item.Id, ct);
        return ApiResult<ItemDto>.Ok(ItemMapper.ToDto(withMappings!));
    }
}

// ════════════════════════════════════════════
// GET ITEMS BY CATEGORY
// ════════════════════════════════════════════
public sealed record GetItemsByCategoryQuery(Guid CategoryId)
    : IRequest<ApiResult<IEnumerable<ItemSummaryDto>>>, ICacheableQuery
{
    public string   CacheKey      => $"items:category:{CategoryId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class GetItemsByCategoryHandler
    : IRequestHandler<GetItemsByCategoryQuery, ApiResult<IEnumerable<ItemSummaryDto>>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetItemsByCategoryHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<IEnumerable<ItemSummaryDto>>> Handle(
        GetItemsByCategoryQuery request, CancellationToken ct)
    {
        _ = await _uow.Categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var items = await _uow.Items.GetByCategoryAsync(
            request.CategoryId, _user.TenantId, ct);

        return ApiResult<IEnumerable<ItemSummaryDto>>.Ok(
            items.Select(ItemMapper.ToSummaryDto));
    }
}
