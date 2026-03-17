namespace FlawlessPictury.AppCore.Plugins.Interaction
{
    /// <summary>
    /// Describes the purpose of a user interaction request.
    /// </summary>
    public enum InteractionPurpose
    {
        /// <summary>Simple yes/no approval.</summary>
        Approve = 0,

        /// <summary>User selects one option from a list (e.g., candidate outputs).</summary>
        PickOne = 1,

        /// <summary>User adjusts parameters/settings.</summary>
        AdjustParameters = 2,

        /// <summary>User selects a region (e.g., crop rectangle).</summary>
        SelectRegion = 3,

        /// <summary>User resolves a conflict (e.g., rename collisions).</summary>
        ResolveConflict = 4
    }
}
