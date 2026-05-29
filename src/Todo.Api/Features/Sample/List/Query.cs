using BuildingBlocks.Application.Results;
using MediatR;

namespace Todo.Api.Features.Sample.List;

public sealed record Query() : IRequest<Result<IReadOnlyList<Response>>>;
