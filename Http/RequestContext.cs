using System.Net;
using System.Text;
using System.Text.Json;
using SkillIssue.Http.Errors;
using SkillIssue.Http.Routing;
using SkillIssue.Modules.Users.Models;

namespace SkillIssue.Http;

public sealed class RequestContext(HttpListenerContext context, CancellationToken cancellationToken)
{
    public HttpListenerRequest Request => context.Request;
    public HttpListenerResponse Response => context.Response;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public Session? Session { get; internal set; }
    public User? User { get; internal set; }
    public UserRole Role => User?.Role ?? UserRole.Guest;
    public string Path => Request.Url?.AbsolutePath ?? "/";
    internal bool IsApiRequest =>
        Path.StartsWith("/api/", StringComparison.Ordinal)
        || Router.GetPathSegments(Path) is ["api", _, ..];
    public bool HasStarted { get; private set; }
    public IReadOnlyDictionary<string, string> RouteValues { get; internal set; } =
        new Dictionary<string, string>();

    public async Task<T> ReadJsonAsync<T>()
        where T : class
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(
                    Request.InputStream,
                    JsonSerializerOptions.Web,
                    CancellationToken
                ) ?? throw new HttpException(ApiError.BadRequest);
        }
        catch (JsonException exception)
        {
            throw new HttpException(ApiError.BadRequest, innerException: exception);
        }
    }

    public Task WriteJsonAsync<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return WriteTextAsync(
            JsonSerializer.Serialize(value),
            "application/json; charset=utf-8",
            statusCode
        );
    }

    public async Task WriteTextAsync(
        string content,
        string contentType,
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        StartResponse(statusCode, contentType, bytes.Length);

        if (Request.HttpMethod != "HEAD")
        {
            await Response.OutputStream.WriteAsync(bytes, CancellationToken);
        }
    }

    public async Task WriteFileAsync(string path, string contentType)
    {
        await using var file = File.OpenRead(path);
        StartResponse(HttpStatusCode.OK, contentType, file.Length);

        if (Request.HttpMethod != "HEAD")
        {
            await file.CopyToAsync(Response.OutputStream, CancellationToken);
        }
    }

    public Task WriteErrorAsync(ApiError error, string? message = null)
    {
        HttpStatusCode statusCode = error.StatusCode;
        message ??= error.Message;

        if (IsApiRequest)
        {
            return WriteJsonAsync(new { code = error.Code, message }, statusCode);
        }

        if (statusCode == HttpStatusCode.Unauthorized)
        {
            Response.RedirectLocation = "/signin";
            StartResponse(HttpStatusCode.Found, "text/plain; charset=utf-8", 0);
            return Task.CompletedTask;
        }

        string html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{{(int)statusCode}} — skill_issue</title>
                <link rel="icon" type="image/svg+xml" href="/static/favicon.svg">
                <link rel="preload" href="/static/fonts/jetbrains-mono/JetBrainsMonoNerdFontMono-Regular-Web.woff2" as="font" type="font/woff2" crossorigin>
                <link rel="stylesheet" href="/static/css/fonts.css">
                <script type="module" src="/static/js/shared/accent.js"></script>
                <link rel="stylesheet" href="/static/css/variables.css">
                <link rel="stylesheet" href="/static/css/base.css">
                <link rel="stylesheet" href="/static/css/ui.css">
            </head>
            <body>
                <main class="page-main">
                    <h1>{{(int)statusCode}}</h1>
                    <p>{{WebUtility.HtmlEncode(message)}}</p>
                    <a href="/">Home</a>
                </main>
            </body>
            </html>
            """;

        return WriteTextAsync(html, "text/html; charset=utf-8", statusCode);
    }

    private void StartResponse(HttpStatusCode statusCode, string contentType, long contentLength)
    {
        Response.StatusCode = (int)statusCode;
        Response.ContentType = contentType;
        Response.ContentLength64 = contentLength;
        HasStarted = true;
    }
}
