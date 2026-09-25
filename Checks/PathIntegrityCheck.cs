using System.Diagnostics;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Inspects the PATH environment variable for entries that point at directories
/// which no longer exist, are empty, or are duplicated — common causes of
/// "command not found" issues that are otherwise hard to diagnose.
/// </summary>
public sealed class PathIntegrityCheck : IHealthCheck
{
    public string Name => "PATH Integrity";

    public Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var entries = pathVar.Split(Path.PathSeparator, StringSplitOptions.None);

        var missing = new List<string>();
        var empty = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();
        var valid = 0;

        foreach (var raw in entries)
        {
            var entry = raw.Trim();
            if (string.IsNullOrEmpty(entry))
            {
                empty.Add("(blank PATH entry)");
                continue;
            }

            if (!seen.Add(entry))
            {
                duplicates.Add(entry);
                continue;
            }

            if (!Directory.Exists(entry))
            {
                missing.Add(entry);
            }
            else
            {
                valid++;
            }
        }

        sw.Stop();

        var totalEntries = entries.Length;
        var problems = missing.Count + empty.Count + duplicates.Count;

        var details = new List<string> { $"Total PATH entries: {totalEntries} (valid: {valid})" };
        if (missing.Count > 0)
        {
            details.Add($"Non-existent directories ({missing.Count}):");
            details.AddRange(missing.Select(m => "  • " + m));
        }
        if (duplicates.Count > 0)
        {
            details.Add($"Duplicate entries ({duplicates.Count}):");
            details.AddRange(duplicates.Select(d => "  • " + d));
        }
        if (empty.Count > 0)
        {
            details.Add($"Blank entries: {empty.Count}");
        }

        var recommendations = new List<string>();
        if (missing.Count > 0)
            recommendations.Add("Remove PATH entries pointing at directories that no longer exist (leftover from uninstalled tools).");
        if (duplicates.Count > 0)
            recommendations.Add("Deduplicate PATH via System Properties → Environment Variables, or a PowerShell profile cleanup script.");
        if (empty.Count > 0)
            recommendations.Add("Remove stray blank/trailing separators from PATH.");

        CheckStatus status;
        int score;
        string summary;

        if (problems == 0)
        {
            status = CheckStatus.Healthy;
            score = 100;
            summary = $"PATH is clean — {valid} valid entries, no issues found";
        }
        else if (missing.Count > 5 || duplicates.Count > 5)
        {
            status = CheckStatus.Warning;
            score = 40;
            summary = $"PATH has {problems} issue(s): {missing.Count} missing, {duplicates.Count} duplicate, {empty.Count} blank";
        }
        else
        {
            status = CheckStatus.Warning;
            score = 70;
            summary = $"PATH has {problems} minor issue(s): {missing.Count} missing, {duplicates.Count} duplicate, {empty.Count} blank";
        }

        return Task.FromResult(new HealthCheckResult
        {
            CategoryName = Name,
            Status = status,
            Summary = summary,
            Details = details,
            Recommendations = recommendations,
            Score = score,
            Weight = 1,
            Duration = sw.Elapsed
        });
    }
}
