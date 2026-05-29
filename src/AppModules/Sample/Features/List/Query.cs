using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.List;

public sealed record Query() : IRequest<Result<IReadOnlyList<Response>>>;
