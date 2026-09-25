using System.Diagnostics;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Generic "is this CLI tool installed, and what version" check.
/// Used for Node.js, Python, Git and Docker, each configured as a separate instance.
/// </summary>
public sealed class ToolVersionCheck : IHealthCheck
{
    private readonly string _displayName;
    private readonly string _executable;
    private readonly string _versionArg;
    private readonly bool _required;

    public string Name => _displayName;

    public ToolVersionCheck(string displayName, string executable, string versionArg = "--version", bool required = true)
    {
        _displayName = displayName;
        _executable = executable;
        _versionArg = versionArg;
        _required = required;
    }

    public async Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = await ProcessRunner.RunAsync(_executable, _versionArg);
        sw.Stop();

        if (!result.Found)
        {
            return new HealthCheckResult
            {
                CategoryName = _displayName,
                Status = _required ? CheckStatus.Warning : CheckStatus.Info,
                Summary = $"{_displayName} not found on PATH",
                Details = new List<string> { $"`{_executable}` did not resolve to an executable." },
                Recommendations = new List<string> { $"Install {_displayName} and ensure `{_executable}` is on PATH, or ignore this if it's not needed for your work." },
                Score = _required ? 30 : 70,
                Weight = _required ? 1 : 1,
                Duration = sw.Elapsed
            };
        }

        var rawOutput = string.IsNullOrWhiteSpace(result.StdOut) ? result.StdErr : result.StdOut;
        var version = rawOutput.Split('\n').FirstOrDefault()?.Trim() ?? "unknown version";

        if (!result.Success)
        {
            return new HealthCheckResult
            {
                CategoryName = _displayName,
                Status = CheckStatus.Warning,
                Summary = $"{_displayName} found but returned a non-zero exit code",
                Details = new List<string> { version, $"stderr: {result.StdErr}" },
                Recommendations = new List<string> { $"Re-run `{_executable} {_versionArg}` manually to diagnose." },
                Score = 50,
                Weight = 1,
                Duration = sw.Elapsed
            };
        }

        return new HealthCheckResult
        {
            CategoryName = _displayName,
            Status = CheckStatus.Healthy,
            Summary = version,
            Details = new List<string> { $"Resolved via PATH as `{_executable}`." },
            Score = 100,
            Weight = 1,
            Duration = sw.Elapsed
        };
    }
}
