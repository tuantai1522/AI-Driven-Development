using Todo.Api.Features.Sample.List;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Todo.Api.Persistence;
using Todo.Api.Persistence.Models;

namespace Todo.Api.UnitTests.Features.Sample.List;

public sealed class ListSamplesHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Items_Ordered_By_Creation_Time()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        dbContext.SampleItems.AddRange(
            new SampleItem { Id = Guid.NewGuid(), Name = "older", CreatedUtc = DateTime.UtcNow.AddMinutes(-2) },
            new SampleItem { Id = Guid.NewGuid(), Name = "newer", CreatedUtc = DateTime.UtcNow.AddMinutes(-1) });
        await dbContext.SaveChangesAsync();

        var handler = new Handler(dbContext);
        var result = await handler.Handle(new Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Select(x => x.Name).Should().ContainInOrder("older", "newer");
    }
}
