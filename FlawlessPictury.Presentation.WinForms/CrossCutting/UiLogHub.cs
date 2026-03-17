using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Presentation.WinForms.CrossCutting
{
    /// <summary>
    /// Shared UI log hub (single logger instance + event source).
    /// </summary>
    public sealed class UiLogHub : ILogger, ILogEventSource
    {
        private readonly IUiDispatcher _dispatcher;
        private readonly int _maxBufferedLines;
        private readonly LogLevel _minimumUiLevel;
        private readonly Queue<LogEventArgs> _buffer;
        private readonly object _sync = new object();

        /// <summary>
        /// Initializes the log hub.
        /// </summary>
        /// <param name="dispatcher">UI dispatcher used to marshal LogEmitted events onto the UI thread.</param>
        /// <param name="maxBufferedLines">How many recent log lines to keep for late subscribers.</param>
        public UiLogHub(IUiDispatcher dispatcher, int maxBufferedLines = 2000, LogLevel minimumUiLevel = LogLevel.Info)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _maxBufferedLines = Math.Max(100, maxBufferedLines);
            _minimumUiLevel = minimumUiLevel;
            _buffer = new Queue<LogEventArgs>(_maxBufferedLines);
        }

        /// <inheritdoc />
        public event EventHandler<LogEventArgs> LogEmitted;

        /// <summary>
        /// Minimum log level that will be buffered/emitted to UI subscribers.
        /// </summary>
        public LogLevel MinimumUiLevel => _minimumUiLevel;

        /// <summary>
        /// Returns a snapshot of buffered log events.
        /// </summary>
        public IReadOnlyList<LogEventArgs> GetBufferedEvents()
        {
            lock (_sync)
            {
                return new List<LogEventArgs>(_buffer);
            }
        }

        /// <summary>
        /// Clears buffered log history (does not affect subscribers, only the stored snapshot).
        /// </summary>
        public void ClearBuffer()
        {
            lock (_sync)
            {
                _buffer.Clear();
            }
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            // UI should stay readable; verbose output (Trace/Debug) is typically routed to file logs only.
            if (level < _minimumUiLevel)
            {
                return;
            }

            var ts = DateTime.Now;
            var effectiveMessage = message ?? string.Empty;
            var prefix = ProcessingLogScope.CurrentPrefix;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                effectiveMessage = prefix + effectiveMessage;
            }

            var line = exception == null
                ? $"[{ts:HH:mm:ss}] {level}: {effectiveMessage}"
                : $"[{ts:HH:mm:ss}] {level}: {effectiveMessage} | {exception.GetType().Name}: {exception.Message}";

            var evt = new LogEventArgs(ts, level, 0, effectiveMessage, exception, line);

            lock (_sync)
            {
                _buffer.Enqueue(evt);
                while (_buffer.Count > _maxBufferedLines)
                {
                    _buffer.Dequeue();
                }
            }

            // Always raise on UI thread to keep subscribers simple and safe.
            _dispatcher.BeginInvoke(() =>
            {
                var handler = LogEmitted;
                if (handler != null)
                {
                    handler(this, evt);
                }
            });
        }
    }
}
