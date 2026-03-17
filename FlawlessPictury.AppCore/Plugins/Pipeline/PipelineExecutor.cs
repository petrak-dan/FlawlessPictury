using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AppLogLevel = FlawlessPictury.AppCore.CrossCutting.LogLevel;
using FlawlessPictury.AppCore.Common;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Interaction;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Stats;

namespace FlawlessPictury.AppCore.Plugins.Pipeline
{
    /// <summary>
    /// Executes pipelines by resolving step definitions to concrete plugin steps.
    /// Also serves as the nested execution host for orchestrator capabilities.
    /// </summary>
    public sealed class PipelineExecutor : IStepExecutionHost
    {
        private readonly IPluginCatalog _catalog;
        private readonly PipelineValidator _validator;

        /// <summary>
        /// Initializes a new executor.
        /// </summary>
        public PipelineExecutor(IPluginCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _validator = new PipelineValidator(_catalog);
        }

        /// <summary>
        /// Executes a pipeline for a single input artifact.
        /// </summary>
        public Task<Result<PipelineExecutionResult>> ExecuteAsync(
            PipelineDefinition pipeline,
            Artifact input,
            StepContext context,
            IProgress<PipelineProgress> progress,
            CancellationToken cancellationToken)
        {
            if (input == null) return Task.FromResult(Result<PipelineExecutionResult>.Fail(Error.Validation("Input artifact is null.")));

            var stepInput = new StepInput(input, input, null, new ParameterValues());
            return ExecutePipelineAsync(pipeline, stepInput, context, progress, cancellationToken);
        }

        /// <summary>
        /// Executes a declarative child pipeline using the supplied input/context.
        /// </summary>
        public async Task<Result<PipelineExecutionResult>> ExecutePipelineAsync(
            PipelineDefinition pipeline,
            StepInput input,
            StepContext context,
            IProgress<PipelineProgress> progress,
            CancellationToken cancellationToken)
        {
            if (pipeline == null) return Result<PipelineExecutionResult>.Fail(Error.Validation("Pipeline is null."));
            if (input == null) return Result<PipelineExecutionResult>.Fail(Error.Validation("StepInput is null."));
            if (input.Primary == null) return Result<PipelineExecutionResult>.Fail(Error.Validation("Primary artifact is null."));
            if (context == null) return Result<PipelineExecutionResult>.Fail(Error.Validation("StepContext is null."));

            EnsureHostBindings(context);

            var validation = _validator.Validate(pipeline, input.Primary, context.WorkflowMode);
            if (!validation.IsValid)
            {
                return Result<PipelineExecutionResult>.Fail(Error.Validation(validation.Errors[0]));
            }

            string presetIdForLog = null;
            if (context.Environment != null)
            {
                context.Environment.TryGetValue("PresetId", out presetIdForLog);
            }

            string runIdForLog = null;
            if (context.Environment != null)
            {
                context.Environment.TryGetValue("RunId", out runIdForLog);
            }

            context.Logger?.Log(
                AppLogLevel.Info,
                "Pipeline start: steps=" + pipeline.Steps.Count +
                " input=" + input.Primary.Locator +
                (string.IsNullOrWhiteSpace(presetIdForLog) ? string.Empty : (" preset=" + presetIdForLog)) +
                (string.IsNullOrWhiteSpace(runIdForLog) ? string.Empty : (" run=" + runIdForLog)));

            var sw = Stopwatch.StartNew();
            var current = input.Primary;
            var original = input.Original ?? input.Primary;
            var explicitReference = input.Reference;
            var result = new PipelineExecutionResult();

            if (pipeline.Steps.Count == 0)
            {
                result.FinalArtifact = input.Primary;
                context.Logger?.Log(AppLogLevel.Info, "Pipeline end: preset=" + presetIdForLog + " durationMs=" + (int)sw.ElapsedMilliseconds + " final=" + result.FinalArtifact?.Locator);
                return Result<PipelineExecutionResult>.Ok(result);
            }

            for (int i = 0; i < pipeline.Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepDef = pipeline.Steps[i];
                var reference = ResolveReference(stepDef, current, original, explicitReference);
                var stepInput = new StepInput(current, original, reference, stepDef.Parameters);

                IProgress<StepProgress> stepProgress = null;
                if (progress != null)
                {
                    var stepIndex = i;
                    stepProgress = new Progress<StepProgress>(p =>
                    {
                        progress.Report(new PipelineProgress(stepIndex, pipeline.Steps.Count, p));
                    });
                }

                var stepResult = await ExecuteStepAsync(stepDef, stepInput, context, stepProgress, cancellationToken).ConfigureAwait(false);
                if (stepResult.IsFailure)
                {
                    return Result<PipelineExecutionResult>.Fail(stepResult.Error);
                }

                var output = stepResult.Value;
                if (output != null && output.Metrics != null && output.Metrics.Count > 0)
                {
                    foreach (var metric in output.Metrics)
                    {
                        if (!result.StepMetrics.ContainsKey(metric.Key))
                        {
                            result.StepMetrics[metric.Key] = metric.Value;
                        }

                        result.StepMetrics["step" + (i + 1).ToString() + "." + metric.Key] = metric.Value;
                    }
                }

                if (output != null && output.OutputArtifacts.Count > 0)
                {
                    current = output.OutputArtifacts[0];
                    result.ProducedArtifacts.AddRange(output.OutputArtifacts);

                    var cap = _catalog.FindCapability(stepDef.PluginId, stepDef.CapabilityId);
                    if (cap != null)
                    {
                        current.MarkRepresentationAvailable(cap.ProducedRepresentation);
                    }
                }
            }

            sw.Stop();
            result.FinalArtifact = current;
            result.PipelineMetrics["duration_ms"] = (int)sw.ElapsedMilliseconds;

            context.Logger?.Log(AppLogLevel.Info, "Pipeline end: preset=" + presetIdForLog + " durationMs=" + (int)sw.ElapsedMilliseconds + " final=" + result.FinalArtifact?.Locator);
            return Result<PipelineExecutionResult>.Ok(result);
        }

