using System;

namespace FlawlessPictury.Infrastructure.Plugins
{
    /// <summary>
    /// Represents a plugin load error captured during discovery.
    /// </summary>
    public sealed class PluginLoadError
    {
        /// <summary>
        /// Initializes a new error record.
        /// </summary>
        /// <param name="pluginPath">DLL path involved (if any).</param>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="exception">Optional exception for technical inspection.</param>
        public PluginLoadError(string pluginPath, string message, Exception exception = null)
        {
            PluginPath = pluginPath;
            Message = message;
            Exception = exception;
        }

        /// <summary>Path to the plugin DLL related to the error (may be null).</summary>
        public string PluginPath { get; }

        /// <summary>User-friendly error message.</summary>
        public string Message { get; }

        /// <summary>Optional exception object.</summary>
        public Exception Exception { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(PluginPath)
                ? Message
                : $"{Message} (Path: {PluginPath})";
        }
    }
}
