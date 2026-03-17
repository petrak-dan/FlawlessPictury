using System;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Minimal logging interface used throughout the application.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a log event.
        /// </summary>
        /// <param name="level">Severity.</param>
        /// <param name="message">Human-readable message.</param>
        /// <param name="exception">Optional exception details.</param>
        void Log(LogLevel level, string message, Exception exception = null);
    }
}
