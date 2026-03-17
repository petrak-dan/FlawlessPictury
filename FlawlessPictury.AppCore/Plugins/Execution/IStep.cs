using System;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Represents a runnable pipeline step instance.
    /// </summary>
    public interface IStep
    {
        /// <summary>
        /// Executes the step.
        /// </summary>
        /// <param name="input">Step input artifacts and configured parameters.</param>
        /// <param name="context">Execution context (directories, logging, policy).</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A result with <see cref="StepOutput"/> on success, or an error on failure.</returns>
        Task<Result<StepOutput>> ExecuteAsync(
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Optional interface for steps that can pause for user interaction and later resume.
    /// </summary>
    public interface IInteractiveStep : IStep
    {
        /// <summary>
        /// Resumes a previously paused step after the host provides an <see cref="Plugins.Interaction.InteractionResponse"/>.
        /// </summary>
        /// <param name="resumeInput">Continuation token and interaction response.</param>
        /// <param name="context">Execution context.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<StepOutput>> ResumeAsync(
            StepResumeInput resumeInput,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken);
    }
}
