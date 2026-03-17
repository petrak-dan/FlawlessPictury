using System;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Represents a progress update emitted by a step.
    /// </summary>
    public sealed class StepProgress
    {
        /// <summary>
        /// Initializes a new progress update.
        /// </summary>
        /// <param name="percent">Percent from 0..100 (null if unknown).</param>
        /// <param name="message">Optional human-readable progress message.</param>
        public StepProgress(int? percent, string message = null)
        {
            if (percent.HasValue && (percent.Value < 0 || percent.Value > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100.");
            }

            Percent = percent;
            Message = message;
        }

        /// <summary>Percent from 0..100, or null if unknown.</summary>
        public int? Percent { get; }

        /// <summary>Optional message describing current work.</summary>
        public string Message { get; }
    }
}
