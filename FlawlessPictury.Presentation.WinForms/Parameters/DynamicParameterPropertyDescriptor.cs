using System;
using System.ComponentModel;
using System.Globalization;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.Presentation.WinForms.Parameters
{
    /// <summary>
    /// PropertyGrid adapter for a single <see cref="ParameterDefinition"/>.
    /// </summary>
    internal sealed class DynamicParameterPropertyDescriptor : PropertyDescriptor
    {
        private readonly ParameterDefinition _definition;
        private readonly ParameterValues _values;
        private readonly bool _isReadOnly;

        /// <summary>
        /// Initializes a new descriptor for a schema parameter.
        /// </summary>
        public DynamicParameterPropertyDescriptor(ParameterDefinition definition, ParameterValues values, Attribute[] attributes, bool isReadOnly)
            : base(definition == null ? string.Empty : definition.Key, attributes)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _isReadOnly = isReadOnly;
        }

        public override Type ComponentType => typeof(DynamicParameterObject);

        public override bool IsReadOnly => _isReadOnly;

        public override Type PropertyType => GetPropertyType(_definition.Type);

        public override TypeConverter Converter
        {
            get
            {
                if (_definition.AllowedValues != null && _definition.AllowedValues.Count > 0)
                {
                    return new AllowedValuesConverter(_definition.AllowedValues);
                }

                return base.Converter;
            }
        }

        public override bool CanResetValue(object component)
        {
            // Reset supported when a default exists.
            return _definition.DefaultValue != null;
        }

        public override object GetValue(object component)
        {
            // Intent: Prefer explicitly set values; fallback to parsed schema defaults; otherwise null/empty.
            object raw;
            if (_values.TryGet(_definition.Key, out raw) && raw != null)
            {
                return CoerceToPropertyType(raw, PropertyType);
            }

            return ParseDefaultValue(_definition, PropertyType);
        }

        public override void SetValue(object component, object value)
        {
            // Intent: Normalize to expected types to reduce surprises downstream.
            if (_isReadOnly)
            {
                return;
            }

            var coerced = CoerceToPropertyType(value, PropertyType);
            _values.Set(_definition.Key, coerced);
            OnValueChanged(component, EventArgs.Empty);
        }

        public override void ResetValue(object component)
        {
            // Intent: Reset back to default (or remove if no default).
            if (_definition.DefaultValue == null)
            {
                _values.Set(_definition.Key, null);
                return;
            }

            _values.Set(_definition.Key, ParseDefaultValue(_definition, PropertyType));
        }

        public override bool ShouldSerializeValue(object component)
        {
            // We serialize via ParameterValues, not via PropertyGrid's serializer.
            return false;
        }

        private static Type GetPropertyType(ParameterType type)
        {
            if (type == ParameterType.Boolean)
            {
                return typeof(bool);
            }

            if (type == ParameterType.Int32)
            {
                return typeof(int);
            }

            if (type == ParameterType.Decimal)
            {
                return typeof(decimal);
            }

            // Path, enum, and string values are edited as strings in the current UI.
            return typeof(string);
        }

        private static object CoerceToPropertyType(object value, Type targetType)
        {
            if (value == null)
            {
                return targetType == typeof(string) ? string.Empty : Activator.CreateInstance(targetType);
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            // Intent: Convert using invariant culture so UI locale doesn't break presets.
            var s = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
            {
                bool b;
                return bool.TryParse(s, out b) ? b : false;
            }

            if (targetType == typeof(int))
            {
                int i;
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) ? i : 0;
            }

            if (targetType == typeof(decimal))
            {
                decimal d;
                return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d) ? d : 0m;
            }

            return s;
        }

        private static object ParseDefaultValue(ParameterDefinition def, Type targetType)
        {
            if (def == null || def.DefaultValue == null)
            {
                return targetType == typeof(string) ? string.Empty : Activator.CreateInstance(targetType);
            }

            return CoerceToPropertyType(def.DefaultValue, targetType);
        }
    }
}
