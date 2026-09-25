using System.Diagnostics;
using DevSentinel.Models;

namespace DevSentinel.Checks;

/// <summary>
/// Monitors free disk space on all ready, fixed drives. Low disk space is one of
/// the most common (and easy to miss) causes of build failures, failed installs,
/// and IDE slowdowns.
/// </summary>
public sealed class DiskSpaceCheck : IHealthCheck
{
    private const double CriticalFreePercent = 5.0;
    private const double WarningFreePercent = 15.0;
    private const long CriticalFreeBytes = 2L * 1024 * 1024 * 1024;   // 2 GB
    private const long WarningFreeBytes = 10L * 1024 * 1024 * 1024;   // 10 GB

    public string Name => "Disk Space";

    public Task<HealthCheckResult> RunAsync()
    {
        var sw = Stopwatch.StartNew();
        var details = new List<string>();
        var recommendations = new List<string>();

        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .ToList();

        if (drives.Count == 0)
        {
            sw.Stop();
            return Task.FromResult(new HealthCheckResult
            {
                CategoryName = Name,
                Status = CheckStatus.Info,
                Summary = "No fixed drives detected",
                Score = 100,
                Weight = 1,
                Duration = sw.Elapsed
            });
        }

        var worst = CheckStatus.Healthy;
        var minScore = 100;

        foreach (var drive in drives)
        {
            var totalBytes = drive.TotalSize;
            var freeBytes = drive.AvailableFreeSpace;
            var freePercent = totalBytes > 0 ? (freeBytes * 100.0 / totalBytes) : 0;

            var freeGb = freeBytes / 1024.0 / 1024.0 / 1024.0;
            var totalGb = totalBytes / 1024.0 / 1024.0 / 1024.0;

            CheckStatus driveStatus;
            int driveScore;

            if (freePercent <= CriticalFreePercent || freeBytes <= CriticalFreeBytes)
            {
                driveStatus = CheckStatus.Critical;
                driveScore = 10;
                recommendations.Add($"Drive {drive.Name} is critically low on space ({freeGb:F1} GB free) — free up space immediately to avoid build/install failures.");
            }
            else if (freePercent <= WarningFreePercent || freeBytes <= WarningFreeBytes)
            {
                driveStatus = CheckStatus.Warning;
                driveScore = 55;
                recommendations.Add($"Drive {drive.Name} is getting low ({freeGb:F1} GB free) — consider cleaning up build artifacts, caches, or old projects.");
            }
            else
            {
                driveStatus = CheckStatus.Healthy;
                driveScore = 100;
            }

            details.Add($"{drive.Name} ({drive.DriveFormat}): {freeGb:F1} GB free / {totalGb:F1} GB total ({freePercent:F1}% free) — {driveStatus}");

            if (driveScore < minScore) minScore = driveScore;
            if (driveStatus > worst) worst = driveStatus;
        }

        sw.Stop();

        var summary = worst switch
        {
            CheckStatus.Critical => "One or more drives are critically low on space",
            CheckStatus.Warning => "One or more drives are running low on space",
            _ => "All fixed drives have healthy free space"
        };

        return Task.FromResult(new HealthCheckResult
        {
            CategoryName = Name,
            Status = worst,
            Summary = summary,
            Details = details,
            Recommendations = recommendations,
            Score = minScore,
            Weight = 1,
            Duration = sw.Elapsed
        });
    }
}
