using DevSentinel.Models;

namespace DevSentinel.Services;

/// <summary>
/// Small helper around Console.ForegroundColor so Program.cs stays readable.
/// Degrades gracefully if the console doesn't support color (e.g. redirected output).
/// </summary>
public static class ConsoleUi
{
    public static void Banner()
    {
        WriteLine(ConsoleColor.Cyan, @"
  ____             ____             _   _            _ 
 |  _ \  _____   __/ ___|  ___ _ __ | |_(_)_ __   ___| |
 | | | |/ _ \ \ / /\___ \ / _ \ '_ \| __| | '_ \ / _ \ |
 | |_| |  __/\ V /  ___) |  __/ | | | |_| | | | |  __/ |
 |____/ \___| \_/  |____/ \___|_| |_|\__|_|_| |_|\___|_|
");
        WriteLine(ConsoleColor.DarkGray, "  Developer Environment Health Monitor — v1.0.0\n");
    }

    public static void Section(string title)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.White, $"» {title}");
    }

    public static void CheckRunning(string name)
    {
        Console.Write($"  Checking {name}...");
    }

    public static void CheckResult(HealthCheckResult result)
    {
        // Overwrite the "Checking X..." line.
        Console.Write("\r");
        var (color, icon) = result.Status switch
        {
            CheckStatus.Healthy => (ConsoleColor.Green, "[ OK ]"),
            CheckStatus.Info => (ConsoleColor.Cyan, "[INFO]"),
            CheckStatus.Warning => (ConsoleColor.Yellow, "[WARN]"),
            CheckStatus.Critical => (ConsoleColor.Red, "[FAIL]"),
            _ => (ConsoleColor.Gray, "[ -- ]")
        };

        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(icon);
        Console.ForegroundColor = prevColor;
        Console.WriteLine($" {result.CategoryName,-22} {result.Summary}");

        foreach (var rec in result.Recommendations)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"         → {rec}");
            Console.ForegroundColor = prevColor;
        }
    }

    public static void ScoreSummary(EnvironmentReport report)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.White, "=========================================");
        var gradeColor = report.Grade switch
        {
            "A" or "B" => ConsoleColor.Green,
            "C" or "D" => ConsoleColor.Yellow,
            _ => ConsoleColor.Red
        };

        var prevColor = Console.ForegroundColor;
        Console.Write("  DEVELOPER ENVIRONMENT HEALTH SCORE: ");
        Console.ForegroundColor = gradeColor;
        Console.Write($"{report.OverallScore}/100 (Grade {report.Grade})");
        Console.ForegroundColor = prevColor;
        Console.WriteLine();

        Console.WriteLine($"  Healthy: {report.HealthyCount}   Warnings: {report.WarningCount}   Critical: {report.CriticalCount}");
        WriteLine(ConsoleColor.White, "=========================================");
    }

    public static void Info(string message) => WriteLine(ConsoleColor.Gray, message);
    public static void Success(string message) => WriteLine(ConsoleColor.Green, message);
    public static void Error(string message) => WriteLine(ConsoleColor.Red, message);

    public static void WaitForExit()
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.DarkGray, "Press any key to close...");
        Console.ReadKey(intercept: true);
    }

    private static void WriteLine(ConsoleColor color, string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }
}
