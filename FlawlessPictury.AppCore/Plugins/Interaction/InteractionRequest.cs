using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins.Interaction
{
    /// <summary>
    /// Represents an interaction request emitted by a step.
    /// </summary>
    public sealed class InteractionRequest
    {
        /// <summary>
        /// Initializes a new request.
        /// </summary>
        public InteractionRequest()
        {
            PreviewLocators = new List<string>();
            CandidateLabels = new List<string>();
            Parameters = new ParameterSchema();
            CurrentValues = new ParameterValues();
            HeadlessBehavior = HeadlessBehavior.Fail;
        }

        /// <summary>Purpose of the interaction.</summary>
        public InteractionPurpose Purpose { get; set; }

        /// <summary>Short title for the UI dialog.</summary>
        public string Title { get; set; }

        /// <summary>Detailed message/instructions for the user.</summary>
        public string Message { get; set; }

        /// <summary>
        /// List of preview locators (typically file paths) the UI can show (images, waveforms, text).
        /// </summary>
        public List<string> PreviewLocators { get; }

        /// <summary>
        /// Optional candidate labels (used with <see cref="InteractionPurpose.PickOne"/>).
        /// </summary>
        public List<string> CandidateLabels { get; }

        /// <summary>
        /// Parameter schema that can be edited by the user (used with <see cref="InteractionPurpose.AdjustParameters"/>).
        /// </summary>
        public ParameterSchema Parameters { get; set; }

        /// <summary>Current parameter values shown to the user for editing.</summary>
        public ParameterValues CurrentValues { get; set; }

        /// <summary>
        /// Defines what happens when UI is not available.
        /// </summary>
        public HeadlessBehavior HeadlessBehavior { get; set; }

        /// <summary>
        /// Optional default choice index for pick-one interactions.
        /// </summary>
        public int? DefaultChoiceIndex { get; set; }
    }
}
