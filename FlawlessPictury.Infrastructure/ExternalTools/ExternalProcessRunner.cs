using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.CrossCutting;
using AppLogLevel = FlawlessPictury.AppCore.CrossCutting.LogLevel;

namespace FlawlessPictury.Infrastructure.ExternalTools
{
    /// <summary>
    /// Default Windows implementation of <see cref="IProcessRunner"/> using System.Diagnostics.Process.
    /// </summary>
    public sealed class ExternalProcessRunner : IProcessRunner
    {
        private readonly ILogger _logger;

        public ExternalProcessRunner(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Result<ProcessExecutionResult>> RunAsync(ProcessExecutionOptions options, CancellationToken cancellationToken)
        {
            if (options == null) return Result<ProcessExecutionResult>.Fail(Error.Validation("ProcessExecutionOptions is null."));
            if (string.IsNullOrWhiteSpace(options.FileName)) return Result<ProcessExecutionResult>.Fail(Error.Validation("FileName is required."));

            var startedUtc = DateTime.UtcNow;

            var psi = new ProcessStartInfo
            {
                FileName = options.FileName,
                Arguments = options.Arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? string.Empty : options.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = !options.MergeStdErrIntoStdOut
            };

            if (options.Environment != null)
            {
                foreach (KeyValuePair<string, string> kvp in options.Environment)
                {
                    try
                    {
                        if (kvp.Key == null) continue;
                        psi.EnvironmentVariables[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                    catch
                    {
                    }
                }
            }

            using (var p = new Process())
            {
                p.StartInfo = psi;
                p.EnableRaisingEvents = true;

                Task<string> readOutText = null;
                Task<byte[]> readOutBytes = null;
                Task<string> readErr = null;

                try
                {
                    if (!p.Start())
                    {
                        return Result<ProcessExecutionResult>.Fail(Error.NotSupported("Failed to start process."));
                    }

                    if (options.CaptureStandardOutputBytes)
                    {
                        readOutBytes = ReadAllBytesAsync(p.StandardOutput.BaseStream, cancellationToken);
                    }
                    else
                    {
                        readOutText = p.StandardOutput.ReadToEndAsync();
                    }

                    if (psi.RedirectStandardError)
                    {
                        readErr = p.StandardError.ReadToEndAsync();
                    }

                    var waitTask = WaitForExitAsync(p);
                    Task completedTask;

                    if (options.Timeout.HasValue && options.Timeout.Value > TimeSpan.Zero)
                    {
                        var timeoutTask = Task.Delay(options.Timeout.Value, CancellationToken.None);
                        completedTask = await Task.WhenAny(waitTask, timeoutTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
                    }
                    else
                    {
                        completedTask = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
                    }

                    var result = new ProcessExecutionResult
                    {
                        StartedUtc = startedUtc
                    };

                    if (completedTask != waitTask)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            result.Canceled = true;
                            TryKill(p);
                        }
                        else
                        {
                            result.TimedOut = true;
                            TryKill(p);
                        }

                        await waitTask.ConfigureAwait(false);
                    }

                    if (readOutText != null)
                    {
                        try { result.StandardOutput = await readOutText.ConfigureAwait(false); } catch { }
                    }

                    if (readOutBytes != null)
                    {
                        try { result.StandardOutputBytes = await readOutBytes.ConfigureAwait(false); } catch { }
                    }

                    if (readErr != null)
                    {
                        try { result.StandardError = await readErr.ConfigureAwait(false); } catch { }
                    }

                    result.ExitCode = SafeExitCode(p);
                    result.FinishedUtc = DateTime.UtcNow;

                    if (_logger != null)
                    {
                        var cmd = BuildCommandLine(options);

                        if (result.Canceled)
                        {
                            _logger.Log(AppLogLevel.Warn, PrefixWithProcessingSlot("Process canceled: " + cmd));
                        }
                        else if (result.TimedOut)
                        {
                            _logger.Log(AppLogLevel.Warn, PrefixWithProcessingSlot("Process timed out: " + cmd));
                        }
                        else if (!IsAcceptableExitCode(options, result.ExitCode))
                        {
                            var err = TrimForLog(result.StandardError, 4000);
                            var outp = options.CaptureStandardOutputBytes ? null : TrimForLog(result.StandardOutput, 2000);
                            var msg = PrefixWithProcessingSlot("Process non-zero exit code (" + result.ExitCode + "): " + cmd);
                            if (!string.IsNullOrWhiteSpace(err))
                            {
                                msg += Environment.NewLine + "stderr: " + err;
                            }
                            if (!string.IsNullOrWhiteSpace(outp))
                            {
                                msg += Environment.NewLine + "stdout: " + outp;
                            }
                            _logger.Log(AppLogLevel.Warn, msg);
                        }
                        else if (result.ExitCode != 0)
                        {
                            _logger.Log(AppLogLevel.Debug, PrefixWithProcessingSlot("Process acceptable non-zero exit (" + result.ExitCode + "): " + cmd));
                        }
                    }

                    return Result<ProcessExecutionResult>.Ok(result);
                }
                catch (Exception ex)
                {
                    _logger?.Log(AppLogLevel.Error, PrefixWithProcessingSlot("Process execution failed: " + BuildCommandLine(options)), ex);
                    return Result<ProcessExecutionResult>.Fail(Error.NotSupported("Process execution failed.", ex.Message));
                }
            }
        }

        private static string PrefixWithProcessingSlot(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            try
            {
                var prefix = ProcessingLogScope.CurrentPrefix;
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    return message;
                }

                var slotNumber = ProcessingLogScope.CurrentSlotNumber;
                if (!slotNumber.HasValue || slotNumber.Value <= 0)
                {
                    return message;
                }

                if (message.StartsWith("Thread ", StringComparison.OrdinalIgnoreCase))
                {
                    return message;
                }

                return "Thread " + slotNumber.Value.ToString(CultureInfo.InvariantCulture) + ": " + message;
            }
            catch
            {
                return message;
            }
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                var buffer = new byte[81920];
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        private static string TrimForLog(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            text = text.Trim();
            if (maxChars <= 0 || text.Length <= maxChars)
            {
                return text;
            }

            return text.Substring(0, maxChars) + " ...";
        }

        private static bool IsAcceptableExitCode(ProcessExecutionOptions options, int exitCode)
        {
            if (exitCode == 0)
            {
                return true;
            }

            var ok = options == null ? null : options.AcceptableExitCodes;
            return ok != null && ok.Count > 0 && ok.Contains(exitCode);
        }

        private static int SafeExitCode(Process p)
        {
            try { return p.ExitCode; } catch { return -1; }
        }

        private static string BuildCommandLine(ProcessExecutionOptions options)
        {
            return (options.FileName ?? string.Empty) + " " + (options.Arguments ?? string.Empty);
        }

        private static void TryKill(Process p)
        {
            try
            {
                if (p == null || p.HasExited) return;
                p.Kill();
            }
            catch
            {
            }
        }

        private static Task WaitForExitAsync(Process process)
        {
            if (process.HasExited)
            {
                return Task.FromResult(true);
            }

            var tcs = new TaskCompletionSource<bool>();
            EventHandler handler = null;
            handler = (s, e) =>
            {
                process.Exited -= handler;
                tcs.TrySetResult(true);
            };
            process.Exited += handler;

            if (process.HasExited)
            {
                process.Exited -= handler;
                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }
    }
}
