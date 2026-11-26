using Npgsql;

namespace SkillIssue.Configuration;

public sealed class AppSettings(string url, string databaseConnectionString)
{
    private const string InvalidUrl = "Invalid URL.";
    private const string InvalidDatabasePort = "POSTGRES_PORT must be a number from 1 to 65535.";
    private const string MissingEnvironmentVariable = "Missing environment variable: {0}.";

    public string Url { get; } = url;
    public string DatabaseConnectionString { get; } = databaseConnectionString;
    public string? LlmApiKey { get; private init; }
    public string? LlmEndpoint { get; private init; }
    public string? LlmModel { get; private init; }

    public static AppSettings FromEnvironment()
    {
        string value = "http://localhost:5173/";

        if (
            !Uri.TryCreate(value, UriKind.Absolute, out var url)
            || url.Scheme != Uri.UriSchemeHttp
            || url.AbsolutePath != "/"
            || url.UserInfo.Length != 0
            || url.Query.Length != 0
            || url.Fragment.Length != 0
        )
        {
            throw new ArgumentException(InvalidUrl);
        }

        if (
            !int.TryParse(GetRequiredVariable("POSTGRES_PORT"), out int port)
            || port is < 1 or > 65535
        )
        {
            throw new ArgumentException(InvalidDatabasePort);
        }

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = GetRequiredVariable("POSTGRES_HOST"),
            Port = port,
            Database = GetRequiredVariable("POSTGRES_DB"),
            Username = GetRequiredVariable("POSTGRES_USER"),
            Password = GetRequiredVariable("POSTGRES_PASSWORD"),
        };

        return new AppSettings(url.AbsoluteUri, connectionString.ConnectionString)
        {
            LlmApiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")?.Trim(),
            LlmEndpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT")?.Trim(),
            LlmModel = Environment.GetEnvironmentVariable("LLM_MODEL")?.Trim(),
        };
    }

    private static string GetRequiredVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(string.Format(MissingEnvironmentVariable, name));
        }

        return value;
    }
}
