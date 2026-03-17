using FlawlessPictury.AppCore.Plugins.Interaction;

namespace FlawlessPictury.AppCore.Plugins.Execution
{
    /// <summary>
    /// Represents the input required to resume a previously paused interactive step.
    /// </summary>
    public sealed class StepResumeInput
    {
        /// <summary>
        /// Initializes a new resume input.
        /// </summary>
        public StepResumeInput(string continuationToken, InteractionResponse response)
        {
            ContinuationToken = continuationToken;
            Response = response;
        }

        /// <summary>
        /// Opaque token previously produced by the step to allow resuming.
        /// </summary>
        public string ContinuationToken { get; }

        /// <summary>
        /// The user/host response to the interaction request.
        /// </summary>
        public InteractionResponse Response { get; }
    }
}
