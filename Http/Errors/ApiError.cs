using System.Net;

namespace SkillIssue.Http.Errors;

public sealed record ApiError(HttpStatusCode StatusCode, string Code, string Message)
{
    public static readonly ApiError BadRequest = new(
        HttpStatusCode.BadRequest,
        "bad_request",
        "Invalid request."
    );

    public static readonly ApiError Unauthorized = new(
        HttpStatusCode.Unauthorized,
        "unauthorized",
        "Please sign in to continue."
    );

    public static readonly ApiError Forbidden = new(
        HttpStatusCode.Forbidden,
        "forbidden",
        "Access denied."
    );

    public static readonly ApiError NotFound = new(
        HttpStatusCode.NotFound,
        "not_found",
        "Not found."
    );

    public static readonly ApiError MethodNotAllowed = new(
        HttpStatusCode.MethodNotAllowed,
        "method_not_allowed",
        "Method not allowed."
    );

    public static readonly ApiError Conflict = new(
        HttpStatusCode.Conflict,
        "conflict",
        "Request conflict. Please try again."
    );

    public static readonly ApiError InternalServerError = new(
        HttpStatusCode.InternalServerError,
        "internal_server_error",
        "Something went wrong. Try again."
    );

    public static readonly ApiError ProjectAiUnavailable = new(
        HttpStatusCode.ServiceUnavailable,
        "project_ai_unavailable",
        ErrorMessages.ProjectAiUnavailable
    );
}
