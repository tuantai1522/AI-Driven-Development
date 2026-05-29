namespace Todo.Api.Features.Sample.Create;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
