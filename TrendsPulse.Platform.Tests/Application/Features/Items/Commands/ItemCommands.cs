using FluentValidation;
using MediatR;
using TrendsPulse.Platform.Application.Tests.Common;
using TrendsPulse.Platform.Application.Tests.Common.Exceptions;
using TrendsPulse.Platform.Application.Tests.Common.Interfaces;
using TrendsPulse.Platform.Application.Tests.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Interfaces;

namespace TrendsPulse.Platform.Application.Tests.Features.Items.Commands;

// ════════════════════════════════════════════
// CREATE ITEM
// ════════════════════════════════════════════
public sealed record CreateItemCommand(
    string         Name,
    string?        Description,
    string?        Symbol,
    PriceUnit      Unit,
    string?        CustomUnitLabel,
    int            DecimalPrecision,
    ItemVisibility Visibility,
    string?        Tags,
    string?        ThumbnailUrl,
    Guid           CategoryId
) : IRequest<ApiResult<ItemDto>>;

public sealed class CreateItemValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.CustomUnitLabel)
            .NotEmpty().WithMessage("Custom unit label is required when Unit is Custom.")
            .When(x => x.Unit == PriceUnit.Custom);

        RuleFor(x => x.DecimalPrecision)
            .InclusiveBetween(0, 8)
            .WithMessage("Decimal precision must be between 0 and 8.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Symbol)
            .MaximumLength(20).WithMessage("Symbol must not exceed 20 characters.")
            .When(x => x.Symbol is not null);
    }
}

public sealed class CreateItemHandler : IRequestHandler<CreateItemCommand, ApiResult<ItemDto>>
{
    private readonly IUnitOfWork           _uow;
    private readonly ICurrentUserService   _user;
    private readonly IItemDomainService    _itemDomain;

    public CreateItemHandler(
        IUnitOfWork uow, ICurrentUserService user, IItemDomainService itemDomain)
    {
        _uow        = uow;
        _user       = user;
        _itemDomain = itemDomain;
    }

    public async Task<ApiResult<ItemDto>> Handle(
        CreateItemCommand cmd, CancellationToken ct)
    {
        if (!_user.IsSuperAdmin && cmd.Visibility == ItemVisibility.Global)
            throw new ForbiddenException("Only Super Admins can create global items.");

        await _itemDomain.EnsureCategoryExistsAsync(cmd.CategoryId, _user.TenantId, ct);

        var slug = await _itemDomain.GenerateUniqueSlugAsync(cmd.Name, excludeItemId: null, ct);

        var item = Item.Create(
            cmd.Name, slug, cmd.Description, cmd.Symbol,
            cmd.Unit, cmd.CustomUnitLabel, cmd.DecimalPrecision,
            cmd.Visibility, cmd.Tags, cmd.ThumbnailUrl,
            cmd.CategoryId, _user.TenantId, _user.UserName);

        await _uow.Items.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);

        // Reload with navigation properties for response
        var created = await _uow.Items.GetWithMappingsAsync(item.Id, ct);
        return ApiResult<ItemDto>.Ok(ItemMapper.ToDto(created!), "Item created successfully.");
    }
}

