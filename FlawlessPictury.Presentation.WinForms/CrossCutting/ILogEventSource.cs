using System;

namespace FlawlessPictury.Presentation.WinForms.CrossCutting
{
    /// <summary>
    /// Event source for log events. Used by presenters to subscribe without depending on a specific logger implementation.
    /// </summary>
    public interface ILogEventSource
    {
        /// <summary>
        /// Raised whenever a log event is emitted.
        /// </summary>
        event EventHandler<LogEventArgs> LogEmitted;
    }
}
