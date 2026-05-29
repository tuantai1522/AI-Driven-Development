using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Endpoints;
using FluentValidation;
using MediatR;
using Todo.Api.Features.Sample.Create;
using Todo.Api.Abstractions.Persistence;
using Todo.Api.Abstractions.Security;
using Todo.Api.Security;
using Todo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

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
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
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
