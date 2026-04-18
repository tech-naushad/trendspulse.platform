// ═══════════════════════════════════════════════════════════
// ExceptionMiddleware.cs
// ═══════════════════════════════════════════════════════════
using System.Net;
using System.Text.Json;
using TrendsPulse.Platform.Application.Common;
using TrendsPulse.Platform.Application.Common.Exceptions;

namespace TrendsPulse.Platform.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate                   _next;
    private readonly ILogger<ExceptionMiddleware>      _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, result) = ex switch
        {
            NotFoundException e     => (HttpStatusCode.NotFound,                 ApiResult<object>.Fail(e.Message)),
            ValidationException e   => (HttpStatusCode.UnprocessableEntity,      ApiResult<object>.Fail(e.Errors)),
            ConflictException e     => (HttpStatusCode.Conflict,                 ApiResult<object>.Fail(e.Message)),
            ForbiddenException e    => (HttpStatusCode.Forbidden,                ApiResult<object>.Fail(e.Message)),
            UnauthorizedAccessException e => (HttpStatusCode.Unauthorized,       ApiResult<object>.Fail(e.Message)),
            _                       => (HttpStatusCode.InternalServerError,      ApiResult<object>.Fail("An unexpected error occurred."))
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled: {Message}", ex.Message);

        ctx.Response.StatusCode  = (int)status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
