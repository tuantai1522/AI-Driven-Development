using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Sample.GetById;

public sealed record Query(Guid Id) : IRequest<Result<Response>>;
