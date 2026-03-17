namespace FlawlessPictury.AppCore.Plugins.View
{
    /// <summary>
    /// Single user-visible property entry emitted by a provider capability.
    /// </summary>
    public sealed class PropertyEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Group { get; set; }
        public string Source { get; set; }
    }
}
