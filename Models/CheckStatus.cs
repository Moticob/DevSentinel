namespace DevSentinel.Models;

/// <summary>
/// Severity/outcome of a single health check.
/// </summary>
public enum CheckStatus
{
    Healthy,
    Info,
    Warning,
    Critical,
    NotApplicable
}
