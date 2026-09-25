using System.Diagnostics;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Detects installed .NET SDKs (and, as detail, installed runtimes) via `dotnet --list-sdks`.
/// </summary>
public sealed class DotNetSdkCheck : IHealthCheck
{
    public string Name => ".NET SDKs";

    public async Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var sdkResult = await ProcessRunner.RunAsync("dotnet", "--list-sdks");
        var runtimeResult = await ProcessRunner.RunAsync("dotnet", "--list-runtimes");
        sw.Stop();

        if (!sdkResult.Found)
        {
            return new HealthCheckResult
            {
                CategoryName = Name,
                Status = CheckStatus.Critical,
                Summary = "dotnet CLI not found on PATH",
                Details = new List<string> { "No `dotnet` executable was found. .NET development is not possible until this is installed." },
                Recommendations = new List<string> { "Install the .NET SDK from https://dotnet.microsoft.com/download and ensure it is added to PATH." },
                Score = 0,
                Weight = 2,
                Duration = sw.Elapsed
            };
        }

        var sdkLines = SplitLines(sdkResult.StdOut);
        var runtimeLines = SplitLines(runtimeResult.StdOut);

        var details = new List<string>();
        if (sdkLines.Count > 0)
        {
            details.Add($"SDKs installed: {sdkLines.Count}");
            details.AddRange(sdkLines.Select(l => "  • " + l));
        }
        if (runtimeLines.Count > 0)
        {
            details.Add($"Runtimes installed: {runtimeLines.Count}");
            details.AddRange(runtimeLines.Select(l => "  • " + l));
        }

        if (sdkLines.Count == 0)
        {
            return new HealthCheckResult
            {
                CategoryName = Name,
                Status = CheckStatus.Critical,
                Summary = "dotnet CLI found, but no SDKs are installed",
                Details = details,
                Recommendations = new List<string> { "Install at least one .NET SDK version via the official installer or `winget install Microsoft.DotNet.SDK.8`." },
                Score = 20,
                Weight = 2,
                Duration = sw.Elapsed
            };
        }

        var latest = sdkLines.LastOrDefault() ?? "unknown";
        return new HealthCheckResult
        {
            CategoryName = Name,
            Status = CheckStatus.Healthy,
            Summary = $"{sdkLines.Count} SDK(s) installed (latest: {latest.Split(' ').FirstOrDefault()})",
            Details = details,
            Score = 100,
            Weight = 2,
            Duration = sw.Elapsed
        };
    }

    private static List<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();
}
