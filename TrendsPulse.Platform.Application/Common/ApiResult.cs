namespace TrendsPulse.Platform.Application.Common;

public sealed class ApiResult<T>
{
    public bool   Success { get; private init; }
    public T?     Data    { get; private init; }
    public string? Message { get; private init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; private init; }

    public static ApiResult<T> Ok(T data, string? message = null)      => new() { Success = true,  Data = data, Message = message };
    public static ApiResult<T> Fail(string message)                     => new() { Success = false, Message = message };
    public static ApiResult<T> Fail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Success = false, Errors = errors, Message = "Validation failed." };
}

public sealed class PagedResult<T>
{
    public IEnumerable<T> Items      { get; init; } = Enumerable.Empty<T>();
    public int            TotalCount { get; init; }
    public int            Page       { get; init; }
    public int            PageSize   { get; init; }
    public int            TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool           HasPreviousPage => Page > 1;
    public bool           HasNextPage     => Page < TotalPages;
}
