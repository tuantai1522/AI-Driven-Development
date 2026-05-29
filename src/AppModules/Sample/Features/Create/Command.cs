using BuildingBlocks.Application.Results;
using MediatR;

namespace AppModules.Sample.Features.Create;

public sealed record Command(string Name) : IRequest<Result<Response>>;
