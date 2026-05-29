using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request {RequestName} {@Request}", typeof(TRequest).Name, request);
        var response = await next();
        logger.LogInformation("Handled request {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
