using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Interaction;
using FlawlessPictury.AppCore.Plugins.View;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Represents a step execution output.
    /// </summary>
    public sealed class StepOutput
    {
        /// <summary>
        /// Initializes a new output object.
        /// </summary>
        public StepOutput()
        {
            OutputArtifacts = new List<Artifact>();
            Metrics = new Dictionary<string, object>();
            Properties = new List<PropertyEntry>();
        }

        /// <summary>
        /// Gets output artifacts produced by the step.
        /// </summary>
        public List<Artifact> OutputArtifacts { get; }

        /// <summary>
        /// Gets optional metrics produced by the step (e.g., size, SSIM, duration).
        /// </summary>
        public Dictionary<string, object> Metrics { get; }

        /// <summary>
        /// Optional UI-agnostic encoded image payload for preview panes.
        /// </summary>
        public ImageViewData ImageView { get; set; }

        /// <summary>
        /// Optional user-visible property entries emitted by a provider capability.
        /// </summary>
        public List<PropertyEntry> Properties { get; }

        /// <summary>
        /// If non-null, indicates the step requires user/host interaction to continue.
        /// </summary>
        public InteractionRequest InteractionRequest { get; set; }

        /// <summary>
        /// Opaque token used to resume an interactive step.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Convenience: returns true when this output requests interaction.
        /// </summary>
        public bool RequiresInteraction => InteractionRequest != null;
    }
}
