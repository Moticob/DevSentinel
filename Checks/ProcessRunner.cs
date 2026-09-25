using System.Diagnostics;

namespace DevSentinel.Checks;

/// <summary>
/// Result of attempting to run an external command.
/// </summary>
public sealed record ProcessRunResult(bool Found, bool Success, string StdOut, string StdErr, int ExitCode);

/// <summary>
/// Runs external processes defensively: a missing executable, a timeout, or a non-zero
/// exit code are all reported back instead of throwing, so checks stay resilient on
/// machines where a given tool simply isn't installed.
/// </summary>
public static class ProcessRunner
{
    public static async Task<ProcessRunResult> RunAsync(string fileName, string arguments, int timeoutMs = 5000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return new ProcessRunResult(true, false, string.Empty, "Timed out", -1);
            }

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            return new ProcessRunResult(true, process.ExitCode == 0, stdOut.Trim(), stdErr.Trim(), process.ExitCode);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Executable not found on PATH.
            return new ProcessRunResult(false, false, string.Empty, "Not found", -1);
        }
        catch (Exception ex)
        {
            return new ProcessRunResult(false, false, string.Empty, ex.Message, -1);
        }
    }

    private static void TryKill(Process process)
    {
        try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
    }
}
