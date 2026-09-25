using DevSentinel.Models;

namespace DevSentinel.Services;

/// <summary>
/// Turns the individual check results into a single weighted 0-100
/// "Developer Environment Health Score" plus a letter grade.
/// </summary>
public static class HealthScoreCalculator
{
    public static void Apply(EnvironmentReport report)
    {
        if (report.Checks.Count == 0)
        {
            report.OverallScore = 0;
            report.Grade = "N/A";
            return;
        }

        var totalWeight = report.Checks.Sum(c => c.Weight);
        var weightedSum = report.Checks.Sum(c => (double)c.Score * c.Weight);

        var score = totalWeight > 0 ? (int)Math.Round(weightedSum / totalWeight) : 0;
        report.OverallScore = Math.Clamp(score, 0, 100);
        report.Grade = ToGrade(report.OverallScore);
    }

    public static string ToGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
}
