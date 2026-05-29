using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using Infrastructure;
using MediatR;
using Todo.Api.Features.Sample.Create;

namespace Todo.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiAssembly = typeof(Command).Assembly;

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddEndpoints(apiAssembly);
        services.AddInfrastructure(configuration);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(apiAssembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssembly(apiAssembly);

        return services;
    }
}
