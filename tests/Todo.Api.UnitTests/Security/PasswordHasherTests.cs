using FluentAssertions;
using Todo.Api.Security;

namespace Todo.Api.UnitTests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_Should_Return_Derived_Value_Instead_Of_Raw_Password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("secret123");

        hash.Should().NotBe("secret123");
        hash.Should().Contain(".");
    }
}
