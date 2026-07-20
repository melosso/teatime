namespace Teatime.Configuration;

// Policy names as constants; a literal typo silently unprotects an endpoint, a typo here is a compile error
internal static class RateLimitPolicies
{
    public const string Search = "search-limit";

    // Newsletter sign-up can trigger a confirmation email, so it is held to a much tighter budget
    public const string Subscribe = "subscribe-limit";
}
