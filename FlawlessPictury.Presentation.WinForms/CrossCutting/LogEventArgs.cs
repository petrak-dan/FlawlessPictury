using System;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Presentation.WinForms.CrossCutting
{
    /// <summary>
    /// Represents a log entry emitted to UI subscribers.
    /// </summary>
    public sealed class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="LogEventArgs"/> instance.
        /// </summary>
        /// <param name="timestamp">The local timestamp of the log entry.</param>
        /// <param name="level">The log severity.</param>
        /// <param name="threadId">The originating thread identifier, if available.</param>
        /// <param name="message">The log message without formatting.</param>
        /// <param name="exception">The associated exception, if any.</param>
        /// <param name="formattedLine">The formatted log line shown by the UI.</param>
        public LogEventArgs(DateTime timestamp, LogLevel level, int threadId, string message, Exception exception, string formattedLine)
        {
            Timestamp = timestamp;
            Level = level;
            ThreadId = threadId;
            Message = message;
            Exception = exception;
            FormattedLine = formattedLine;
        }

        /// <summary>
        /// Gets the local timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the log severity.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the originating thread identifier, if captured.
        /// </summary>
        public int ThreadId { get; }

        /// <summary>
        /// Gets the raw log message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the associated exception, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the formatted line intended for display.
        /// </summary>
        public string FormattedLine { get; }
    }
}