// ════════════════════════════════════════════
// UPDATE ITEM
// ════════════════════════════════════════════
public sealed record UpdateItemCommand(
    Guid           Id,
    string         Name,
    string?        Description,
    string?        Symbol,
    PriceUnit      Unit,
    string?        CustomUnitLabel,
    int            DecimalPrecision,
    ItemStatus     Status,
    ItemVisibility Visibility,
    string?        Tags,
    string?        ThumbnailUrl,
    Guid           CategoryId
) : IRequest<ApiResult<ItemDto>>;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.CustomUnitLabel)
            .NotEmpty().WithMessage("Custom unit label is required when Unit is Custom.")
            .When(x => x.Unit == PriceUnit.Custom);

        RuleFor(x => x.DecimalPrecision)
            .InclusiveBetween(0, 8)
            .WithMessage("Decimal precision must be between 0 and 8.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}

public sealed class UpdateItemHandler : IRequestHandler<UpdateItemCommand, ApiResult<ItemDto>>
{
    private readonly IUnitOfWork          _uow;
    private readonly ICurrentUserService  _user;
    private readonly IItemDomainService   _itemDomain;

    public UpdateItemHandler(
        IUnitOfWork uow, ICurrentUserService user, IItemDomainService itemDomain)
    {
        _uow        = uow;
        _user       = user;
        _itemDomain = itemDomain;
    }

    public async Task<ApiResult<ItemDto>> Handle(
        UpdateItemCommand cmd, CancellationToken ct)
    {
        var item = await _uow.Items.GetWithMappingsAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Item), cmd.Id);

        if (item.IsSystem && !_user.IsSuperAdmin)
            throw new ForbiddenException("System items can only be modified by Super Admins.");

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to modify this item.");

        if (!_user.IsSuperAdmin && cmd.Visibility == ItemVisibility.Global)
            throw new ForbiddenException("Only Super Admins can make items global.");

        await _itemDomain.EnsureCategoryExistsAsync(cmd.CategoryId, _user.TenantId, ct);

        // Regenerate slug only if name changed
        var slug = item.Name.Equals(cmd.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            ? Domain.ValueObjects.Slug.Create(item.Slug)
            : await _itemDomain.GenerateUniqueSlugAsync(cmd.Name, excludeItemId: cmd.Id, ct);

        item.Update(cmd.Name, slug, cmd.Description, cmd.Symbol,
            cmd.Unit, cmd.CustomUnitLabel, cmd.DecimalPrecision,
            cmd.Status, cmd.Visibility, cmd.Tags, cmd.ThumbnailUrl,
            cmd.CategoryId, _user.UserName);

        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Items.GetWithMappingsAsync(cmd.Id, ct);
        return ApiResult<ItemDto>.Ok(ItemMapper.ToDto(updated!), "Item updated successfully.");
    }
}

// ════════════════════════════════════════════
// PATCH STATUS
// ════════════════════════════════════════════
public sealed record PatchItemStatusCommand(
    Guid       Id,
    ItemStatus Status
) : IRequest<ApiResult<ItemDto>>;

public sealed class PatchItemStatusValidator : AbstractValidator<PatchItemStatusCommand>
{
    public PatchItemStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be Active, Paused, or Deprecated.");
    }
}

public sealed class PatchItemStatusHandler
    : IRequestHandler<PatchItemStatusCommand, ApiResult<ItemDto>>
{
    private readonly IUnitOfWork          _uow;
    private readonly ICurrentUserService  _user;

    public PatchItemStatusHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<ItemDto>> Handle(
        PatchItemStatusCommand cmd, CancellationToken ct)
    {
        var item = await _uow.Items.GetWithMappingsAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Item), cmd.Id);

        if (item.IsSystem && !_user.IsSuperAdmin)
            throw new ForbiddenException("System items can only be modified by Super Admins.");

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to modify this item.");

        item.ChangeStatus(cmd.Status, _user.UserName);
        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Items.GetWithMappingsAsync(cmd.Id, ct);
        return ApiResult<ItemDto>.Ok(ItemMapper.ToDto(updated!),
            $"Item status changed to {cmd.Status}.");
    }
}

// ════════════════════════════════════════════
// DELETE ITEM
// ════════════════════════════════════════════
public sealed record DeleteItemCommand(Guid Id) : IRequest<ApiResult<bool>>;

public sealed class DeleteItemHandler : IRequestHandler<DeleteItemCommand, ApiResult<bool>>
{
    private readonly IUnitOfWork          _uow;
    private readonly ICurrentUserService  _user;

    public DeleteItemHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<bool>> Handle(
        DeleteItemCommand cmd, CancellationToken ct)
    {
        var item = await _uow.Items.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Item), cmd.Id);

        if (item.IsSystem)
            throw new ForbiddenException("System items cannot be deleted.");

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to delete this item.");

        item.SoftDelete(_user.UserName);
        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Item deleted successfully.");
    }
}
