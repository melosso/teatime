using Teatime.Configuration;
using Teatime.Services.Extensions;

namespace Teatime.Endpoints;

internal static class AltchaEndpoints
{
    public static IEndpointRouteBuilder MapAltchaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/altcha");
        group.MapGet("/challenge", (AltchaService service) => Results.Json(service.Create()));
        group.MapPost("/verify", VerifyAsync);
        return app;
    }

    private static async Task<IResult> VerifyAsync(HttpContext context, AltchaService service)
    {
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        if (!service.Verify(form["altcha"].ToString()))
            return Results.BadRequest();

        context.Response.Cookies.Append(AltchaGate.CookieName, AltchaGate.IssueSessionCookieValue(context), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = AltchaGate.SessionLifetime(context),
            Path = "/",
        });
        return Results.Ok();
    }
}
