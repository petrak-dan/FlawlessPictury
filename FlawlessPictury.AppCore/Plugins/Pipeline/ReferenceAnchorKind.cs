namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Indicates which artifact should be used as the reference baseline for the step.
    /// </summary>
    public enum ReferenceAnchorKind
    {
        /// <summary>No reference passed (Reference will be null).</summary>
        None = 0,

        /// <summary>Use the original pipeline input artifact as the reference baseline.</summary>
        OriginalInput = 1,

        /// <summary>Use the current/previous artifact in the pipeline flow as the reference baseline.</summary>
        PreviousStepOutput = 2
    }
}
