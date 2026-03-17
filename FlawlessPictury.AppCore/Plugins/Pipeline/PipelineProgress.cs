using FlawlessPictury.AppCore.Plugins.Execution;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Represents a pipeline-level progress update.
    /// </summary>
    public sealed class PipelineProgress
    {
        /// <summary>
        /// Initializes a new progress update.
        /// </summary>
        public PipelineProgress(int stepIndex, int stepCount, StepProgress stepProgress)
        {
            StepIndex = stepIndex;
            StepCount = stepCount;
            StepProgress = stepProgress;
        }

        /// <summary>0-based index of the currently running step.</summary>
        public int StepIndex { get; }

        /// <summary>Total number of steps in the pipeline.</summary>
        public int StepCount { get; }

        /// <summary>Progress reported by the current step.</summary>
        public StepProgress StepProgress { get; }
    }
}
