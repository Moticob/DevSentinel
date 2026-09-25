using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevSentinel.Models;

namespace DevSentinel.Services;

/// <summary>
/// Exports a completed <see cref="EnvironmentReport"/> to disk in one of several formats.
/// </summary>
public static class ReportExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ExportJson(EnvironmentReport report, string path)
    {
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    public static string ExportText(EnvironmentReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=======================================");
        sb.AppendLine("   DevSentinel — Environment Health Report");
        sb.AppendLine("=======================================");
        sb.AppendLine($"Generated (UTC): {report.GeneratedAtUtc:u}");
        sb.AppendLine($"Machine:         {report.MachineName}");
        sb.AppendLine($"User:            {report.UserName}");
        sb.AppendLine($"OS:              {report.OSDescription} ({report.OSArchitecture})");
        sb.AppendLine();
        sb.AppendLine($"OVERALL SCORE: {report.OverallScore}/100  (Grade {report.Grade})");
        sb.AppendLine($"Healthy: {report.HealthyCount}   Warnings: {report.WarningCount}   Critical: {report.CriticalCount}");
        sb.AppendLine();

        foreach (var check in report.Checks)
        {
            sb.AppendLine("---------------------------------------");
            sb.AppendLine($"[{check.Status.ToString().ToUpperInvariant()}] {check.CategoryName} — score {check.Score}");
            sb.AppendLine($"  {check.Summary}");
            foreach (var detail in check.Details)
                sb.AppendLine($"  {detail}");
            if (check.Recommendations.Count > 0)
            {
                sb.AppendLine("  Recommendations:");
                foreach (var rec in check.Recommendations)
                    sb.AppendLine($"    -> {rec}");
            }
        }

        sb.AppendLine("---------------------------------------");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public static string ExportHtml(EnvironmentReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>DevSentinel Report — {Escape(report.MachineName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(CssStyles);
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<h1>🛡️ DevSentinel — Environment Health Report</h1>");
        sb.AppendLine("<div class=\"meta\">");
        sb.AppendLine($"<div><strong>Generated (UTC):</strong> {report.GeneratedAtUtc:u}</div>");
        sb.AppendLine($"<div><strong>Machine:</strong> {Escape(report.MachineName)}</div>");
        sb.AppendLine($"<div><strong>User:</strong> {Escape(report.UserName)}</div>");
        sb.AppendLine($"<div><strong>OS:</strong> {Escape(report.OSDescription)} ({Escape(report.OSArchitecture)})</div>");
        sb.AppendLine("</div>");

        var gradeClass = GradeClass(report.Grade);
        sb.AppendLine("<div class=\"score-card\">");
        sb.AppendLine($"<div class=\"score-number {gradeClass}\">{report.OverallScore}</div>");
        sb.AppendLine("<div class=\"score-label\">Health Score / 100</div>");
        sb.AppendLine($"<div class=\"grade {gradeClass}\">Grade {Escape(report.Grade)}</div>");
        sb.AppendLine("<div class=\"score-breakdown\">");
        sb.AppendLine($"<span class=\"pill healthy\">{report.HealthyCount} Healthy</span>");
        sb.AppendLine($"<span class=\"pill warning\">{report.WarningCount} Warning</span>");
        sb.AppendLine($"<span class=\"pill critical\">{report.CriticalCount} Critical</span>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"checks\">");
        foreach (var check in report.Checks)
        {
            var statusClass = check.Status.ToString().ToLowerInvariant();
            sb.AppendLine($"<div class=\"check-card {statusClass}\">");
            sb.AppendLine("<div class=\"check-header\">");
            sb.AppendLine($"<span class=\"status-badge {statusClass}\">{Escape(check.Status.ToString())}</span>");
            sb.AppendLine($"<span class=\"check-name\">{Escape(check.CategoryName)}</span>");
            sb.AppendLine($"<span class=\"check-score\">{check.Score}/100</span>");
            sb.AppendLine("</div>");
            sb.AppendLine($"<p class=\"summary\">{Escape(check.Summary)}</p>");

            if (check.Details.Count > 0)
            {
                sb.AppendLine("<ul class=\"details\">");
                foreach (var d in check.Details)
                    sb.AppendLine($"<li>{Escape(d)}</li>");
                sb.AppendLine("</ul>");
            }

            if (check.Recommendations.Count > 0)
            {
                sb.AppendLine("<div class=\"recommendations\"><strong>Recommendations:</strong><ul>");
                foreach (var r in check.Recommendations)
                    sb.AppendLine($"<li>{Escape(r)}</li>");
                sb.AppendLine("</ul></div>");
            }

            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine($"<p class=\"footer\">Generated by DevSentinel v{Escape(report.ToolVersion)}</p>");
        sb.AppendLine("</div></body></html>");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string GradeClass(string grade) => grade switch
    {
        "A" => "grade-a",
        "B" => "grade-b",
        "C" => "grade-c",
        "D" => "grade-d",
        _ => "grade-f"
    };

    private static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s);

    private const string CssStyles = @"
        :root {
            --bg: #0f1117; --card: #171a23; --border: #262a36;
            --text: #e6e8ee; --muted: #9098ab;
            --healthy: #3ddc84; --warning: #f5c451; --critical: #ff5c6c; --info: #5fb3ff;
        }
        * { box-sizing: border-box; }
        body { background: var(--bg); color: var(--text); font-family: 'Segoe UI', Consolas, Arial, sans-serif; margin: 0; padding: 32px 16px; }
        .container { max-width: 860px; margin: 0 auto; }
        h1 { font-size: 22px; margin-bottom: 4px; }
        .meta { display: flex; flex-wrap: wrap; gap: 16px; color: var(--muted); font-size: 13px; margin-bottom: 24px; }
        .score-card { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 24px; text-align: center; margin-bottom: 24px; }
        .score-number { font-size: 56px; font-weight: 700; line-height: 1; }
        .score-label { color: var(--muted); font-size: 13px; margin-top: 4px; }
        .grade { display: inline-block; margin-top: 8px; padding: 4px 14px; border-radius: 999px; font-weight: 600; font-size: 14px; }
        .score-breakdown { margin-top: 16px; display: flex; gap: 8px; justify-content: center; flex-wrap: wrap; }
        .pill { padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; }
        .pill.healthy { background: rgba(61,220,132,0.15); color: var(--healthy); }
        .pill.warning { background: rgba(245,196,81,0.15); color: var(--warning); }
        .pill.critical { background: rgba(255,92,108,0.15); color: var(--critical); }
        .grade-a { color: var(--healthy); } .grade-b { color: var(--healthy); }
        .grade-c { color: var(--warning); } .grade-d { color: var(--warning); }
        .grade-f { color: var(--critical); }
        .checks { display: flex; flex-direction: column; gap: 12px; }
        .check-card { background: var(--card); border: 1px solid var(--border); border-left: 4px solid var(--muted); border-radius: 10px; padding: 16px 18px; }
        .check-card.healthy { border-left-color: var(--healthy); }
        .check-card.warning { border-left-color: var(--warning); }
        .check-card.critical { border-left-color: var(--critical); }
        .check-card.info { border-left-color: var(--info); }
        .check-header { display: flex; align-items: center; gap: 10px; margin-bottom: 6px; }
        .check-name { font-weight: 600; flex: 1; }
        .check-score { color: var(--muted); font-size: 13px; }
        .status-badge { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 6px; text-transform: uppercase; }
        .status-badge.healthy { background: rgba(61,220,132,0.15); color: var(--healthy); }
        .status-badge.warning { background: rgba(245,196,81,0.15); color: var(--warning); }
        .status-badge.critical { background: rgba(255,92,108,0.15); color: var(--critical); }
        .status-badge.info { background: rgba(95,179,255,0.15); color: var(--info); }
        .summary { margin: 6px 0; color: var(--text); }
        .details { margin: 8px 0 0 0; padding-left: 20px; color: var(--muted); font-size: 13px; line-height: 1.6; }
        .recommendations { margin-top: 10px; font-size: 13px; background: rgba(255,255,255,0.03); border-radius: 8px; padding: 10px 14px; }
        .recommendations ul { margin: 6px 0 0 0; padding-left: 20px; }
        .footer { text-align: center; color: var(--muted); font-size: 12px; margin-top: 24px; }
    ";
}
