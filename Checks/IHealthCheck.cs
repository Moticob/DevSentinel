using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Contract for a single environment health check.
/// </summary>
public interface IHealthCheck
{
    /// <summary>Short display name used in progress output.</summary>
    string Name { get; }

    /// <summary>Runs the check and returns its result. Must not throw for expected failures
    /// (missing tools, etc.) — those should be reflected in the result itself.</summary>
    Task<HealthCheckResult> RunAsync();
}
