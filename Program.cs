using DevSentinel.Checks;
using DevSentinel.Models;
using DevSentinel.Services;

namespace DevSentinel;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var options = CliOptions.Parse(args);

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (!options.Quiet)
            ConsoleUi.Banner();

        var checks = BuildChecks();
        var report = new EnvironmentReport();

        foreach (var check in checks)
        {
            if (!options.Quiet)
                ConsoleUi.CheckRunning(check.Name);

            HealthCheckResult result;
            try
            {
                result = await check.RunAsync();
            }
            catch (Exception ex)
            {
                // A check should never crash the whole tool — convert unexpected
                // exceptions into a Critical result for that category instead.
                result = new HealthCheckResult
                {
                    CategoryName = check.Name,
                    Status = CheckStatus.Critical,
                    Summary = "Check failed unexpectedly",
                    Details = new List<string> { ex.Message },
                    Score = 0,
                    Weight = 1
                };
            }

            report.Checks.Add(result);

            if (!options.Quiet)
                ConsoleUi.CheckResult(result);
        }

        HealthScoreCalculator.Apply(report);

        if (!options.Quiet)
            ConsoleUi.ScoreSummary(report);

        if (options.ExportFormats.Count > 0)
        {
            ExportReport(report, options);
        }

        if (!options.Quiet)
            ConsoleUi.WaitForExit();

        // Exit code reflects health: 0 = healthy/warning, 1 = at least one critical
        // issue was found. Useful for CI pipelines or scheduled tasks.
        return report.CriticalCount > 0 ? 1 : 0;
    }

    private static List<IHealthCheck> BuildChecks() => new()
    {
        new DotNetSdkCheck(),
        new ToolVersionCheck("Node.js", "node"),
        new ToolVersionCheck("Python", "python"),
        new ToolVersionCheck("Git", "git"),
        new ToolVersionCheck("Docker", "docker", required: false),
        new PathIntegrityCheck(),
        new PortUsageCheck(),
        new DependencyCheck(),
        new DiskSpaceCheck()
    };

    private static void ExportReport(EnvironmentReport report, CliOptions options)
    {
        var baseDir = options.OutputDirectory ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(baseDir);
        var stamp = report.GeneratedAtUtc.ToLocalTime().ToString("yyyyMMdd-HHmmss");
        var baseName = Path.Combine(baseDir, $"devsentinel-report-{stamp}");

        foreach (var format in options.ExportFormats)
        {
            try
            {
                var path = format switch
                {
                    ExportFormat.Json => ReportExporter.ExportJson(report, baseName + ".json"),
                    ExportFormat.Html => ReportExporter.ExportHtml(report, baseName + ".html"),
                    ExportFormat.Text => ReportExporter.ExportText(report, baseName + ".txt"),
                    _ => null
                };

                if (path != null && !options.Quiet)
                    ConsoleUi.Success($"  Report exported: {path}");
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"  Failed to export {format}: {ex.Message}");
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        DevSentinel — Developer Environment Health Monitor

        USAGE:
          DevSentinel.exe [options]

        OPTIONS:
          --export <fmt>     Export report. fmt = json | html | txt | all
                              May be passed multiple times, e.g. --export json --export html
          --output <dir>     Directory to write exported report(s) to (default: current directory)
          --quiet             Suppress console progress output (exit code still reflects health)
          --help              Show this help message

        EXIT CODES:
          0   No critical issues found
          1   At least one critical issue was found

        EXAMPLES:
          DevSentinel.exe
          DevSentinel.exe --export html --output C:\Reports
          DevSentinel.exe --export all --quiet
        """);
    }
}
