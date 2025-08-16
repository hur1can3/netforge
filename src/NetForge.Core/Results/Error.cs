namespace NetForge.Core.Results;

public sealed record Error(string Code, string Message, ErrorSeverity Severity = ErrorSeverity.Failure)
{
    public static Error Validation(string field, string message) => new($"Validation:{field}", message, ErrorSeverity.Validation);
    public static Error NotFound(string entity, string message) => new($"NotFound:{entity}", message, ErrorSeverity.NotFound);
    public static Error Conflict(string entity, string message) => new($"Conflict:{entity}", message, ErrorSeverity.Conflict);
    public static Error Unexpected(string message) => new("Unexpected", message, ErrorSeverity.Failure);
}

public enum ErrorSeverity { Validation, NotFound, Conflict, Failure }
