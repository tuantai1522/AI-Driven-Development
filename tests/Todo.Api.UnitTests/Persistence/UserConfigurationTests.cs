using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Domain.Users;
using Todo.Api.Persistence;

namespace Todo.Api.UnitTests.Persistence;

public sealed class UserConfigurationTests
{
    [Fact]
    public void ApplicationDbContext_Should_Map_Users_Table_With_Unique_Email_And_UserName_Indexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        dbContext.Users.Should().NotBeNull();

        var entityType = dbContext.Model.FindEntityType(typeof(User));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("users");
        entityType.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(User.Email) }));
        entityType.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(User.UserName) }));
    }
}
