using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Executes shell commands for command-type hooks.
/// </summary>
internal static class CommandHookExecutor
{
    /// <summary>
    /// Executes a shell command with a timeout.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task ExecuteAsync(string command, int timeoutMs = 30000)
    {
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        var startInfo = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        using var cts = new CancellationTokenSource(timeoutMs);

        process.Start();

#if NET8_0_OR_GREATER
        await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
#else
        await Task.Run(() => process.WaitForExit(timeoutMs), cts.Token).ConfigureAwait(false);
#endif
    }
}
