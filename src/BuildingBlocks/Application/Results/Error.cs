namespace BuildingBlocks.Application.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error NotFound = new("common.not_found", "The requested resource was not found.");
    public static readonly Error Validation = new("common.validation", "The request failed validation.");
}
