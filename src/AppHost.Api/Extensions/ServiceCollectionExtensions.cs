using AppModules.Sample.Domain;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using Infrastructure;
using MediatR;

namespace AppHost.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var appModulesAssembly = typeof(SampleItem).Assembly;

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddEndpoints(appModulesAssembly);
        services.AddInfrastructure(configuration);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(appModulesAssembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(appModulesAssembly);

        return services;
    }
}
