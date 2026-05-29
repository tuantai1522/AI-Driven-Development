namespace BuildingBlocks.Application.Exceptions;

public sealed class RequestValidationException(Dictionary<string, string[]> errors) : Exception("Validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
