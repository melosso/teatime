using Teatime.Services.Theming.Structures;

namespace Teatime.Services.Theming;

/// <summary>Built-in page structures. To add your own, implement <see cref="ITeatimeStructure"/> and add a line to <see cref="All"/>.</summary>
public static class StructureRegistry
{
    public static IReadOnlyList<ITeatimeStructure> All { get; } =
    [
        new DefaultStructure(),
        new EditorialStructure()
    ];

    public static ITeatimeStructure Default { get; } = All[0];

    /// <summary>Unknown names warn and fall back to the default; a typo must never take a site down.</summary>
    public static ITeatimeStructure Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Default;

        var trimmed = name.Trim();
        foreach (var structure in All)
        {
            if (string.Equals(structure.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                return structure;
        }

        Serilog.Log.Warning(
            "Unknown structure {Structure}; falling back to {Default}. Available: {Available}",
            trimmed, Default.Name, string.Join(", ", All.Select(s => s.Name)));
        return Default;
    }
}
