using System;

namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// Provides basic matching for MIME/media types.
    /// </summary>
    public static class MediaTypeMatcher
    {
        /// <summary>
        /// Returns true if the <paramref name="actualMediaType"/> matches the <paramref name="pattern"/>.
        /// </summary>
        /// <param name="actualMediaType">Concrete media type (e.g., "image/jpeg").</param>
        /// <param name="pattern">Pattern (e.g., "image/*", "*/*", or exact type).</param>
        public static bool IsMatch(string actualMediaType, string pattern)
        {
            if (string.IsNullOrWhiteSpace(actualMediaType) || string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            actualMediaType = actualMediaType.Trim();
            pattern = pattern.Trim();

            if (string.Equals(pattern, "*/*", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(actualMediaType, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Handle "type/*"
            int slash = pattern.IndexOf('/');
            if (slash > 0 && pattern.EndsWith("/*", StringComparison.Ordinal))
            {
                var typePart = pattern.Substring(0, slash);
                var actualSlash = actualMediaType.IndexOf('/');
                if (actualSlash > 0)
                {
                    var actualTypePart = actualMediaType.Substring(0, actualSlash);
                    return string.Equals(typePart, actualTypePart, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
