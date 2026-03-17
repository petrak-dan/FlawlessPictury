using System;
using System.Collections.Generic;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Options for executing an external process.
    /// </summary>
    public sealed class ProcessExecutionOptions
    {
        /// <summary>Absolute or relative path to executable.</summary>
        public string FileName { get; set; }

        /// <summary>Command line arguments. Caller is responsible for correct quoting.</summary>
        public string Arguments { get; set; }

        /// <summary>Optional working directory.</summary>
        public string WorkingDirectory { get; set; }

        /// <summary>Optional timeout. Null means no timeout (still cancelable).</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>Optional environment variables to set/override for the process.</summary>
        public IDictionary<string, string> Environment { get; set; }

        /// <summary>
        /// If true, stderr is merged into stdout. If false, capture separately.
        /// </summary>
public bool MergeStdErrIntoStdOut { get; set; }

/// <summary>
/// Optional list of exit codes to treat as "acceptable" (non-error).
/// If null/empty, only exit code 0 is considered acceptable.
///
/// Why:
/// - Some tools (e.g. ImageMagick compare) use exit code 1 to mean "images differ",
///   which is not a failure for metric-gathering steps.
/// </summary>
public ISet<int> AcceptableExitCodes { get; set; }

        /// <summary>
        /// When true, captures stdout as raw bytes instead of decoding it as text.
        /// </summary>
        public bool CaptureStandardOutputBytes { get; set; }

        /// <summary>
        /// Creates a shallow copy suitable for per-run tweaks.
        /// </summary>
        public ProcessExecutionOptions Clone()
        {
            return new ProcessExecutionOptions
            {
                FileName = FileName,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory,
                Timeout = Timeout,
                MergeStdErrIntoStdOut = MergeStdErrIntoStdOut,
                AcceptableExitCodes = AcceptableExitCodes == null ? null : new HashSet<int>(AcceptableExitCodes),
                CaptureStandardOutputBytes = CaptureStandardOutputBytes,
                Environment = Environment == null ? null : new Dictionary<string, string>(Environment, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
