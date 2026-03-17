namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// Describes the effect of a step on content.
    /// </summary>
    public enum OperationEffect
    {
        /// <summary>The step does not modify content; it only observes/analyzes.</summary>
        NoChange = 0,

        /// <summary>The step produces a new output artifact (safe default).</summary>
        ProducesNewArtifact = 1,

        /// <summary>The step modifies an existing artifact in-place (requires explicit user consent/policy).</summary>
        MutatesExisting = 2
    }
}
