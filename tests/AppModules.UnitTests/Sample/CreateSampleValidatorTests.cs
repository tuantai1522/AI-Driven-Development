using AppModules.Sample.Features.Create;
using FluentAssertions;

namespace AppModules.UnitTests.Sample;

public sealed class CreateSampleValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new Validator();
        var command = new Command(string.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(Command.Name));
    }
}
