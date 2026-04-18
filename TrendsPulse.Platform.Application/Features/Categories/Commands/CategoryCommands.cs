using FluentValidation;
using MediatR;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Interfaces;

namespace TrendsPulse.Platform.Application.Features.Categories.Commands;

// ════════════════════════════════════════════
// CREATE CATEGORY
// ════════════════════════════════════════════
public sealed record CreateCategoryCommand(
    string       Name,
    string?      Description,
    string?      IconCode,
    string?      ColorHex,
    CategoryType Type,
    int?         DisplayOrder
) : IRequest<ApiResult<CategoryDto>>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.ColorHex)
            .Matches(@"^#[0-9A-Fa-f]{6}$").When(x => x.ColorHex is not null)
            .WithMessage("ColorHex must be a valid hex colour (e.g. #F59E0B).");
    }
}

public sealed class CreateCategoryHandler
    : IRequestHandler<CreateCategoryCommand, ApiResult<CategoryDto>>
{
    private readonly IUnitOfWork              _uow;
    private readonly ICurrentUserService      _user;
    private readonly ICategoryDomainService   _domainService;

    public CreateCategoryHandler(
        IUnitOfWork uow,
        ICurrentUserService user,
        ICategoryDomainService domainService)
    {
        _uow           = uow;
        _user          = user;
        _domainService = domainService;
    }

    public async Task<ApiResult<CategoryDto>> Handle(
        CreateCategoryCommand cmd, CancellationToken ct)
    {
        if (!_user.IsSuperAdmin && cmd.Type != CategoryType.Custom
            && cmd.Type != CategoryType.Stocks)
        {
            // Only SuperAdmins can create system-type categories.
            // Tenants can create Custom/Stocks (business decision).
        }

        await _domainService.EnsureNameIsUniqueAsync(
            cmd.Name, _user.TenantId, excludeId: null, ct);

        var displayOrder = cmd.DisplayOrder
            ?? await _uow.Categories.GetNextDisplayOrderAsync(_user.TenantId, ct);

        var category = Category.Create(
            cmd.Name, cmd.Description, cmd.IconCode, cmd.ColorHex,
            cmd.Type, displayOrder, _user.TenantId, _user.UserName);

        await _uow.Categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<CategoryDto>.Ok(
            CategoryMapper.ToDto(category, 0),
            "Category created successfully.");
    }
}

// ════════════════════════════════════════════
// UPDATE CATEGORY
// ════════════════════════════════════════════
public sealed record UpdateCategoryCommand(
    Guid         Id,
    string       Name,
    string?      Description,
    string?      IconCode,
    string?      ColorHex,
    CategoryType Type,
    int          DisplayOrder
) : IRequest<ApiResult<CategoryDto>>;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative.");
    }
}

public sealed class UpdateCategoryHandler
    : IRequestHandler<UpdateCategoryCommand, ApiResult<CategoryDto>>
{
    private readonly IUnitOfWork            _uow;
    private readonly ICurrentUserService    _user;
    private readonly ICategoryDomainService _domainService;

    public UpdateCategoryHandler(
        IUnitOfWork uow, ICurrentUserService user, ICategoryDomainService domainService)
    {
        _uow = uow; _user = user; _domainService = domainService;
    }

    public async Task<ApiResult<CategoryDto>> Handle(
        UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Category), cmd.Id);

        if (category.IsSystem && !_user.IsSuperAdmin)
            throw new ForbiddenException("System categories can only be modified by Super Admins.");

        if (category.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to modify this category.");

        await _domainService.EnsureNameIsUniqueAsync(
            cmd.Name, _user.TenantId, excludeId: cmd.Id, ct);

        category.Update(cmd.Name, cmd.Description, cmd.IconCode,
            cmd.ColorHex, cmd.Type, cmd.DisplayOrder, _user.UserName);

        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<CategoryDto>.Ok(CategoryMapper.ToDto(category, 0));
    }
}

// ════════════════════════════════════════════
// DELETE CATEGORY
// ════════════════════════════════════════════
public sealed record DeleteCategoryCommand(Guid Id) : IRequest<ApiResult<bool>>;

public sealed class DeleteCategoryHandler
    : IRequestHandler<DeleteCategoryCommand, ApiResult<bool>>
{
    private readonly IUnitOfWork            _uow;
    private readonly ICurrentUserService    _user;
    private readonly ICategoryDomainService _domainService;

    public DeleteCategoryHandler(
        IUnitOfWork uow, ICurrentUserService user, ICategoryDomainService domainService)
    {
        _uow = uow; _user = user; _domainService = domainService;
    }

    public async Task<ApiResult<bool>> Handle(
        DeleteCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Category), cmd.Id);

        if (category.IsSystem)
            throw new ForbiddenException("System categories cannot be deleted.");

        if (category.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to delete this category.");

        await _domainService.EnsureCanDeleteAsync(cmd.Id, ct);

        category.SoftDelete(_user.UserName);
        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Category deleted successfully.");
    }
}