        /// <summary>
        /// Executes a single declarative step definition using the supplied input/context.
        /// </summary>
        public async Task<Result<StepOutput>> ExecuteStepAsync(
            PipelineStepDefinition stepDefinition,
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            if (stepDefinition == null) return Result<StepOutput>.Fail(Error.Validation("Step definition is null."));
            if (input == null) return Result<StepOutput>.Fail(Error.Validation("StepInput is null."));
            if (input.Primary == null) return Result<StepOutput>.Fail(Error.Validation("Primary artifact is null."));
            if (context == null) return Result<StepOutput>.Fail(Error.Validation("StepContext is null."));

            EnsureHostBindings(context);

            var plugin = _catalog.FindPlugin(stepDefinition.PluginId);
            if (plugin == null)
            {
                context.Logger?.Log(AppLogLevel.Error, "Pipeline error: Plugin '" + stepDefinition.PluginId + "' not found.");
                return Result<StepOutput>.Fail(Error.DependencyMissing("Plugin '" + stepDefinition.PluginId + "' not found."));
            }

            var cap = _catalog.FindCapability(stepDefinition.PluginId, stepDefinition.CapabilityId);
            if (cap == null)
            {
                context.Logger?.Log(AppLogLevel.Error, "Pipeline error: Capability '" + stepDefinition.CapabilityId + "' not found in plugin '" + stepDefinition.PluginId + "'.");
                return Result<StepOutput>.Fail(Error.DependencyMissing("Capability '" + stepDefinition.CapabilityId + "' not found in plugin '" + stepDefinition.PluginId + "'."));
            }

            var stepSw = Stopwatch.StartNew();
            context.Logger?.Log(AppLogLevel.Debug, "Step start: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId + " - " + cap.DisplayName);
            EmitStepStats(context, new StatsEvent(StatsEventKind.StepStarted)
            {
                StepId = string.IsNullOrWhiteSpace(stepDefinition.StepId) ? (stepDefinition.PluginId + "/" + stepDefinition.CapabilityId) : stepDefinition.StepId,
                StepDisplayName = cap.DisplayName,
                FilePath = input.Primary == null ? null : input.Primary.Locator,
                Message = "Step started"
            }
            .Add("pluginId", stepDefinition.PluginId)
            .Add("capabilityId", stepDefinition.CapabilityId)
            .Add("inputLocator", input.Primary == null ? null : input.Primary.Locator));

            IStep step;
            try
            {
                step = plugin.CreateStep(stepDefinition.CapabilityId, stepDefinition.Parameters);
            }
            catch (Exception ex)
            {
                context.Logger?.Log(AppLogLevel.Error, "Failed to create step for '" + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId + "' (" + cap.DisplayName + ").", ex);
                return Result<StepOutput>.Fail(Error.NotSupported("Failed to create step for '" + cap.DisplayName + "'.", ex.Message));
            }

            Result<StepOutput> stepResult;
            var previousStepDefinition = context.CurrentStepDefinition;
            context.CurrentStepDefinition = stepDefinition;
            try
            {
                stepResult = await step.ExecuteAsync(input, context, progress, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                stepSw.Stop();
                context.Logger?.Log(AppLogLevel.Error, "Step exception: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId + " durationMs=" + stepSw.ElapsedMilliseconds, ex);
                EmitStepStats(context, new StatsEvent(StatsEventKind.StepCompleted)
                {
                    StepId = string.IsNullOrWhiteSpace(stepDefinition.StepId) ? (stepDefinition.PluginId + "/" + stepDefinition.CapabilityId) : stepDefinition.StepId,
                    StepDisplayName = cap.DisplayName,
                    FilePath = input.Primary == null ? null : input.Primary.Locator,
                    Message = "Step exception"
                }
                .Add("pluginId", stepDefinition.PluginId)
                .Add("capabilityId", stepDefinition.CapabilityId)
                .Add("durationMs", ((int)stepSw.ElapsedMilliseconds).ToString())
                .Add("success", "false")
                .Add("error", ex.Message));
                return Result<StepOutput>.Fail(Error.NotSupported("Step threw an exception: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId, ex.ToString()));
            }
            finally
            {
                context.CurrentStepDefinition = previousStepDefinition;
            }

            if (stepResult.IsFailure)
            {
                stepSw.Stop();
                var err = stepResult.Error;
                if (err != null)
                {
                    context.Logger?.Log(AppLogLevel.Error,
                        "Step failed: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId +
                        " durationMs=" + stepSw.ElapsedMilliseconds +
                        " code=" + err.Code +
                        " message=" + err.Message +
                        " details=" + err.Details);
                }
                else
                {
                    context.Logger?.Log(AppLogLevel.Error, "Step failed: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId + " durationMs=" + stepSw.ElapsedMilliseconds + " (no error details)");
                }

                EmitStepStats(context, new StatsEvent(StatsEventKind.StepCompleted)
                {
                    StepId = string.IsNullOrWhiteSpace(stepDefinition.StepId) ? (stepDefinition.PluginId + "/" + stepDefinition.CapabilityId) : stepDefinition.StepId,
                    StepDisplayName = cap.DisplayName,
                    FilePath = input.Primary == null ? null : input.Primary.Locator,
                    Message = "Step failed"
                }
                .Add("pluginId", stepDefinition.PluginId)
                .Add("capabilityId", stepDefinition.CapabilityId)
                .Add("durationMs", ((int)stepSw.ElapsedMilliseconds).ToString())
                .Add("success", "false")
                .Add("error", stepResult.Error == null ? null : stepResult.Error.Message));
                return Result<StepOutput>.Fail(stepResult.Error);
            }

            var output = stepResult.Value;
            if (output != null && output.RequiresInteraction)
            {
                var interactionResult = await HandleInteractionAsync(step, output, context, progress, cancellationToken).ConfigureAwait(false);
                if (interactionResult.IsFailure)
                {
                    return Result<StepOutput>.Fail(interactionResult.Error);
                }

                output = interactionResult.Value;
            }

            stepSw.Stop();
            context.Logger?.Log(AppLogLevel.Debug, "Step end: " + stepDefinition.PluginId + "/" + stepDefinition.CapabilityId + " durationMs=" + stepSw.ElapsedMilliseconds + " outputs=" + (output != null ? output.OutputArtifacts.Count : 0));

            if (output != null)
            {
                foreach (var artifact in output.OutputArtifacts)
                {
                    artifact?.MarkRepresentationAvailable(cap.ProducedRepresentation);
                }
            }

            var stepCompletedStats = new StatsEvent(StatsEventKind.StepCompleted)
            {
                StepId = string.IsNullOrWhiteSpace(stepDefinition.StepId) ? (stepDefinition.PluginId + "/" + stepDefinition.CapabilityId) : stepDefinition.StepId,
                StepDisplayName = cap.DisplayName,
                FilePath = input.Primary == null ? null : input.Primary.Locator,
                Message = "Step completed"
            }
            .Add("pluginId", stepDefinition.PluginId)
            .Add("capabilityId", stepDefinition.CapabilityId)
            .Add("durationMs", ((int)stepSw.ElapsedMilliseconds).ToString())
            .Add("success", "true")
            .Add("outputArtifacts", output == null ? "0" : output.OutputArtifacts.Count.ToString());

            if (output != null && output.Properties != null)
            {
                for (var i = 0; i < output.Properties.Count; i++)
                {
                    var property = output.Properties[i];
                    if (property == null || string.IsNullOrWhiteSpace(property.Name))
                    {
                        continue;
                    }

                    var propertyKey = BuildPropertyStatsKey(property.Group, property.Name);
                    if (!string.IsNullOrWhiteSpace(propertyKey))
                    {
                        stepCompletedStats.Add(propertyKey, property.Value);
                    }
                }
            }

            EmitStepStats(context, stepCompletedStats);

            return Result<StepOutput>.Ok(output ?? new StepOutput());
        }

        private static string BuildPropertyStatsKey(string group, string name)
        {
            var trimmedName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return null;
            }

            var trimmedGroup = (group ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedGroup))
            {
                return "property." + trimmedName;
            }

            return "property." + trimmedGroup + ":" + trimmedName;
        }

        private void EnsureHostBindings(StepContext context)
        {
            if (context.PluginCatalog == null)
            {
                context.PluginCatalog = _catalog;
            }

            if (context.ExecutionHost == null)
            {
                context.ExecutionHost = this;
            }
        }

        private static Artifact ResolveReference(PipelineStepDefinition stepDef, Artifact current, Artifact original, Artifact explicitReference)
        {
            if (stepDef == null)
            {
                return explicitReference;
            }

            if (stepDef.ReferenceAnchor == ReferenceAnchorKind.OriginalInput)
            {
                return original;
            }

            if (stepDef.ReferenceAnchor == ReferenceAnchorKind.PreviousStepOutput)
            {
                return current;
            }

            return explicitReference;
        }

        private static async Task<Result<StepOutput>> HandleInteractionAsync(
            IStep step,
            StepOutput output,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            var req = output.InteractionRequest;
            if (req == null)
            {
                return Result<StepOutput>.Ok(output);
            }

            if (req.HeadlessBehavior == HeadlessBehavior.SkipStep)
            {
                context.Logger?.Log(AppLogLevel.Warn, "Skipping interactive step '" + (req.Title ?? "Unnamed") + "' (headless behavior).");
                return Result<StepOutput>.Ok(new StepOutput());
            }

            if (req.HeadlessBehavior == HeadlessBehavior.UseDefaults)
            {
                var interactive = step as IInteractiveStep;
                if (interactive == null || string.IsNullOrWhiteSpace(output.ContinuationToken))
                {
                    return Result<StepOutput>.Fail(Error.NotSupported("Step requested interaction, but cannot be resumed (missing IInteractiveStep or token)."));
                }

                var resp = new InteractionResponse
                {
                    Approved = true,
                    SelectedIndex = req.DefaultChoiceIndex,
                    UpdatedValues = req.CurrentValues
                };

                var resumeInput = new StepResumeInput(output.ContinuationToken, resp);
                return await interactive.ResumeAsync(resumeInput, context, progress, cancellationToken).ConfigureAwait(false);
            }

            return Result<StepOutput>.Fail(Error.NotSupported(
                "Pipeline requires user interaction, but no interaction host is available.",
                "Configure a host that can render and resume interaction requests."));
        }

        private static void EmitStepStats(StepContext context, StatsEvent statsEvent)
        {
            if (context == null || context.StatsSink == null || statsEvent == null)
            {
                return;
            }

            string value;
            if (context.Environment != null)
            {
                if (string.IsNullOrWhiteSpace(statsEvent.RunId) && context.Environment.TryGetValue("RunId", out value))
                {
                    statsEvent.RunId = value;
                }

                if (string.IsNullOrWhiteSpace(statsEvent.PresetId) && context.Environment.TryGetValue("PresetId", out value))
                {
                    statsEvent.PresetId = value;
                }

                if (string.IsNullOrWhiteSpace(statsEvent.FileId) && context.Environment.TryGetValue("FileId", out value))
                {
                    statsEvent.FileId = value;
                }

                if (string.IsNullOrWhiteSpace(statsEvent.FilePath) && context.Environment.TryGetValue("InputSourcePath", out value))
                {
                    statsEvent.FilePath = value;
                }
            }

            context.StatsSink.Emit(statsEvent);
        }
    }
}
