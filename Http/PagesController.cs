using SkillIssue.Http.Routing;
using SkillIssue.Templates;

namespace SkillIssue.Http;

public sealed class PagesController
{
    public static Task HomeAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "Home.html");
    }

    public static Task ProjectsAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "Projects.html");
    }

    public static Task SignInAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "SignIn.html");
    }

    public static Task ProfileAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "Profile.html");
    }

    public static Task MyProjectsAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "MyProjects.html");
    }

    public static Task SignUpAsync(RequestContext context)
    {
        return WritePageWithHeaderAsync(context, "SignUp.html");
    }

    private static Task WritePageWithHeaderAsync(RequestContext context, string fileName)
    {
        var user = context.User;
        string currentPath = "/" + string.Join('/', Router.GetPathSegments(context.Path));
        var headerData = new SiteHeaderData(
            CurrentPath: currentPath,
            IsSignedIn: user is not null,
            AccountName: user?.FullName ?? string.Empty
        );

        return WritePageAsync(context, fileName, headerData);
    }

    private static async Task WritePageAsync(
        RequestContext context,
        string fileName,
        ITemplateData? templateData = null
    )
    {
        string html = await TemplateEngine.RenderAsync(
            fileName,
            templateData,
            context.CancellationToken
        );
        await context.WriteTextAsync(html, "text/html; charset=utf-8");
    }
}
