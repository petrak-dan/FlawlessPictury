using System;
using System.Collections.Generic;
using System.Globalization;

namespace FlawlessPictury.AppCore.Plugins.Parameters
{
    /// <summary>
    /// Describes a configurable parameter.
    /// </summary>
    public sealed class ParameterDefinition
    {
        /// <summary>
        /// Initializes a new parameter definition.
        /// </summary>
        /// <param name="key">Stable parameter key (unique within a capability).</param>
        /// <param name="displayName">User-visible name.</param>
        /// <param name="type">Parameter type.</param>
        public ParameterDefinition(string key, string displayName, ParameterType type)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));

            Key = key;
            DisplayName = displayName;
            Type = type;

            IsRequired = false;
            IsAdvanced = false;
            IsSearchable = false;

            AllowedValues = new List<string>();
        }

        /// <summary>Stable parameter key.</summary>
        public string Key { get; }

        /// <summary>User-visible name shown in the UI.</summary>
        public string DisplayName { get; }

        /// <summary>Optional UI/help description.</summary>
        public string Description { get; set; }

        /// <summary>Parameter data type.</summary>
        public ParameterType Type { get; }

        /// <summary>Default value stored as string (invariant culture).</summary>
        public string DefaultValue { get; set; }

        /// <summary>Whether the value is required for execution.</summary>
        public bool IsRequired { get; set; }

        /// <summary>Whether the parameter should be hidden behind an "Advanced" toggle.</summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// Whether an orchestrator is allowed to search/override this parameter generically.
        /// </summary>
        public bool IsSearchable { get; set; }

        /// <summary>Optional minimum value (for numeric types) stored as string (invariant culture).</summary>
        public string MinValue { get; set; }

        /// <summary>Optional maximum value (for numeric types) stored as string (invariant culture).</summary>
        public string MaxValue { get; set; }

        /// <summary>Optional regex pattern (for strings) used for validation.</summary>
        public string RegexPattern { get; set; }

        /// <summary>Allowed values for enum-like parameters.</summary>
        public List<string> AllowedValues { get; }

        /// <summary>
        /// Helper: Sets a numeric default value using invariant formatting.
        /// </summary>
        public ParameterDefinition WithDefault(int value)
        {
            DefaultValue = value.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        /// <summary>
        /// Helper: Sets a boolean default value.
        /// </summary>
        public ParameterDefinition WithDefault(bool value)
        {
            DefaultValue = value ? "true" : "false";
            return this;
        }

        /// <summary>
        /// Helper: Sets a string default value.
        /// </summary>
        public ParameterDefinition WithDefault(string value)
        {
            DefaultValue = value;
            return this;
        }
    }
}
