using FluentValidation;
using MediatR;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Application.Common.Mappings;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Interfaces;

namespace TrendsPulse.Platform.Application.Features.DataSourceMappings.Commands;

// ════════════════════════════════════════════
// CREATE MAPPING
// ════════════════════════════════════════════
public sealed record CreateMappingCommand(
    Guid           ItemId,
    DataSourceType SourceType,
    string         ExternalIdentifier,
    bool           IsPrimary,
    FetchFrequency FetchFrequency,
    bool           IsEnabled,
    string?        AdditionalConfig,
    string?        AvFunction,
    string?        AvMarket,
    string?        CustomEndpointUrl,
    string?        CustomPriceJsonPath,
    string?        CustomTimestampJsonPath,
    string?        CustomHeaders
) : IRequest<ApiResult<DataSourceMappingDto>>;

public sealed class CreateMappingValidator : AbstractValidator<CreateMappingCommand>
{
    public CreateMappingValidator()
    {
        RuleFor(x => x.ExternalIdentifier)
            .NotEmpty().WithMessage("External identifier is required.")
            .MaximumLength(300);

        // AlphaVantage rules
        RuleFor(x => x.AvFunction)
            .NotEmpty().WithMessage("AlphaVantage function is required (e.g. TIME_SERIES_DAILY).")
            .When(x => x.SourceType == DataSourceType.AlphaVantage);

        // Custom HTTP rules
        RuleFor(x => x.CustomEndpointUrl)
            .NotEmpty().WithMessage("Endpoint URL is required for Custom HTTP connector.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Endpoint URL must be a valid absolute URL.")
            .When(x => x.SourceType == DataSourceType.CustomHttp);

        RuleFor(x => x.CustomPriceJsonPath)
            .NotEmpty().WithMessage("Price JSONPath is required for Custom HTTP connector.")
            .When(x => x.SourceType == DataSourceType.CustomHttp);
    }
}

public sealed class CreateMappingHandler
    : IRequestHandler<CreateMappingCommand, ApiResult<DataSourceMappingDto>>
{
    private readonly IUnitOfWork          _uow;
    private readonly ICurrentUserService  _user;

    public CreateMappingHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<DataSourceMappingDto>> Handle(
        CreateMappingCommand cmd, CancellationToken ct)
    {
        var item = await _uow.Items.GetByIdAsync(cmd.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), cmd.ItemId);

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to configure this item.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Demote existing primary if needed
            if (cmd.IsPrimary)
            {
                var existingPrimary = await _uow.DataSourceMappings
                    .GetPrimaryForItemAsync(cmd.ItemId, ct);
                if (existingPrimary is not null)
                {
                    existingPrimary.DemoteFromPrimary(_user.UserName);
                    _uow.DataSourceMappings.Update(existingPrimary);
                }
            }

            var mapping = DataSourceMapping.Create(
                cmd.ItemId, cmd.SourceType, cmd.ExternalIdentifier,
                cmd.IsPrimary, cmd.FetchFrequency, cmd.IsEnabled,
                cmd.AdditionalConfig, cmd.AvFunction, cmd.AvMarket,
                cmd.CustomEndpointUrl, cmd.CustomPriceJsonPath,
                cmd.CustomTimestampJsonPath, cmd.CustomHeaders,
                _user.UserName);

            await _uow.DataSourceMappings.AddAsync(mapping, ct);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            return ApiResult<DataSourceMappingDto>.Ok(
                DataSourceMappingMapper.ToDto(mapping, item.Name),
                "Mapping created successfully.");
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}

// ════════════════════════════════════════════
// UPDATE MAPPING
// ════════════════════════════════════════════
public sealed record UpdateMappingCommand(
    Guid           Id,
    DataSourceType SourceType,
    string         ExternalIdentifier,
    bool           IsPrimary,
    FetchFrequency FetchFrequency,
    bool           IsEnabled,
    string?        AdditionalConfig,
    string?        AvFunction,
    string?        AvMarket,
    string?        CustomEndpointUrl,
    string?        CustomPriceJsonPath,
    string?        CustomTimestampJsonPath,
    string?        CustomHeaders
) : IRequest<ApiResult<DataSourceMappingDto>>;

