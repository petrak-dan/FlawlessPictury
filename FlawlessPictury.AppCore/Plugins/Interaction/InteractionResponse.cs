using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins.Interaction
{
    /// <summary>
    /// Represents a response to an <see cref="InteractionRequest"/>.
    /// </summary>
    public sealed class InteractionResponse
    {
        /// <summary>
        /// Gets or sets whether the user approved (for approve interactions).
        /// </summary>
        public bool? Approved { get; set; }

        /// <summary>
        /// Gets or sets the selected candidate index (for pick-one interactions).
        /// </summary>
        public int? SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets updated parameter values (for adjust-parameters interactions).
        /// </summary>
        public ParameterValues UpdatedValues { get; set; }

        /// <summary>
        /// Gets or sets optional region selection data (for select-region interactions).
        /// </summary>
        /// <remarks>
        /// The current implementation stores a simple string payload.
        /// by adding new properties instead of changing this one.
        /// </remarks>
        public string RegionData { get; set; }

        /// <summary>
        /// Gets or sets optional conflict resolution data (for resolve-conflict interactions).
        /// </summary>
        public string ResolutionData { get; set; }
    }
}
