using AppModules.Sample.Domain;
using AppModules.Sample.Features.List;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppModules.UnitTests.Sample;

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