public sealed class UpdateMappingValidator : AbstractValidator<UpdateMappingCommand>
{
    public UpdateMappingValidator()
    {
        RuleFor(x => x.ExternalIdentifier)
            .NotEmpty().WithMessage("External identifier is required.")
            .MaximumLength(300);

        RuleFor(x => x.AvFunction)
            .NotEmpty().WithMessage("AlphaVantage function is required.")
            .When(x => x.SourceType == DataSourceType.AlphaVantage);

        RuleFor(x => x.CustomEndpointUrl)
            .NotEmpty().WithMessage("Endpoint URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Endpoint URL must be a valid absolute URL.")
            .When(x => x.SourceType == DataSourceType.CustomHttp);

        RuleFor(x => x.CustomPriceJsonPath)
            .NotEmpty().WithMessage("Price JSONPath is required.")
            .When(x => x.SourceType == DataSourceType.CustomHttp);
    }
}

public sealed class UpdateMappingHandler
    : IRequestHandler<UpdateMappingCommand, ApiResult<DataSourceMappingDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public UpdateMappingHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<DataSourceMappingDto>> Handle(
        UpdateMappingCommand cmd, CancellationToken ct)
    {
        var mapping = await _uow.DataSourceMappings.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(DataSourceMapping), cmd.Id);

        var item = await _uow.Items.GetByIdAsync(mapping.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), mapping.ItemId);

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to configure this item.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Promoting to primary — demote current primary
            if (cmd.IsPrimary && !mapping.IsPrimary)
            {
                var current = await _uow.DataSourceMappings
                    .GetPrimaryForItemAsync(mapping.ItemId, ct);
                if (current is not null && current.Id != cmd.Id)
                {
                    current.DemoteFromPrimary(_user.UserName);
                    _uow.DataSourceMappings.Update(current);
                }
            }

            mapping.Update(
                cmd.SourceType, cmd.ExternalIdentifier, cmd.IsPrimary,
                cmd.FetchFrequency, cmd.IsEnabled, cmd.AdditionalConfig,
                cmd.AvFunction, cmd.AvMarket, cmd.CustomEndpointUrl,
                cmd.CustomPriceJsonPath, cmd.CustomTimestampJsonPath,
                cmd.CustomHeaders, _user.UserName);

            _uow.DataSourceMappings.Update(mapping);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            return ApiResult<DataSourceMappingDto>.Ok(
                DataSourceMappingMapper.ToDto(mapping, item.Name),
                "Mapping updated successfully.");
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}

// ════════════════════════════════════════════
// TOGGLE MAPPING
// ════════════════════════════════════════════
public sealed record ToggleMappingCommand(Guid Id)
    : IRequest<ApiResult<DataSourceMappingDto>>;

public sealed class ToggleMappingHandler
    : IRequestHandler<ToggleMappingCommand, ApiResult<DataSourceMappingDto>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public ToggleMappingHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<DataSourceMappingDto>> Handle(
        ToggleMappingCommand cmd, CancellationToken ct)
    {
        var mapping = await _uow.DataSourceMappings.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(DataSourceMapping), cmd.Id);

        var item = await _uow.Items.GetByIdAsync(mapping.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), mapping.ItemId);

        if (item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to configure this item.");

        mapping.Toggle(_user.UserName);
        _uow.DataSourceMappings.Update(mapping);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<DataSourceMappingDto>.Ok(
            DataSourceMappingMapper.ToDto(mapping, item.Name),
            $"Mapping {(mapping.IsEnabled ? "enabled" : "disabled")}.");
    }
}

// ════════════════════════════════════════════
// DELETE MAPPING
// ════════════════════════════════════════════
public sealed record DeleteMappingCommand(Guid Id) : IRequest<ApiResult<bool>>;

public sealed class DeleteMappingHandler
    : IRequestHandler<DeleteMappingCommand, ApiResult<bool>>
{
    private readonly IUnitOfWork         _uow;
    private readonly ICurrentUserService _user;

    public DeleteMappingHandler(IUnitOfWork uow, ICurrentUserService user)
    {
        _uow  = uow;
        _user = user;
    }

    public async Task<ApiResult<bool>> Handle(
        DeleteMappingCommand cmd, CancellationToken ct)
    {
        var mapping = await _uow.DataSourceMappings.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(nameof(DataSourceMapping), cmd.Id);

        if (mapping.IsPrimary)
            throw new ConflictException(
                "Cannot delete the primary mapping. Promote another mapping first.");

        var item = await _uow.Items.GetByIdAsync(mapping.ItemId, ct);
        if (item is not null && item.TenantId != _user.TenantId && !_user.IsSuperAdmin)
            throw new ForbiddenException("You do not have permission to configure this item.");

        mapping.SoftDelete(_user.UserName);
        _uow.DataSourceMappings.Update(mapping);
        await _uow.SaveChangesAsync(ct);

        return ApiResult<bool>.Ok(true, "Mapping deleted successfully.");
    }
}
