namespace NetForge.Core.Utilities;

public static class ForgeGuard
{
    public static T AgainstNull<T>(T? value, string paramName)
    {
        if (value is null) throw new ArgumentNullException(paramName);
        return value;
    }

    public static string AgainstNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Parameter '{paramName}' cannot be empty", paramName);
        return value;
    }

    public static int AgainstNonPositive(int value, string paramName)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(paramName, value, $"Parameter '{paramName}' must be > 0");
        return value;
    }
}
