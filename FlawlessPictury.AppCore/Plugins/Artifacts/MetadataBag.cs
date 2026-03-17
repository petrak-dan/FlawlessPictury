using System;
using System.Collections.Generic;
using System.Globalization;

namespace FlawlessPictury.AppCore.Plugins.Artifacts
{
    /// <summary>
    /// Stores metadata fields for an <see cref="Artifact"/> using string keys and string values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type intentionally stays simple and serializer-friendly. If you need richer typing
    /// (arrays, structured objects), you can add support while maintaining backward compatibility.
    /// </para>
    /// <para>
    /// Keys should be stable and ideally namespaced (e.g., "exif.DateTimeOriginal", "fs.LastWriteTimeUtc").
    /// </para>
    /// </remarks>
    public sealed class MetadataBag
    {
        private readonly Dictionary<string, string> _values;

        /// <summary>
        /// Initializes an empty metadata bag.
        /// </summary>
        public MetadataBag()
        {
            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a metadata bag with an initial set of values.
        /// </summary>
        /// <param name="initialValues">Initial key/value pairs to copy. Null means "empty".</param>
        public MetadataBag(IDictionary<string, string> initialValues)
            : this()
        {
            if (initialValues == null)
            {
                return;
            }

            // Intent: Copy values into our internal dictionary with consistent key comparison rules.
            foreach (var kvp in initialValues)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                _values[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this bag contains a specific key.
        /// </summary>
        /// <param name="key">Metadata key (case-insensitive).</param>
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _values.ContainsKey(key);
        }

        /// <summary>
        /// Tries to get a raw string value for a given key.
        /// </summary>
        /// <param name="key">Metadata key (case-insensitive).</param>
        /// <param name="value">When successful, receives the value.</param>
        /// <returns>True if the key exists; otherwise false.</returns>
        public bool TryGetString(string key, out string value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        /// <summary>
        /// Sets or replaces a metadata value. Passing null removes the key.
        /// </summary>
        /// <param name="key">Metadata key (case-insensitive).</param>
        /// <param name="value">Value to set; null removes the key.</param>
        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // Intent: Null means "remove" to avoid ambiguous "present but empty" in downstream code.
            if (value == null)
            {
                _values.Remove(key);
                return;
            }

            _values[key] = value;
        }

        /// <summary>
        /// Removes a metadata key if present.
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _values.Remove(key);
        }

        /// <summary>
        /// Returns a snapshot of the internal metadata dictionary.
        /// </summary>
        /// <remarks>
        /// Prefer this for serialization/export. The returned dictionary is a copy.
        /// </remarks>
        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tries to parse a boolean value.
        /// </summary>
        public bool TryGetBoolean(string key, out bool value)
        {
            value = false;

            string s;
            if (!TryGetString(key, out s))
            {
                return false;
            }

            return bool.TryParse(s, out value);
        }

        /// <summary>
        /// Tries to parse an integer value using invariant culture.
        /// </summary>
        public bool TryGetInt32(string key, out int value)
        {
            value = 0;

            string s;
            if (!TryGetString(key, out s))
            {
                return false;
            }

            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Tries to parse a DateTime (assumes it is stored as an ISO 8601 string).
        /// </summary>
        public bool TryGetDateTime(string key, out DateTime value)
        {
            value = default(DateTime);

            string s;
            if (!TryGetString(key, out s))
            {
                return false;
            }

            return DateTime.TryParse(
                s,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out value);
        }
    }
}
