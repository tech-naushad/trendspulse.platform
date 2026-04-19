namespace TrendsPulse.Platform.Application.Tests.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.") { }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Thrown by FluentValidation pipeline behaviour.
/// Contains field-level errors.
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
