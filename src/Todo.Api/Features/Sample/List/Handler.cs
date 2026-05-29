using BuildingBlocks.Application.Results;
using Infrastructure.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Todo.Api.Features.Sample.List;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Query, Result<IReadOnlyList<Response>>>
{
    public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var items = await dbContext.SampleItems
            .AsNoTracking()
            .OrderBy(x => x.CreatedUtc)
            .Select(x => new Response(x.Id, x.Name, x.CreatedUtc))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<Response>>.Success(items);
    }
}
