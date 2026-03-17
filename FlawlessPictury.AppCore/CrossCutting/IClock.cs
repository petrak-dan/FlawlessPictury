using System;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Provides current time. Used for timestamps, durations, and consistent testing.
    /// </summary>
    public interface IClock
    {
        /// <summary>Gets the current time in UTC.</summary>
        DateTime UtcNow { get; }
    }
}
