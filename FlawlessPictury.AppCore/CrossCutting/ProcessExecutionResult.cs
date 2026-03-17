using System;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Result of running an external process.
    /// </summary>
    public sealed class ProcessExecutionResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public byte[] StandardOutputBytes { get; set; }

        public bool TimedOut { get; set; }
        public bool Canceled { get; set; }

        public DateTime StartedUtc { get; set; }
        public DateTime FinishedUtc { get; set; }

        public TimeSpan Duration => FinishedUtc - StartedUtc;
    }
}
