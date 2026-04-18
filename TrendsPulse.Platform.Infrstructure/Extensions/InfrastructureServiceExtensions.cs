using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Domain.Interfaces;
using TrendsPulse.Platform.Infrstructure.Services;

namespace TrendsPulse.Platform.Infrstructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<Persistence.ApplicationDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(Persistence.ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();
        services.AddScoped<ICategoryDomainService, CategoryDomainService>();
        services.AddScoped<IItemDomainService, ItemDomainService>();

        return services;
    }
}
