using Microsoft.Extensions.DependencyInjection;
using Standup.Application.Services;

namespace Standup.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IStandupAggregatorService, StandupAggregatorService>();

        return services;
    }
}
