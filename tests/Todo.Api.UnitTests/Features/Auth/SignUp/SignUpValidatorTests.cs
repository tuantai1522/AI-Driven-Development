using FluentAssertions;
using Todo.Api.Features.Auth.SignUp;

namespace Todo.Api.UnitTests.Features.Auth.SignUp;

public sealed class SignUpValidatorTests
{
    private readonly Validator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Missing()
    {
        var result = _validator.Validate(new Command(string.Empty, "secret123", "sampleUser"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Invalid()
    {
        var result = _validator.Validate(new Command("not-an-email", "secret123", "sampleUser"));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_Password_Is_Too_Short()
    {
        var result = _validator.Validate(new Command("user@example.com", "short", "sampleUser"));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.Password));
    }

    [Fact]
    public void Validate_Should_Fail_When_UserName_Is_Too_Short()
    {
        var result = _validator.Validate(new Command("user@example.com", "secret123", "ab"));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.UserName));
    }

    [Fact]
    public void Validate_Should_Fail_When_UserName_Is_Too_Long()
    {
        var result = _validator.Validate(new Command("user@example.com", "secret123", new string('a', 31)));

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.UserName));
    }
}
