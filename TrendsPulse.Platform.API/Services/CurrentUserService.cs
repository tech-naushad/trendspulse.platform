using System.Security.Claims;
using TrendsPulse.Platform.Application.Common.Interfaces;

namespace TrendsPulse.Platform.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? P => _http.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var v = P?.FindFirstValue(ClaimTypes.NameIdentifier) ?? P?.FindFirstValue("sub");
            return Guid.TryParse(v, out var id) ? id : Guid.Empty;
        }
    }
    public Guid? TenantId
    {
        get
        {
            var v = P?.FindFirstValue("tenant_id");
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }
    public string  UserName      => P?.FindFirstValue(ClaimTypes.Name) ?? "system";
    public bool    IsAuthenticated => P?.Identity?.IsAuthenticated == true;
    public bool    IsSuperAdmin  => P?.IsInRole("SuperAdmin") == true;
    public bool    IsTenantAdmin => P?.IsInRole("TenantAdmin") == true || IsSuperAdmin;
}
