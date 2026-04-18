using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using ValidationException = TrendsPulse.Platform.Application.Common.Exceptions.ValidationException;

namespace TrendsPulse.Platform.Application.Common.Behaviours;

// ── 1. Validation Behaviour ───────────────────────────────────────────────────
/// <summary>
/// Runs every FluentValidation IValidator registered for TRequest
/// before the handler executes.
/// Zero handlers = pass through. One or more validators = all must pass.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}

// ── 2. Logging Behaviour ──────────────────────────────────────────────────────
/// <summary>
/// Logs every request/response at Debug level.
/// Logs slow requests (> 500ms) at Warning.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        _logger.LogDebug("Handling {RequestName}", name);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("Slow request {RequestName} took {Elapsed}ms", name, sw.ElapsedMilliseconds);
        else
            _logger.LogDebug("Handled {RequestName} in {Elapsed}ms", name, sw.ElapsedMilliseconds);

        return response;
    }
}

// ── 3. Caching Behaviour ──────────────────────────────────────────────────────
/// <summary>
/// Caches responses for queries that implement ICacheableQuery.
/// Commands are never cached (they don't implement ICacheableQuery).
/// </summary>
public sealed class CachingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;

    public CachingBehaviour(IMemoryCache cache) => _cache = cache;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        if (_cache.TryGetValue(cacheable.CacheKey, out TResponse? cached) && cached is not null)
            return cached;

        var response = await next();
        _cache.Set(cacheable.CacheKey, response,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration });

        return response;
    }
}
