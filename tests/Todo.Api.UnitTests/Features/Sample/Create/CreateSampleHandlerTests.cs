using Todo.Api.Features.Sample.Create;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Todo.Api.UnitTests.Features.Sample.Create;

public sealed class CreateSampleHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_Entity_And_Return_Response()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var handler = new Handler(dbContext);

        var result = await handler.Handle(new Command(" First sample "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("First sample");
        dbContext.SampleItems.Should().ContainSingle(x => x.Name == "First sample");
    }
}
