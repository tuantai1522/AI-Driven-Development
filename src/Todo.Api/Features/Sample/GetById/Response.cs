namespace Todo.Api.Features.Sample.GetById;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
