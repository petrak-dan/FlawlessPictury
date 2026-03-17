using System.Collections.Generic;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Represents a pipeline definition (preset) consisting of an ordered chain of steps.
    /// </summary>
    public sealed class PipelineDefinition
    {
        /// <summary>
        /// Initializes an empty pipeline definition.
        /// </summary>
        public PipelineDefinition()
        {
            Steps = new List<PipelineStepDefinition>();
        }

        /// <summary>Optional name shown in UI.</summary>
        public string Name { get; set; }

        /// <summary>Ordered list of steps.</summary>
        public List<PipelineStepDefinition> Steps { get; }
    }
}
