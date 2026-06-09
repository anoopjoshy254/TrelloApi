namespace TrelloApi.Helpers;

/// <summary>
/// Generic API response wrapper for consistent response structure.
/// Every endpoint returns this type so clients have a predictable contract.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Request successful")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Fail(List<string> errors)
        => new() { Success = false, Message = "Validation failed", Errors = errors };
}

/// <summary>
/// Non-generic version for endpoints that return no data payload.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }

    public static ApiResponse Ok(string message = "Request successful")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Paginated list wrapper returned by list endpoints supporting pagination.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
