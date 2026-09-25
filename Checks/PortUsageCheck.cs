using System.Diagnostics;
using System.Net.NetworkInformation;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Reports which commonly-used development ports (web servers, databases, message
/// brokers, etc.) currently have a TCP listener bound to them. Useful for spotting
/// "why won't my server start" (port already in use) situations at a glance.
/// </summary>
public sealed class PortUsageCheck : IHealthCheck
{
    public string Name => "Port Usage";

    private static readonly (int Port, string Service)[] WatchedPorts =
    {
        (3000, "Node/React/Next.js dev server"),
        (3001, "Alt Node dev server"),
        (4200, "Angular CLI"),
        (5000, "ASP.NET Core / Flask"),
        (5001, "ASP.NET Core (HTTPS)"),
        (5173, "Vite dev server"),
        (5432, "PostgreSQL"),
        (5672, "RabbitMQ"),
        (6379, "Redis"),
        (7071, "Azure Functions Core Tools"),
        (8000, "Django / generic dev server"),
        (8080, "Generic HTTP / Tomcat"),
        (8081, "Generic HTTP alt"),
        (8443, "Generic HTTPS"),
        (9000, "PHP-FPM / SonarQube"),
        (9200, "Elasticsearch"),
        (27017, "MongoDB"),
        (1433, "SQL Server"),
        (3306, "MySQL/MariaDB")
    };

    public Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();

        List<int> activeListenerPorts;
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            activeListenerPorts = props.GetActiveTcpListeners()
                .Select(l => l.Port)
                .ToList();
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(new HealthCheckResult
            {
                CategoryName = Name,
                Status = CheckStatus.Info,
                Summary = "Unable to enumerate active TCP listeners",
                Details = new List<string> { ex.Message },
                Score = 60,
                Weight = 1,
                Duration = sw.Elapsed
            });
        }

        var activePorts = new HashSet<int>(activeListenerPorts);
        var busy = WatchedPorts.Where(w => activePorts.Contains(w.Port)).ToList();

        sw.Stop();

        var details = new List<string> { $"Active TCP listeners on this machine: {activePorts.Count}" };
        if (busy.Count > 0)
        {
            details.Add("Watched dev ports currently in use:");
            details.AddRange(busy.Select(b => $"  • {b.Port} — {b.Service}"));
        }
        else
        {
            details.Add("None of the commonly-watched development ports are currently occupied.");
        }

        return Task.FromResult(new HealthCheckResult
        {
            CategoryName = Name,
            Status = CheckStatus.Info,
            Summary = busy.Count == 0
                ? "All watched development ports are free"
                : $"{busy.Count} watched port(s) currently in use: {string.Join(", ", busy.Select(b => b.Port))}",
            Details = details,
            Score = 100, // informational only — busy ports aren't inherently unhealthy
            Weight = 1,
            Duration = sw.Elapsed
        });
    }
}
