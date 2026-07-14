namespace Teatime.Configuration;

public sealed record CliArguments(string? ExportDir, string? ExportBaseUrl, string? BasePath)
{
    public static CliArguments Parse(string[] args)
    {
        string? exportDir = null;
        string? exportBaseUrl = null;
        string? basePath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--export" when i + 1 < args.Length: exportDir = args[++i]; break;
                case "--base-url" when i + 1 < args.Length: exportBaseUrl = args[++i]; break;
                case "--base-path" when i + 1 < args.Length: basePath = args[++i]; break;
            }
        }

        return new CliArguments(exportDir, exportBaseUrl, basePath);
    }
}
