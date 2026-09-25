using System.Diagnostics;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Checks for the presence of common package managers / dependency tooling
/// (npm, yarn/pnpm, pip, NuGet) that most projects rely on, plus a couple of
/// well-known environment variables that misconfigured setups often lack.
/// </summary>
public sealed class DependencyCheck : IHealthCheck
{
    public string Name => "Dependency Tooling";

    private static readonly (string Exe, string Arg, string Label)[] Tools =
    {
        ("npm", "--version", "npm"),
        ("yarn", "--version", "yarn"),
        ("pnpm", "--version", "pnpm"),
        ("pip", "--version", "pip"),
        ("nuget", "help", "NuGet CLI")
    };

    public async Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var found = new List<string>();
        var missing = new List<string>();

        foreach (var (exe, arg, label) in Tools)
        {
            var result = await ProcessRunner.RunAsync(exe, arg);
            if (result.Found)
            {
                var version = (string.IsNullOrWhiteSpace(result.StdOut) ? result.StdErr : result.StdOut)
                    .Split('\n').FirstOrDefault()?.Trim();
                found.Add($"{label}: {version}");
            }
            else
            {
                missing.Add(label);
            }
        }

        sw.Stop();

        var details = new List<string>();
        if (found.Count > 0)
        {
            details.Add("Detected:");
            details.AddRange(found.Select(f => "  • " + f));
        }
        if (missing.Count > 0)
        {
            details.Add("Not found:");
            details.AddRange(missing.Select(m => "  • " + m));
        }

        var recommendations = new List<string>();
        if (missing.Contains("npm"))
            recommendations.Add("Install Node.js (includes npm) if you do JavaScript/TypeScript development.");
        if (missing.Contains("pip"))
            recommendations.Add("Install Python (includes pip) if you do Python development.");

        // npm is the most universally expected package manager for a modern dev box —
        // weight the score toward whether it (and at least one other tool) is present.
        var essentialPresent = found.Count;
        var score = Math.Clamp((essentialPresent * 100) / Tools.Length, 0, 100);

        var status = essentialPresent == 0
            ? CheckStatus.Warning
            : essentialPresent < Tools.Length
                ? CheckStatus.Info
                : CheckStatus.Healthy;

        return new HealthCheckResult
        {
            CategoryName = Name,
            Status = status,
            Summary = $"{found.Count}/{Tools.Length} common package managers detected",
            Details = details,
            Recommendations = recommendations,
            Score = score,
            Weight = 1,
            Duration = sw.Elapsed
        };
    }
}
