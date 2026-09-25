namespace DevSentinel.Models;

/// <summary>
/// Result produced by a single <see cref="Checks.IHealthCheck"/> implementation.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>Display name of the category, e.g. ".NET SDKs".</summary>
    public required string CategoryName { get; init; }

    /// <summary>Overall status for this category.</summary>
    public CheckStatus Status { get; init; } = CheckStatus.Info;

    /// <summary>One-line human-readable summary.</summary>
    public required string Summary { get; init; }

    /// <summary>Extra detail lines (rendered as a bullet list in reports).</summary>
    public List<string> Details { get; init; } = new();

    /// <summary>Actionable suggestions to fix issues found by this check.</summary>
    public List<string> Recommendations { get; init; } = new();

    /// <summary>Score contribution for this category, 0-100.</summary>
    public int Score { get; init; }

    /// <summary>Relative weight of this category when computing the overall score.</summary>
    public int Weight { get; init; } = 1;

    /// <summary>How long the check took to run.</summary>
    public TimeSpan Duration { get; init; }
}
