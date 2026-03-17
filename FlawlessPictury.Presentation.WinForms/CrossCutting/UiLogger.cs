using System;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Presentation.WinForms.CrossCutting
{
    /// <summary>
    /// UI logger adapter used by WinForms presentation layer.
    ///
    /// - Plugin Explorer UI was removed; this logger no longer depends on any PluginExplorer view types.
    /// - The UI log stream is provided via <see cref="UiLogHub"/> (buffered events) and consumed by LogPresenter/LogForm.
    /// </summary>
    public sealed class UiLogger : ILogger
    {
        private readonly UiLogHub _logHub;

        public UiLogger(UiLogHub logHub)
        {
            _logHub = logHub ?? throw new ArgumentNullException(nameof(logHub));
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            // UiLogHub is itself an ILogger; forward directly.
            _logHub.Log(level, message, exception);
        }
    }
}
