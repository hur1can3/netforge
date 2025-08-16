namespace NetForge.Core.Results;

public sealed record ForgeError(string Code, string Message, ForgeErrorSeverity Severity = ForgeErrorSeverity.Failure)
{
    public static ForgeError Validation(string field, string message) => new($"Validation:{field}", message, ForgeErrorSeverity.Validation);
    public static ForgeError NotFound(string entity, string message) => new($"NotFound:{entity}", message, ForgeErrorSeverity.NotFound);
    public static ForgeError Conflict(string entity, string message) => new($"Conflict:{entity}", message, ForgeErrorSeverity.Conflict);
    public static ForgeError Unexpected(string message) => new("Unexpected", message, ForgeErrorSeverity.Failure);
}

public enum ForgeErrorSeverity { Validation, NotFound, Conflict, Failure }
