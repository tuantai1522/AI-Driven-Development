namespace AppModules.Sample.Features.GetById;

public sealed record Response(Guid Id, string Name, DateTime CreatedUtc);
