namespace Vowlt.Api.Shared.Models;

/// <summary>
/// Standardized error response for all API endpoints.
/// Provides both a single error message and a list of all errors for validation scenarios.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Primary error message (first error from the list).
    /// Always populated when IsSuccess = false.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// List of all error messages.
    /// Useful for validation errors where multiple fields may be invalid.
    /// </summary>
    public required List<string> Errors { get; init; }

    /// <summary>
    /// Creates an ErrorResponse from a Result.
    /// </summary>
    public static ErrorResponse FromResult<T>(Result<T> result)
    {
        return new ErrorResponse
        {
            Error = result.Error ?? "An unknown error occurred",
            Errors = result.Errors
        };
    }

    /// <summary>
    /// Creates an ErrorResponse from a non-generic Result.
    /// </summary>
    public static ErrorResponse FromResult(Result result)
    {
        return new ErrorResponse
        {
            Error = result.Error ?? "An unknown error occurred",
            Errors = result.Errors
        };
    }

    /// <summary>
    /// Creates an ErrorResponse from a single error message.
    /// </summary>
    public static ErrorResponse FromMessage(string error)
    {
        return new ErrorResponse
        {
            Error = error,
            Errors = [error]
        };
    }
}

