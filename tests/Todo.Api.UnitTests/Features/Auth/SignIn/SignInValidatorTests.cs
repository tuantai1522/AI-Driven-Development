using FluentAssertions;
using Todo.Api.Features.Auth.SignIn;

namespace Todo.Api.UnitTests.Features.Auth.SignIn;

public sealed class SignInValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Missing()
    {
        var result = _validator.Validate(new Command(string.Empty, "secret123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Invalid()
    {
        var result = _validator.Validate(new Command("not-an-email", "secret123"));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Password_Is_Missing()
    {
        var result = _validator.Validate(new Command("user@example.com", string.Empty));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Password));
    }
}
