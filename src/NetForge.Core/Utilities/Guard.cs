namespace NetForge.Core.Utilities;

public static class ForgeGuard
{
    public static void AgainstNull(object? value, string paramName)
    {
        if (value is null) throw new ArgumentNullException(paramName);
    }
    public static void AgainstNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Parameter '{paramName}' cannot be empty", paramName);
    }
}
