using FluentValidation;

namespace Todo.Api.Features.Sample.Create;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
