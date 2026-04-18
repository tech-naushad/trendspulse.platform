using MediatR;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Interfaces;

namespace TrendsPulse.Platform.Application.Features.DataSourceMappings.Queries;

// ════════════════════════════════════════════
// GET MAPPINGS BY ITEM
// ════════════════════════════════════════════
public sealed record GetMappingsByItemQuery(Guid ItemId)
    : IRequest<ApiResult<IEnumerable<DataSourceMappingDto>>>, ICacheableQuery
{
    public string   CacheKey      => $"mappings:item:{ItemId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class GetMappingsByItemHandler
    : IRequestHandler<GetMappingsByItemQuery, ApiResult<IEnumerable<DataSourceMappingDto>>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetMappingsByItemHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<IEnumerable<DataSourceMappingDto>>> Handle(
        GetMappingsByItemQuery request, CancellationToken ct)
    {
        var item = await _uow.Items.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);

        if (item.TenantId.HasValue
            && item.TenantId != _user.TenantId
            && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have access to this item.");

        var mappings = await _uow.DataSourceMappings.GetByItemAsync(request.ItemId, ct);

        var dtos = mappings
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.SourceType)
            .Select(m => DataSourceMappingMapper.ToDto(m, item.Name));

        return ApiResult<IEnumerable<DataSourceMappingDto>>.Ok(dtos);
    }
}

// ════════════════════════════════════════════
// GET MAPPING BY ID
// ════════════════════════════════════════════
public sealed record GetMappingByIdQuery(Guid Id)
    : IRequest<ApiResult<DataSourceMappingDto>>, ICacheableQuery
{
    public string   CacheKey      => $"mapping:{Id}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class GetMappingByIdHandler
    : IRequestHandler<GetMappingByIdQuery, ApiResult<DataSourceMappingDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public GetMappingByIdHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<DataSourceMappingDto>> Handle(
        GetMappingByIdQuery request, CancellationToken ct)
    {
        var mapping = await _uow.DataSourceMappings.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(DataSourceMapping), request.Id);

        var item = await _uow.Items.GetByIdAsync(mapping.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), mapping.ItemId);

        if (item.TenantId.HasValue
            && item.TenantId != _user.TenantId
            && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have access to this mapping.");

        return ApiResult<DataSourceMappingDto>.Ok(
            DataSourceMappingMapper.ToDto(mapping, item.Name));
    }
}
