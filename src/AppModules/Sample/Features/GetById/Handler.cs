using AppModules.Abstractions;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AppModules.Sample.Features.GetById;

public sealed class Handler(IApplicationDbContext dbContext) : IRequestHandler<Query, Result<Response>>
{
    public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
        var item = await dbContext.SampleItems
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new Response(x.Id, x.Name, x.CreatedUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return item is null
            ? Result<Response>.Failure(Error.NotFound)
            : Result<Response>.Success(item);
    }
}
