namespace Teatime.Services.Theming.Structures;

/// <summary>Teatime's default page shape, unchanged. Used when none is configured.</summary>
public sealed class DefaultStructure : ITeatimeStructure
{
    public string Name => "default";

    public string Label => "Default";

    public string ComponentCss => string.Empty;
}
