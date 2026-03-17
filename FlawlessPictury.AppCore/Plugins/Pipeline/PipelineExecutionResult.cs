using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins.Artifacts;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Represents the results of a pipeline execution.
    /// </summary>
    public sealed class PipelineExecutionResult
    {
        /// <summary>
        /// Initializes a new result.
        /// </summary>
        public PipelineExecutionResult()
        {
            ProducedArtifacts = new List<Artifact>();
            PipelineMetrics = new Dictionary<string, object>();
            StepMetrics = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the final artifact after the pipeline finishes.
        /// </summary>
        public Artifact FinalArtifact { get; set; }

        /// <summary>
        /// Gets artifacts produced by steps during execution.
        /// </summary>
        public List<Artifact> ProducedArtifacts { get; }

        /// <summary>
        /// Optional pipeline-level metrics (duration, totals, etc.).
        /// </summary>
        public Dictionary<string, object> PipelineMetrics { get; }

        /// <summary>Flattened metrics emitted by individual steps.</summary>
        public Dictionary<string, object> StepMetrics { get; }
    }
}
