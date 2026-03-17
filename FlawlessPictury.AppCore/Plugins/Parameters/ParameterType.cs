namespace FlawlessPictury.AppCore.Plugins.Parameters
{
    /// <summary>
    /// Supported parameter types for schema-driven plugin configuration.
    /// </summary>
    public enum ParameterType
    {
        /// <summary>A free-form string.</summary>
        String = 0,

        /// <summary>An integer number.</summary>
        Int32 = 1,

        /// <summary>A boolean (true/false).</summary>
        Boolean = 2,

        /// <summary>A decimal number.</summary>
        Decimal = 3,

        /// <summary>A file system path.</summary>
        Path = 4,

        /// <summary>A selection among a predefined set of values.</summary>
        Enum = 5
    }
}
