namespace FlawlessPictury.AppCore.Plugins.Capabilities
{
    /// <summary>
    /// High-level kind of operation performed by a capability/step.
    /// </summary>
    public enum OperationKind
    {
        /// <summary>Reads input(s) and produces initial artifact(s).</summary>
        Producer = 0,

        /// <summary>Transforms content and typically produces a new artifact.</summary>
        Transformer = 1,

        /// <summary>Modifies an existing artifact in-place (requires explicit workflow permission).</summary>
        Mutator = 2,

        /// <summary>Reads artifacts and produces metrics without modifying content.</summary>
        Analyzer = 3,

        /// <summary>Reads/writes metadata or attributes (may be mutating or non-mutating).</summary>
        MetadataOperator = 4,

        /// <summary>Orchestrates other steps (retry/branch/optimize). Does not directly transform content.</summary>
        Orchestrator = 5
    }
}
