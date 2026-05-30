using BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.ExceptionHandling;

public static class ErrorProblemDetailsMapper
{
    public static ProblemDetails Map(Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unexpected => (StatusCodes.Status500InternalServerError, "Unexpected Error"),
            _ => (StatusCodes.Status400BadRequest, "Request Failed")
        };

        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = error.Message,
            Type = error.Code
        };
    }
}
