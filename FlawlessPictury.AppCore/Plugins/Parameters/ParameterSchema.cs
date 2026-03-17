using System;
using System.Collections.Generic;

namespace FlawlessPictury.AppCore.Plugins.Parameters
{
    /// <summary>
    /// A schema describing parameters used to configure a capability/step.
    /// </summary>
    public sealed class ParameterSchema
    {
        private readonly List<ParameterDefinition> _parameters;

        /// <summary>
        /// Initializes an empty schema.
        /// </summary>
        public ParameterSchema()
        {
            _parameters = new List<ParameterDefinition>();
        }

        /// <summary>
        /// Gets the parameter definitions.
        /// </summary>
        public IReadOnlyList<ParameterDefinition> Parameters => _parameters;

        /// <summary>
        /// Adds a parameter definition to the schema.
        /// </summary>
        public ParameterSchema Add(ParameterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            // Intent: Keep order stable so UI rendering remains predictable.
            _parameters.Add(definition);
            return this;
        }

        /// <summary>
        /// Tries to find a parameter definition by key.
        /// </summary>
        public bool TryGet(string key, out ParameterDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int i = 0; i < _parameters.Count; i++)
            {
                if (string.Equals(_parameters[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    definition = _parameters[i];
                    return true;
                }
            }

            return false;
        }
    }
}
