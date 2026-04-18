using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TrendsPulse.Platform.Application.Common.Behaviours;

namespace TrendsPulse.Platform.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        // MediatR — scans assembly for all IRequestHandler<,>
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline order: Logging → Validation → Caching → Handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
        });

        // FluentValidation — auto-registers all AbstractValidator<T> in assembly
        services.AddValidatorsFromAssembly(assembly);

        // IMemoryCache for CachingBehaviour
        services.AddMemoryCache();

        return services;
    }
}
