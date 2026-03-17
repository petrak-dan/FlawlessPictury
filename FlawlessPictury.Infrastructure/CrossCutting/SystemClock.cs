using System;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Infrastructure.CrossCutting
{
    /// <summary>
    /// Provides the system clock time in UTC.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
