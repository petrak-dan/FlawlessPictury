using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Parameters;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Represents step inputs, including current artifact and optional reference artifacts.
    /// </summary>
    public sealed class StepInput
    {
        /// <summary>
        /// Initializes a new <see cref="StepInput"/>.
        /// </summary>
        public StepInput(Artifact primary, Artifact original, Artifact reference, ParameterValues parameters)
        {
            Primary = primary;
            Original = original;
            Reference = reference;
            Parameters = parameters ?? new ParameterValues();
        }

        /// <summary>Artifact representing the current pipeline flow.</summary>
        public Artifact Primary { get; }

        /// <summary>Artifact representing the original input (optional).</summary>
        public Artifact Original { get; }

        /// <summary>Artifact representing a selected comparison baseline (optional).</summary>
        public Artifact Reference { get; }

        /// <summary>Configured parameters for this step instance.</summary>
        public ParameterValues Parameters { get; }
    }
}
