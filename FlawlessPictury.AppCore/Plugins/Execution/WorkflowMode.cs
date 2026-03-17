namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Controls whether pipelines are allowed to modify original inputs in-place.
    /// </summary>
    public enum WorkflowMode
    {
        /// <summary>
        /// Safe mode: steps should not modify original inputs; outputs are staged and written to an output folder.
        /// </summary>
        SafeOutput = 0,

        /// <summary>
        /// In-place mode: mutating steps are allowed (renames, attribute updates, audio gain changes, etc.).
        /// </summary>
        InPlace = 1
    }
}
