using System;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Validates pipeline definitions.
    /// </summary>
    public sealed class PipelineValidator
    {
        private readonly IPluginCatalog _catalog;

        /// <summary>
        /// Initializes the validator.
        /// </summary>
        public PipelineValidator(IPluginCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Validates a pipeline definition for a given input artifact and workflow mode.
        /// </summary>
        /// <param name="pipeline">Pipeline definition.</param>
        /// <param name="input">Input artifact.</param>
        /// <param name="workflowMode">Workflow mode policy.</param>
        public PipelineValidationResult Validate(PipelineDefinition pipeline, Artifact input, WorkflowMode workflowMode)
        {
            var result = new PipelineValidationResult();

            if (pipeline == null)
            {
                result.Errors.Add("Pipeline is null.");
                return result;
            }

            if (input == null)
            {
                result.Errors.Add("Input artifact is null.");
                return result;
            }

            if (pipeline.Steps.Count == 0)
            {
                result.Warnings.Add("Pipeline has no steps; input will pass through unchanged.");
                return result;
            }

            var currentMediaType = input.MediaType;
            var currentArtifact = input;

            for (int i = 0; i < pipeline.Steps.Count; i++)
            {
                var stepDef = pipeline.Steps[i];
                if (stepDef == null)
                {
                    result.Errors.Add($"Step {i}: step definition is null.");
                    continue;
                }

                var plugin = _catalog.FindPlugin(stepDef.PluginId);
                if (plugin == null)
                {
                    result.Errors.Add($"Step {i}: plugin '{stepDef.PluginId}' not found.");
                    continue;
                }

                var cap = _catalog.FindCapability(stepDef.PluginId, stepDef.CapabilityId);
                if (cap == null)
                {
                    result.Errors.Add($"Step {i}: capability '{stepDef.CapabilityId}' not found in plugin '{stepDef.PluginId}'.");
                    continue;
                }

                // Policy: mutating steps require explicit InPlace mode.
                if (cap.Effect == OperationEffect.MutatesExisting && workflowMode != WorkflowMode.InPlace)
                {
                    result.Errors.Add($"Step {i}: capability '{cap.DisplayName}' mutates files and is not allowed in SafeOutput mode.");
                }

                // Format validation: if the capability declares input media types, enforce matching.
                if (cap.InputMediaTypes.Count > 0)
                {
                    bool matches = false;
                    for (int m = 0; m < cap.InputMediaTypes.Count; m++)
                    {
                        if (MediaTypeMatcher.IsMatch(currentMediaType, cap.InputMediaTypes[m]))
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (!matches)
                    {
                        result.Errors.Add(
                            $"Step {i}: input type '{currentMediaType}' is not accepted by capability '{cap.DisplayName}'.");
                    }
                }

                // Representation validation requires the representation to be available on the artifact.
                if (!currentArtifact.HasRepresentation(cap.RequiredRepresentation))
                {
                    result.Errors.Add(
                        $"Step {i}: required representation '{cap.RequiredRepresentation}' is not available on current artifact. " +
                        "The runtime does not auto-materialize representations.");
                }

                // Update expected media type for subsequent steps if the capability provides a single explicit output type.
                if (cap.OutputMediaTypes.Count == 1)
                {
                    currentMediaType = cap.OutputMediaTypes[0];
                }
                else if (cap.OutputMediaTypes.Count > 1)
                {
                    // Ambiguous: downstream validation becomes less strict.
                    currentMediaType = "*/*";
                }

                // Representation produced is not enforced here (since the step may be analyzer-only or no-op).
                // The executor will mark produced representations when it gets actual outputs.
            }

            return result;
        }
    }
}
