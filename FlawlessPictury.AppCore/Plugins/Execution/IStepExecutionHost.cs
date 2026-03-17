using System;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Pipeline;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Host-side execution API used by orchestrator steps to run child work through the same engine.
    /// </summary>
    public interface IStepExecutionHost
    {
        /// <summary>
        /// Executes a single declarative step definition using the supplied input/context.
        /// </summary>
        Task<Result<StepOutput>> ExecuteStepAsync(
            PipelineStepDefinition stepDefinition,
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Executes a declarative child pipeline using the supplied input/context.
        /// </summary>
        Task<Result<PipelineExecutionResult>> ExecutePipelineAsync(
            PipelineDefinition pipeline,
            StepInput input,
            StepContext context,
            IProgress<PipelineProgress> progress,
            CancellationToken cancellationToken);
    }
}
