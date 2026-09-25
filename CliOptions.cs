namespace DevSentinel;

internal enum ExportFormat { Json, Html, Text }

internal sealed class CliOptions
{
    public bool ShowHelp { get; private set; }
    public bool Quiet { get; private set; }
    public string? OutputDirectory { get; private set; }
    public List<ExportFormat> ExportFormats { get; } = new();

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;

                case "--quiet":
                case "-q":
                    options.Quiet = true;
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputDirectory = args[++i];
                    }
                    break;

                case "--export":
                case "-e":
                    if (i + 1 < args.Length)
                    {
                        AddFormat(options, args[++i]);
                    }
                    break;

                default:
                    // Unknown args are ignored rather than treated as fatal errors,
                    // keeping the tool friendly to run without reading docs first.
                    break;
            }
        }

        return options;
    }

    private static void AddFormat(CliOptions options, string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "json":
                AddIfMissing(options.ExportFormats, ExportFormat.Json);
                break;
            case "html":
                AddIfMissing(options.ExportFormats, ExportFormat.Html);
                break;
            case "txt":
            case "text":
                AddIfMissing(options.ExportFormats, ExportFormat.Text);
                break;
            case "all":
                AddIfMissing(options.ExportFormats, ExportFormat.Json);
                AddIfMissing(options.ExportFormats, ExportFormat.Html);
                AddIfMissing(options.ExportFormats, ExportFormat.Text);
                break;
        }
    }

    private static void AddIfMissing(List<ExportFormat> list, ExportFormat format)
    {
        if (!list.Contains(format)) list.Add(format);
    }
}
