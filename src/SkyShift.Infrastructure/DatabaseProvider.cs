namespace SkyShift.Infrastructure;

/// <summary>
/// The relational database the API runs against, chosen explicitly by configuration
/// (Database:Provider). SQL Server is the default, so existing local setups are unchanged.
/// </summary>
public enum DatabaseProvider
{
    SqlServer,
    PostgreSql
}

/// <summary>
/// Parses the configured provider name. An empty value means SQL Server; an unknown value
/// fails fast at startup instead of silently falling back to a different database.
/// </summary>
public static class DatabaseProviderNames
{
    public const string SqlServer = "SqlServer";
    public const string PostgreSql = "PostgreSQL";

    public static DatabaseProvider Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), SqlServer, StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.SqlServer;

        if (string.Equals(value.Trim(), PostgreSql, StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.PostgreSql;

        throw new InvalidOperationException(
            $"Unsupported 'Database:Provider' value '{value}'. Use '{SqlServer}' or '{PostgreSql}'.");
    }
}
