using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.GetById;

public sealed record Query(Guid Id) : IRequest<Result<Response>>;
