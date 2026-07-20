namespace Teatime.Services.Extensions;

/// <summary>What the newsletter back end supports, published here because blocks render at parse time.</summary>
public sealed record NewsletterAvailability(bool Enabled, bool CollectsName)
{
    public static readonly NewsletterAvailability None = new(false, false);

    private static volatile NewsletterAvailability _current = None;

    public static NewsletterAvailability Current
    {
        get => _current;
        set => _current = value;
    }
}
