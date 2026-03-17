using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Infrastructure.CrossCutting
{
    /// <summary>
    /// Forwards log entries to multiple <see cref="ILogger"/> sinks.
    /// </summary>
    public sealed class CompositeLogger : ILogger
    {
        private readonly IReadOnlyList<ILogger> _sinks;

        /// <summary>
        /// Creates a composite logger.
        /// </summary>
        /// <param name="sinks">Sinks to receive logs. Null entries are ignored.</param>
        public CompositeLogger(params ILogger[] sinks)
        {
            var list = new List<ILogger>();

            if (sinks != null)
            {
                foreach (var s in sinks)
                {
                    if (s != null)
                    {
                        list.Add(s);
                    }
                }
            }

            _sinks = list;
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            for (int i = 0; i < _sinks.Count; i++)
            {
                try
                {
                    _sinks[i].Log(level, message, exception);
                }
                catch
                {
                    // Never allow logging failures to crash the app.
                }
            }
        }
    }
}
