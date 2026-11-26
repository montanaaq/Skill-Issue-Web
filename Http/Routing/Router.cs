using SkillIssue.Http.Errors;
using SkillIssue.Modules.Users;

namespace SkillIssue.Http.Routing;

public sealed class Router
{
    private readonly List<Entry> _entries = [];

    public void Map(
        string method,
        string template,
        RequestDelegate handler,
        params UserRole[] allowedRoles
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(allowedRoles);

        if (!template.StartsWith('/') || template.Contains('?') || template.Contains('#'))
        {
            throw new ArgumentException(ErrorMessages.InvalidRouteTemplate, nameof(template));
        }

        method = method.ToUpperInvariant();
        string[] segments = Split(template);
        string pattern = string.Join(
            '/',
            segments.Select(segment => IsParameter(segment) ? "{}" : segment)
        );

        if (_entries.Any(entry => entry.Method == method && entry.Pattern == pattern))
        {
            throw new InvalidOperationException(ErrorMessages.DuplicateRoute);
        }

        var roles =
            allowedRoles.Length == 0 ? [UserRole.User, UserRole.Admin] : allowedRoles.ToArray();

        _entries.Add(new Entry(method, segments, pattern, handler, roles));
        _entries.Sort(
            (left, right) =>
                left.Segments.Count(IsParameter).CompareTo(right.Segments.Count(IsParameter))
        );
    }

    public RouteMatch? Match(string method, string path)
    {
        string[] segments = GetPathSegments(path);
        var allowed = new SortedSet<string>(StringComparer.Ordinal);
        RouteMatch? headFallback = null;

        foreach (var entry in _entries)
        {
            if (entry.Segments.Length != segments.Length)
            {
                continue;
            }

            var values = new Dictionary<string, string>();
            bool matches = true;

            for (int index = 0; index < segments.Length; index++)
            {
                string pattern = entry.Segments[index];
                if (IsParameter(pattern))
                {
                    values[pattern[1..^1]] = segments[index];
                }
                else if (pattern != segments[index])
                {
                    matches = false;
                    break;
                }
            }

            if (!matches)
            {
                continue;
            }

            allowed.Add(entry.Method);
            if (entry.Method == method)
            {
                return new RouteMatch(entry.Handler, values, [], entry.AllowedRoles);
            }

            if (entry.Method == "GET")
            {
                allowed.Add("HEAD");
                headFallback ??= new RouteMatch(entry.Handler, values, [], entry.AllowedRoles);
            }
        }

        if (method == "HEAD" && headFallback is not null)
        {
            return headFallback;
        }

        return allowed.Count == 0
            ? null
            : new RouteMatch(null, new Dictionary<string, string>(), allowed.ToArray(), []);
    }

    internal static string[] GetPathSegments(string path) =>
        Split(path).Select(Uri.UnescapeDataString).ToArray();

    private static string[] Split(string path) =>
        path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static bool IsParameter(string segment) =>
        segment.Length > 2 && segment.StartsWith('{') && segment.EndsWith('}');
}
