using BuildingBlocks.Application.Results;
using Infrastructure.Abstractions;
using Infrastructure.Persistence.Models;
using MediatR;

namespace Todo.Api.Features.Sample.Create;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Command, Result<Response>>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
    {
        var entity = new SampleItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.SampleItems.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Response>.Success(new Response(entity.Id, entity.Name, entity.CreatedUtc));
    }
}
