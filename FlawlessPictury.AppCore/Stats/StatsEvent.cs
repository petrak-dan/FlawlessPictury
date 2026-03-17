using System;
using System.Collections.Generic;

namespace FlawlessPictury.AppCore.Stats
{
    /// <summary>
    /// Structured stats event used by reporting and analytics sinks.
    /// </summary>
    public sealed class StatsEvent
    {
        /// <summary>
        /// Initializes a new <see cref="StatsEvent"/> instance.
        /// </summary>
        /// <param name="kind">The event kind represented by this instance.</param>
        public StatsEvent(StatsEventKind kind)
        {
            Kind = kind;
            TimestampUtc = DateTime.UtcNow;
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the event timestamp in Coordinated Universal Time.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the high-level event kind.
        /// </summary>
        public StatsEventKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the run identifier associated with the event.
        /// </summary>
        public string RunId { get; set; }

        /// <summary>
        /// Gets or sets the preset identifier associated with the event.
        /// </summary>
        public string PresetId { get; set; }

        /// <summary>
        /// Gets or sets the file identifier associated with the event.
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// Gets or sets the primary file path associated with the event.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the step identifier associated with the event.
        /// </summary>
        public string StepId { get; set; }

        /// <summary>
        /// Gets or sets the step display name associated with the event.
        /// </summary>
        public string StepDisplayName { get; set; }

        /// <summary>
        /// Gets or sets an optional human-readable message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the structured event data payload.
        /// </summary>
        public Dictionary<string, string> Data { get; }

        /// <summary>
        /// Adds or replaces a value in the event data payload.
        /// </summary>
        /// <param name="key">The data key.</param>
        /// <param name="value">The data value.</param>
        /// <returns>The current event instance.</returns>
        public StatsEvent Add(string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Data[key] = value;
            }

            return this;
        }
    }
}
