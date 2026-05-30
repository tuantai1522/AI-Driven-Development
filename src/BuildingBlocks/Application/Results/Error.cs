namespace BuildingBlocks.Application.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error Failure = new("common.failure", "The request failed.", ErrorType.Failure);
    public static readonly Error Validation = new("common.validation", "The request failed validation.", ErrorType.Validation);
    public static readonly Error NotFound = new("common.not_found", "The requested resource was not found.", ErrorType.NotFound);
    public static readonly Error Unauthorized = new("common.unauthorized", "The current user is not authenticated.", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("common.forbidden", "The current user does not have access to this resource.", ErrorType.Forbidden);
    public static readonly Error Unexpected = new("common.unexpected", "An unexpected error occurred.", ErrorType.Unexpected);
}
