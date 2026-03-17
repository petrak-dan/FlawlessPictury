using System.Collections.Generic;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Represents validation messages for a pipeline.
    /// </summary>
    public sealed class PipelineValidationResult
    {
        /// <summary>
        /// Initializes a new validation result.
        /// </summary>
        public PipelineValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        /// <summary>Validation errors. If any exist, the pipeline should be considered invalid.</summary>
        public List<string> Errors { get; }

        /// <summary>Validation warnings. The pipeline may still be runnable.</summary>
        public List<string> Warnings { get; }

        /// <summary>True if there are no errors.</summary>
        public bool IsValid => Errors.Count == 0;
    }
}
