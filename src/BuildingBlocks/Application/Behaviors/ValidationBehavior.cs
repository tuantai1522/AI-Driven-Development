using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var errors = failures
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).ToArray());

        if (errors.Count != 0)
        {
            throw new RequestValidationException(errors);
        }

        return await next();
    }
}
