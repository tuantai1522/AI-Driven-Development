namespace BuildingBlocks.Application.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    Conflict = 2,
    NotFound = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
