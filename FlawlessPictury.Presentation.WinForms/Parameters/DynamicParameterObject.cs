using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.Presentation.WinForms.Parameters
{
    /// <summary>
    /// Dynamic property source for PropertyGrid based on <see cref="ParameterSchema"/>.
    /// </summary>
    internal sealed class DynamicParameterObject : ICustomTypeDescriptor
    {
        private readonly ParameterSchema _schema;
        private readonly ParameterValues _values;
        private readonly Func<ParameterDefinition, bool> _filter;
        private readonly HashSet<string> _readOnlyKeys;

        /// <summary>
        /// Initializes the dynamic parameter object.
        /// </summary>
        public DynamicParameterObject(ParameterSchema schema, ParameterValues values)
            : this(schema, values, null, null)
        {
        }

        /// <summary>
        /// Initializes the dynamic parameter object with optional filtering and read-only overrides.
        /// </summary>
        public DynamicParameterObject(ParameterSchema schema, ParameterValues values, Func<ParameterDefinition, bool> filter, IEnumerable<string> readOnlyKeys)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _filter = filter;
            _readOnlyKeys = new HashSet<string>((readOnlyKeys ?? Enumerable.Empty<string>()).Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the backing values object that is mutated by PropertyGrid.
        /// </summary>
        public ParameterValues Values => _values;

        public AttributeCollection GetAttributes() => AttributeCollection.Empty;
        public string GetClassName() => nameof(DynamicParameterObject);
        public string GetComponentName() => nameof(DynamicParameterObject);
        public TypeConverter GetConverter() => null;
        public EventDescriptor GetDefaultEvent() => null;
        public PropertyDescriptor GetDefaultProperty() => null;
        public object GetEditor(Type editorBaseType) => null;
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;
        public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        public PropertyDescriptorCollection GetProperties()
        {
            var props = new List<PropertyDescriptor>();

            // Intent: Create one property per schema parameter, stable order.
            foreach (var def in _schema.Parameters)
            {
                if (def == null)
                {
                    continue;
                }

                if (_filter != null && !_filter(def))
                {
                    continue;
                }

                var isReadOnly = _readOnlyKeys.Contains(def.Key);
                var attrs = BuildAttributes(def, isReadOnly);
                props.Add(new DynamicParameterPropertyDescriptor(def, _values, attrs, isReadOnly));
            }

            return new PropertyDescriptorCollection(props.ToArray(), true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        private static Attribute[] BuildAttributes(ParameterDefinition def, bool isReadOnly)
        {
            var list = new List<Attribute>();

            // Category: separate Advanced from General for readability.
            list.Add(new CategoryAttribute(def.IsAdvanced ? "Advanced" : "General"));

            if (!string.IsNullOrWhiteSpace(def.Description))
            {
                list.Add(new DescriptionAttribute(def.Description));
            }

            // Show required parameters clearly.
            if (def.IsRequired)
            {
                list.Add(new DisplayNameAttribute(def.DisplayName + " *"));
            }
            else if (!string.IsNullOrWhiteSpace(def.DisplayName))
            {
                list.Add(new DisplayNameAttribute(def.DisplayName));
            }

            if (isReadOnly)
            {
                list.Add(ReadOnlyAttribute.Yes);
            }

            return list.ToArray();
        }

        /// <summary>
        /// Validates values against a schema and returns user-facing error messages.
        /// </summary>
        /// <remarks>
        /// Validation is performed when the editor commits changes.
        /// </remarks>
        public static List<string> Validate(ParameterSchema schema, ParameterValues values)
        {
            var errors = new List<string>();
            if (schema == null || values == null)
            {
                errors.Add("Internal error: schema/values missing.");
                return errors;
            }

            foreach (var def in schema.Parameters)
            {
                if (def == null)
                {
                    continue;
                }

                object raw;
                values.TryGet(def.Key, out raw);

                // Required validation (string empty counts as missing).
                if (def.IsRequired)
                {
                    if (raw == null)
                    {
                        errors.Add($"'{def.DisplayName}' is required.");
                        continue;
                    }

                    var s = raw as string;
                    if (s != null && string.IsNullOrWhiteSpace(s))
                    {
                        errors.Add($"'{def.DisplayName}' is required.");
                        continue;
                    }
                }

                // Enum validation
                if (def.Type == ParameterType.Enum && def.AllowedValues != null && def.AllowedValues.Count > 0)
                {
                    var s = raw as string;
                    if (!string.IsNullOrWhiteSpace(s) && !def.AllowedValues.Contains(s))
                    {
                        errors.Add($"'{def.DisplayName}' must be one of: {string.Join(", ", def.AllowedValues)}.");
                    }
                }

                // Regex validation (strings only)
                if (!string.IsNullOrWhiteSpace(def.RegexPattern))
                {
                    var s = raw as string;
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        try
                        {
                            if (!Regex.IsMatch(s, def.RegexPattern))
                            {
                                errors.Add($"'{def.DisplayName}' does not match required format.");
                            }
                        }
                        catch
                        {
                            // Bad regex should not crash the UI; treat as non-fatal.
                        }
                    }
                }
            }

            return errors;
        }
    }
}
