namespace DevSentinel.Models;

/// <summary>
/// Aggregate report produced after running all health checks.
/// </summary>
public sealed class EnvironmentReport
{
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string MachineName { get; init; } = Environment.MachineName;
    public string UserName { get; init; } = Environment.UserName;
    public string OSDescription { get; init; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string OSArchitecture { get; init; } = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
    public string ToolVersion { get; init; } = "1.0.0";

    public List<HealthCheckResult> Checks { get; init; } = new();

    /// <summary>Weighted overall score, 0-100.</summary>
    public int OverallScore { get; set; }

    /// <summary>Letter grade derived from <see cref="OverallScore"/>.</summary>
    public string Grade { get; set; } = "N/A";

    public int CriticalCount => Checks.Count(c => c.Status == CheckStatus.Critical);
    public int WarningCount => Checks.Count(c => c.Status == CheckStatus.Warning);
    public int HealthyCount => Checks.Count(c => c.Status == CheckStatus.Healthy);
}
