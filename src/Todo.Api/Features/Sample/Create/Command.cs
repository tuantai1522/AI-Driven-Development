using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Sample.Create;

public sealed record Command(string Name) : IRequest<Result<Response>>;
