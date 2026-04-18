using MediatR;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Interfaces;

namespace TrendsPulse.Platform.Application.Features.Categories.Queries;

// ════════════════════════════════════════════
// GET ALL CATEGORIES
// ════════════════════════════════════════════
public sealed record GetCategoriesQuery : IRequest<ApiResult<IEnumerable<CategoryDto>>>, ICacheableQuery
{
    public string    CacheKey       => $"categories:tenant:{TenantId}";
    public TimeSpan  CacheDuration  => TimeSpan.FromMinutes(5);
    public Guid?     TenantId       { get; init; }
}

public sealed class GetCategoriesHandler
    : IRequestHandler<GetCategoriesQuery, ApiResult<IEnumerable<CategoryDto>>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetCategoriesHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<IEnumerable<CategoryDto>>> Handle(
        GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _uow.Categories.GetVisibleToTenantAsync(_user.TenantId, ct);

        var dtos = categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => CategoryMapper.ToDto(
                c, c.Items.Count(i => !i.IsDeleted)));

        return ApiResult<IEnumerable<CategoryDto>>.Ok(dtos);
    }
}

// ════════════════════════════════════════════
// GET CATEGORY BY ID
// ════════════════════════════════════════════
public sealed record GetCategoryByIdQuery(Guid Id)
    : IRequest<ApiResult<CategoryDto>>, ICacheableQuery
{
    public string   CacheKey      => $"category:{Id}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class GetCategoryByIdHandler
    : IRequestHandler<GetCategoryByIdQuery, ApiResult<CategoryDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetCategoryByIdHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<CategoryDto>> Handle(
        GetCategoryByIdQuery request, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Category), request.Id);

        if (category.TenantId.HasValue
            && category.TenantId != _user.TenantId
            && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have access to this category.");

        var itemCount = await _uow.Categories.HasActiveItemsAsync(request.Id, ct) ? 1 : 0;
        return ApiResult<CategoryDto>.Ok(CategoryMapper.ToDto(category, itemCount));
    }
}
