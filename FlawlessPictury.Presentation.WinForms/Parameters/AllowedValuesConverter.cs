using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FlawlessPictury.Presentation.WinForms.Parameters
{
    /// <summary>
    /// A <see cref="TypeConverter"/> that provides a fixed list of standard string values.
    /// </summary>
    internal sealed class AllowedValuesConverter : StringConverter
    {
        private readonly string[] _values;

        /// <summary>
        /// Initializes the converter.
        /// </summary>
        /// <param name="values">Allowed values to present in a drop-down list.</param>
        public AllowedValuesConverter(IEnumerable<string> values)
        {
            _values = values == null ? new string[0] : new List<string>(values).ToArray();
        }

        /// <inheritdoc />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            // Exclusive means the user must choose from the list (no free typing).
            return true;
        }

        /// <inheritdoc />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(_values);
        }
    }
}
