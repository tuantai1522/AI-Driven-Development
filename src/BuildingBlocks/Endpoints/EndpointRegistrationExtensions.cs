using System.Reflection;
using BuildingBlocks.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Endpoints;

public static class EndpointRegistrationExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        var endpointTypes = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(type))
            .ToArray();

        foreach (var endpointType in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
