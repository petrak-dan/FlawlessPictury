using System;
using System.Collections.Generic;
using System.Globalization;

namespace FlawlessPictury.AppCore.Plugins.Parameters
{
    /// <summary>
    /// Stores parameter values (typically created from UI/preset) for a specific step instance.
    /// </summary>
    public sealed class ParameterValues
    {
        private readonly Dictionary<string, object> _values;

        /// <summary>
        /// Initializes an empty values collection.
        /// </summary>
        public ParameterValues()
        {
            _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets a parameter value. Null removes the value.
        /// </summary>
        public void Set(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // Intent: Null means "unset" to allow fallback to schema defaults.
            if (value == null)
            {
                _values.Remove(key);
                return;
            }

            _values[key] = value;
        }

        /// <summary>
        /// Tries to get a raw parameter value.
        /// </summary>
        public bool TryGet(string key, out object value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets a string value, or returns the provided default if missing.
        /// </summary>
        public string GetString(string key, string defaultValue = null)
        {
            object value;
            if (!TryGet(key, out value) || value == null)
            {
                return defaultValue;
            }

            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets an integer value, or returns the provided default if missing/unparseable.
        /// </summary>
        public int GetInt32(string key, int defaultValue = 0)
        {
            object value;
            if (!TryGet(key, out value) || value == null)
            {
                return defaultValue;
            }

            if (value is int)
            {
                return (int)value;
            }

            var s = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
            int parsed;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : defaultValue;
        }

        /// <summary>
        /// Gets a boolean value, or returns the provided default if missing/unparseable.
        /// </summary>
        public bool GetBoolean(string key, bool defaultValue = false)
        {
            object value;
            if (!TryGet(key, out value) || value == null)
            {
                return defaultValue;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            var s = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
            bool parsed;
            return bool.TryParse(s, out parsed) ? parsed : defaultValue;
        }

        /// <summary>
        /// Returns a copy of the internal map for serialization/debugging.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>(_values, StringComparer.OrdinalIgnoreCase);
        }
    }
}
