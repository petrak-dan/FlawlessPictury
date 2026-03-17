using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlawlessPictury.AppCore.Common;
using AppLogLevel = FlawlessPictury.AppCore.CrossCutting.LogLevel;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Stats;

namespace FlawlessPictury.Plugins.Optimizer.Steps
{
    /// <summary>
    /// Searches an integer parameter by executing encoder and metric child steps until a suitable output is found.
    /// </summary>
    public sealed class SearchParameterStep : IStep
    {
        private readonly ParameterValues _p;

        /// <summary>
        /// Initializes a new <see cref="SearchParameterStep"/> instance.
        /// </summary>
        /// <param name="parameters">The configured optimizer parameters.</param>
        public SearchParameterStep(ParameterValues parameters)
        {
            _p = parameters ?? new ParameterValues();
        }

        /// <inheritdoc />
        public async Task<Result<StepOutput>> ExecuteAsync(
            StepInput input,
            StepContext context,
            IProgress<StepProgress> progress,
            CancellationToken cancellationToken)
        {
            if (input == null) return Result<StepOutput>.Fail(Error.Validation("StepInput is null."));
            if (context == null) return Result<StepOutput>.Fail(Error.Validation("StepContext is null."));
            if (context.ExecutionHost == null) return Result<StepOutput>.Fail(Error.DependencyMissing("StepContext.ExecutionHost is not set."));
            if (context.PluginCatalog == null) return Result<StepOutput>.Fail(Error.DependencyMissing("StepContext.PluginCatalog is not set."));
            if (input.Primary == null) return Result<StepOutput>.Fail(Error.Validation("Primary artifact is null."));

            var inPath = input.Primary.Locator;
            if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
            {
                return Result<StepOutput>.Fail(Error.Validation("Input file does not exist."));
            }

            var reference = input.Reference ?? input.Original ?? input.Primary;
            var currentStepDefinition = context.CurrentStepDefinition;

            var encoderTemplate = ResolveChildTemplate(currentStepDefinition, "encoder");
            var metricTemplate = ResolveChildTemplate(currentStepDefinition, "metric");

            if (encoderTemplate == null)
            {
                var encoderPluginId = _p.GetString("encoder.pluginId", null);
                var encoderCapabilityId = _p.GetString("encoder.capabilityId", null);
                if (string.IsNullOrWhiteSpace(encoderPluginId) || string.IsNullOrWhiteSpace(encoderCapabilityId))
                {
                    return Result<StepOutput>.Fail(Error.Validation("Encoder child step or encoder plugin/capability id is required."));
                }

                encoderTemplate = new PipelineStepDefinition(encoderPluginId, encoderCapabilityId)
                {
                    SlotKey = "encoder",
                    Parameters = ExtractPrefixedParameters(_p, "encoder.param.")
                };
            }

            if (metricTemplate == null)
            {
                var metricPluginId = _p.GetString("metric.pluginId", null);
                var metricCapabilityId = _p.GetString("metric.capabilityId", null);
                if (string.IsNullOrWhiteSpace(metricPluginId) || string.IsNullOrWhiteSpace(metricCapabilityId))
                {
                    return Result<StepOutput>.Fail(Error.Validation("Metric child step or metric plugin/capability id is required."));
                }

                metricTemplate = new PipelineStepDefinition(metricPluginId, metricCapabilityId)
                {
                    SlotKey = "metric",
                    Parameters = ExtractPrefixedParameters(_p, "metric.param.")
                };
            }

            var encoderCap = context.PluginCatalog.FindCapability(encoderTemplate.PluginId, encoderTemplate.CapabilityId);
            if (encoderCap == null)
            {
                return Result<StepOutput>.Fail(Error.DependencyMissing("Encoder capability not found: " + encoderTemplate.PluginId + "/" + encoderTemplate.CapabilityId));
            }

            var metricCap = context.PluginCatalog.FindCapability(metricTemplate.PluginId, metricTemplate.CapabilityId);
            if (metricCap == null)
            {
                return Result<StepOutput>.Fail(Error.DependencyMissing("Metric capability not found: " + metricTemplate.PluginId + "/" + metricTemplate.CapabilityId));
            }

            var searchKey = _p.GetString("search.parameterKey", null);
            if (string.IsNullOrWhiteSpace(searchKey))
            {
                searchKey = TryResolveSingleSearchableKey(encoderCap);
            }

            if (string.IsNullOrWhiteSpace(searchKey))
            {
                return Result<StepOutput>.Fail(Error.Validation("search.parameterKey is required unless the encoder capability exposes exactly one searchable parameter."));
            }

            ValidateCapabilityHints(context.Logger, encoderCap, metricCap, searchKey);

            var min = _p.GetInt32("search.min", 0);
            var max = _p.GetInt32("search.max", 100);
            var maxTries = _p.GetInt32("search.maxTries", 10);
            var strategy = _p.GetString("search.strategy", "BinarySearch");
            var qualityDirection = _p.GetString("search.qualityDirection", "HigherIsBetter");

            if (min > max)
            {
                return Result<StepOutput>.Fail(Error.Validation("search.min must be <= search.max."));
            }

            var target = GetDouble(_p, "constraint.metricTarget", 0.99);
            var metricDirection = _p.GetString("constraint.metricDirection", "HigherIsBetter");
            var metricKey = _p.GetString("constraint.metricKey", "metric");

            var requireSmaller = _p.GetBoolean("objective.requireSmallerThanInput", true);
            var fallbackToBest = _p.GetBoolean("objective.fallbackToBest", true);
            var fallbackToInput = _p.GetBoolean("objective.fallbackToInput", false);

            var outFormat = _p.GetString("output.format", null);
            if (string.IsNullOrWhiteSpace(outFormat))
            {
                outFormat = GetExtensionNoDot(inPath);
            }

            var namePattern = _p.GetString("output.namePattern", "{name}.{format}");
            var debugTries = _p.GetBoolean("log.debugTries", false);
            var inputSize = SafeGetLength(inPath);

            var encoderBaseParams = CloneParams(encoderTemplate.Parameters);
            var metricBaseParams = CloneParams(metricTemplate.Parameters);

            LogSearchPlan(context.Logger, encoderTemplate, metricTemplate, searchKey, min, max, maxTries, strategy, target, metricDirection, requireSmaller, inputSize);

            ISearchStrategy algo;
            if (string.Equals(strategy, "BinarySearch", StringComparison.OrdinalIgnoreCase))
            {
                algo = new BinarySearchStrategy();
            }
            else if (string.Equals(strategy, "CoarseToFine", StringComparison.OrdinalIgnoreCase))
            {
                algo = new CoarseToFineStrategy();
            }
            else
            {
                algo = new BracketThenBinaryStrategy();
            }

            var qualityImprovesWithHigher = !string.Equals(qualityDirection, "LowerIsBetter", StringComparison.OrdinalIgnoreCase);
            var tries = 0;
            Candidate bestAcceptable = null;
            Candidate bestAny = null;

            Func<int, Task<Candidate>> evaluate = async candidateValue =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                tries++;

                var tryLabel = tries.ToString(CultureInfo.InvariantCulture) + "/" + maxTries.ToString(CultureInfo.InvariantCulture);
                progress?.Report(new StepProgress(null, "Optimizer try " + tryLabel + ": " + searchKey + "=" + candidateValue.ToString(CultureInfo.InvariantCulture)));
                LogTryStart(context.Logger, tries, maxTries, searchKey, candidateValue);

                var attemptDir = Path.Combine(context.TempDirectory, "opt_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(attemptDir);

                var childCtx = context.CreateChild(attemptDir, attemptDir, attemptDir);

                var encoderStep = CloneStepDefinition(encoderTemplate);
                var encParams = CloneParams(encoderBaseParams);
                encParams.Set(searchKey, candidateValue.ToString(CultureInfo.InvariantCulture));
                encoderStep.Parameters = encParams;
                encoderStep.DisplayName = string.IsNullOrWhiteSpace(encoderStep.DisplayName) ? encoderCap.DisplayName : encoderStep.DisplayName;
                encoderStep.ReferenceAnchor = ReferenceAnchorKind.None;

                var encInput = new StepInput(input.Primary, input.Original, input.Reference, encParams);
                LogTryStage(context.Logger, tries, maxTries, "running encoder", encoderStep.PluginId + "/" + encoderStep.CapabilityId);
                var encRes = await context.ExecutionHost.ExecuteStepAsync(encoderStep, encInput, childCtx, null, cancellationToken).ConfigureAwait(false);
                if (encRes.IsFailure)
                {
                    LogTryFailure(context.Logger, tries, maxTries, candidateValue, "encoder failed", encRes.Error == null ? null : encRes.Error.Message);
                    EmitCandidateStats(context, input, currentStepDefinition, tries, maxTries, searchKey, candidateValue, null, null, false, "encoder_failed", encRes.Error == null ? null : encRes.Error.Message);
                    return Candidate.Fail(candidateValue, "Encoder failed: " + (encRes.Error == null ? "unknown error" : encRes.Error.Message));
                }

                if (encRes.Value == null || encRes.Value.OutputArtifacts.Count == 0)
                {
                    LogTryFailure(context.Logger, tries, maxTries, candidateValue, "encoder produced no output", null);
                    EmitCandidateStats(context, input, currentStepDefinition, tries, maxTries, searchKey, candidateValue, null, null, false, "encoder_no_output", null);
                    return Candidate.Fail(candidateValue, "Encoder produced no output artifacts.");
                }

                var candArtifact = encRes.Value.OutputArtifacts[0];
                var candPath = candArtifact.Locator;
                var candBytes = SafeGetLength(candPath);

                var metricStep = CloneStepDefinition(metricTemplate);
                metricStep.Parameters = CloneParams(metricBaseParams);
                metricStep.DisplayName = string.IsNullOrWhiteSpace(metricStep.DisplayName) ? metricCap.DisplayName : metricStep.DisplayName;
                metricStep.ReferenceAnchor = ReferenceAnchorKind.None;

                var metInput = new StepInput(candArtifact, input.Original, reference, metricStep.Parameters);
                LogTryStage(context.Logger, tries, maxTries, "running metric", metricStep.PluginId + "/" + metricStep.CapabilityId);
                var metRes = await context.ExecutionHost.ExecuteStepAsync(metricStep, metInput, childCtx, null, cancellationToken).ConfigureAwait(false);
                if (metRes.IsFailure)
                {
                    LogTryFailure(context.Logger, tries, maxTries, candidateValue, "metric failed", metRes.Error == null ? null : metRes.Error.Message);
                    EmitCandidateStats(context, input, currentStepDefinition, tries, maxTries, searchKey, candidateValue, null, candBytes, false, "metric_failed", metRes.Error == null ? null : metRes.Error.Message);
                    return Candidate.Fail(candidateValue, "Metric failed: " + (metRes.Error == null ? "unknown error" : metRes.Error.Message));
                }

                var metricVal = TryGetMetric(metRes.Value, metricKey);
                var cand = new Candidate(candidateValue, candArtifact, candBytes, metricVal, null);
                var acceptance = EvaluateAcceptance(cand, target, metricDirection, requireSmaller, inputSize);
                LogTryResult(context.Logger, debugTries, tries, maxTries, searchKey, candidateValue, metricVal, candBytes, acceptance, candPath, inputSize);
                EmitCandidateStats(context, input, currentStepDefinition, tries, maxTries, searchKey, candidateValue, metricVal, candBytes, acceptance.Passed, "evaluated", null);
                return cand;
            };

            Func<Candidate, bool> passes = cand => EvaluateAcceptance(cand, target, metricDirection, requireSmaller, inputSize).Passed;

            Action<Candidate> consider = cand =>
            {
                if (cand == null || cand.IsFailure)
                {
                    return;
                }

                if (bestAny == null || IsBetterAny(cand, bestAny))
                {
                    bestAny = cand;
                    LogBestCandidate(context.Logger, "overall", cand);
                }

                if (passes(cand) && (bestAcceptable == null || IsBetterAcceptable(cand, bestAcceptable)))
                {
                    bestAcceptable = cand;
                    LogBestCandidate(context.Logger, "acceptable", cand);
                }
            };

            var stratRes = await algo.RunAsync(min, max, maxTries, qualityImprovesWithHigher, evaluate, consider, passes, cancellationToken).ConfigureAwait(false);
            if (stratRes.IsFailure)
            {
                return Result<StepOutput>.Fail(new Error("processing", stratRes.ErrorMessage));
            }

            Candidate chosen = null;
            string chosenFrom = null;

            if (bestAcceptable != null)
            {
                chosen = bestAcceptable;
                chosenFrom = "acceptable";
            }
            else if (fallbackToInput)
            {
                chosen = null;
                chosenFrom = "input";
            }
            else if (fallbackToBest && bestAny != null)
            {
                chosen = bestAny;
                chosenFrom = "best_any";
                context.Logger?.Log(AppLogLevel.Warn, "No candidate met the metric threshold; using best available candidate (fallback).");
            }

            LogSummary(context.Logger, tries, maxTries, chosen, chosenFrom, bestAcceptable != null, target, metricDirection, strategy);

            if (chosen == null)
            {
                if (string.Equals(chosenFrom, "input", StringComparison.OrdinalIgnoreCase))
                {
                    context.Logger?.Log(AppLogLevel.Info, "No candidate met constraints; returning original file (objective.fallbackToInput=true).");
                    var passthrough = new StepOutput();
                    passthrough.OutputArtifacts.Add(input.Primary);
                    passthrough.Metrics["tries"] = tries;
                    passthrough.Metrics["chosenFrom"] = "input";
                    passthrough.Metrics["chosenParameterKey"] = searchKey;
                    passthrough.Metrics["chosenParameter"] = null;
                    passthrough.Metrics["passed"] = false;
                    passthrough.Metrics["chosenBytes"] = inputSize.HasValue ? (object)inputSize.Value : null;
                    return Result<StepOutput>.Ok(passthrough);
                }

                return Result<StepOutput>.Fail(new Error("processing", "No candidate outputs were produced."));
            }

            if (requireSmaller && inputSize.HasValue && chosen.Bytes.HasValue && chosen.Bytes.Value >= inputSize.Value)
            {
                context.Logger?.Log(AppLogLevel.Info, "Best candidate is not smaller than input; returning original file.");
                var passthrough = new StepOutput();
                passthrough.OutputArtifacts.Add(input.Primary);
                passthrough.Metrics["tries"] = tries;
                passthrough.Metrics["chosenFrom"] = "input";
                passthrough.Metrics["chosenParameterKey"] = searchKey;
                passthrough.Metrics["chosenParameter"] = null;
                passthrough.Metrics["chosenMetric"] = null;
                passthrough.Metrics["chosenBytes"] = inputSize.Value;
                passthrough.Metrics["passed"] = false;
                return Result<StepOutput>.Ok(passthrough);
            }

            var baseNameOut = Path.GetFileNameWithoutExtension(inPath);
            var outName = ExpandPattern(namePattern, baseNameOut, outFormat);
            var finalPath = Path.Combine(context.OutputDirectory, outName);

            Directory.CreateDirectory(context.OutputDirectory);
            File.Copy(chosen.Artifact.Locator, finalPath, true);

            var finalArtifact = new Artifact(Guid.NewGuid(), "*/*", finalPath, context.Clock.UtcNow);
            var output = new StepOutput();
            output.OutputArtifacts.Add(finalArtifact);
            output.Metrics["tries"] = tries;
            output.Metrics["chosenFrom"] = chosenFrom ?? "unknown";
            output.Metrics["chosenParameterKey"] = searchKey;
            output.Metrics["chosenParameter"] = chosen.ParameterValue;
            output.Metrics["chosenMetric"] = chosen.Metric;
            output.Metrics["chosenBytes"] = chosen.Bytes;
            output.Metrics["passed"] = bestAcceptable != null;

            progress?.Report(new StepProgress(100, "Done"));
            return Result<StepOutput>.Ok(output);
        }

        private static void ValidateCapabilityHints(ILogger logger, CapabilityDescriptor encoderCap, CapabilityDescriptor metricCap, string searchKey)
        {
            if (encoderCap != null && !encoderCap.HasRole(CapabilityRole.OptimizationEncoder))
            {
                logger?.Log(AppLogLevel.Warn, "Encoder capability does not explicitly declare OptimizationEncoder role: " + encoderCap.DisplayName);
            }

            if (metricCap != null && !metricCap.HasRole(CapabilityRole.OptimizationMetric))
            {
                logger?.Log(AppLogLevel.Warn, "Metric capability does not explicitly declare OptimizationMetric role: " + metricCap.DisplayName);
            }

            if (encoderCap != null && encoderCap.Parameters != null)
            {
                ParameterDefinition parameter;
                if (encoderCap.Parameters.TryGet(searchKey, out parameter))
                {
                    if (!parameter.IsSearchable)
                    {
                        logger?.Log(AppLogLevel.Warn, "Encoder parameter is not explicitly marked searchable: " + encoderCap.DisplayName + "." + searchKey);
                    }
                }
                else
                {
                    logger?.Log(AppLogLevel.Warn, "Encoder parameter was not found in capability schema: " + encoderCap.DisplayName + "." + searchKey);
                }
            }
        }


        private static PipelineStepDefinition ResolveChildTemplate(PipelineStepDefinition currentStepDefinition, string slotKey)
        {
            if (currentStepDefinition == null || currentStepDefinition.ChildSteps == null || string.IsNullOrWhiteSpace(slotKey))
            {
                return null;
            }

            for (var i = 0; i < currentStepDefinition.ChildSteps.Count; i++)
            {
                var child = currentStepDefinition.ChildSteps[i];
                if (child != null && string.Equals(child.SlotKey, slotKey, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private static string TryResolveSingleSearchableKey(CapabilityDescriptor encoderCap)
        {
            if (encoderCap == null || encoderCap.Parameters == null)
            {
                return null;
            }

            string key = null;
            for (var i = 0; i < encoderCap.Parameters.Parameters.Count; i++)
            {
                var parameter = encoderCap.Parameters.Parameters[i];
                if (parameter == null || !parameter.IsSearchable)
                {
                    continue;
                }

                if (key != null)
                {
                    return null;
                }

                key = parameter.Key;
            }

            return key;
        }

        private static PipelineStepDefinition CloneStepDefinition(PipelineStepDefinition source)
        {
            var clone = new PipelineStepDefinition(source.PluginId, source.CapabilityId)
            {
                StepId = source.StepId,
                SlotKey = source.SlotKey,
                DisplayName = source.DisplayName,
                Parameters = CloneParams(source.Parameters),
                ReferenceAnchor = source.ReferenceAnchor
            };

            if (source.ChildSteps != null)
            {
                for (var i = 0; i < source.ChildSteps.Count; i++)
                {
                    var child = source.ChildSteps[i];
                    if (child != null)
                    {
                        clone.ChildSteps.Add(CloneStepDefinition(child));
                    }
                }
            }

            return clone;
        }

        private static AcceptanceInfo EvaluateAcceptance(Candidate cand, double target, string metricDirection, bool requireSmaller, long? inputSize)
        {
            if (cand == null || cand.IsFailure)
            {
                return new AcceptanceInfo(false, "candidate_failed");
            }

            if (!cand.Metric.HasValue)
            {
                return new AcceptanceInfo(false, "metric_missing");
            }

            var metricPassed = string.Equals(metricDirection, "LowerIsBetter", StringComparison.OrdinalIgnoreCase)
                ? cand.Metric.Value <= target
                : cand.Metric.Value >= target;

            if (!metricPassed)
            {
                return new AcceptanceInfo(false, "metric_threshold_failed");
            }

            if (requireSmaller && inputSize.HasValue)
            {
                if (!cand.Bytes.HasValue)
                {
                    return new AcceptanceInfo(false, "candidate_size_missing");
                }

                if (cand.Bytes.Value >= inputSize.Value)
                {
                    return new AcceptanceInfo(false, "not_smaller_than_input");
                }
            }

            return new AcceptanceInfo(true, "accepted");
        }

        private static void LogSearchPlan(ILogger logger, PipelineStepDefinition encoderStep, PipelineStepDefinition metricStep, string searchKey, int min, int max, int maxTries, string strategy, double target, string metricDirection, bool requireSmaller, long? inputBytes)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Info,
                "Optimizer plan: encoder=" + encoderStep.PluginId + "/" + encoderStep.CapabilityId +
                " metric=" + metricStep.PluginId + "/" + metricStep.CapabilityId +
                " searchKey=" + searchKey +
                " range=" + min.ToString(CultureInfo.InvariantCulture) + ".." + max.ToString(CultureInfo.InvariantCulture) +
                " maxTries=" + maxTries.ToString(CultureInfo.InvariantCulture) +
                " strategy=" + strategy +
                " target=" + target.ToString("0.######", CultureInfo.InvariantCulture) +
                " metricDirection=" + metricDirection +
                " requireSmaller=" + (requireSmaller ? "true" : "false") +
                (inputBytes.HasValue ? (" inputBytes=" + inputBytes.Value.ToString(CultureInfo.InvariantCulture)) : string.Empty));
        }

        private static void LogTryStart(ILogger logger, int tryNumber, int maxTries, string searchKey, int candidateValue)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Info,
                "Optimizer try " + tryNumber.ToString(CultureInfo.InvariantCulture) + "/" + maxTries.ToString(CultureInfo.InvariantCulture) +
                ": " + searchKey + "=" + candidateValue.ToString(CultureInfo.InvariantCulture));
        }

        private static void LogTryStage(ILogger logger, int tryNumber, int maxTries, string stage, string target)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Info,
                "Optimizer try " + tryNumber.ToString(CultureInfo.InvariantCulture) + "/" + maxTries.ToString(CultureInfo.InvariantCulture) +
                ": " + stage +
                (string.IsNullOrWhiteSpace(target) ? string.Empty : (" [" + target + "]")));
        }

        private static void LogTryFailure(ILogger logger, int tryNumber, int maxTries, int candidateValue, string stage, string details)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Warn,
                "Optimizer try " + tryNumber.ToString(CultureInfo.InvariantCulture) + "/" + maxTries.ToString(CultureInfo.InvariantCulture) +
                " failed for value=" + candidateValue.ToString(CultureInfo.InvariantCulture) +
                ": " + stage +
                (string.IsNullOrWhiteSpace(details) ? string.Empty : (" (" + details + ")")));
        }

        private static void LogTryResult(ILogger logger, bool debugTries, int tryNumber, int maxTries, string searchKey, int candidateValue, double? metric, long? bytes, AcceptanceInfo acceptance, string artifactPath, long? inputBytes)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Info,
                "Optimizer try " + tryNumber.ToString(CultureInfo.InvariantCulture) + "/" + maxTries.ToString(CultureInfo.InvariantCulture) +
                " result: " + searchKey + "=" + candidateValue.ToString(CultureInfo.InvariantCulture) +
                " metric=" + (metric.HasValue ? metric.Value.ToString("0.######", CultureInfo.InvariantCulture) : "n/a") +
                " bytes=" + (bytes.HasValue ? bytes.Value.ToString(CultureInfo.InvariantCulture) : "n/a") +
                (inputBytes.HasValue ? (" inputBytes=" + inputBytes.Value.ToString(CultureInfo.InvariantCulture)) : string.Empty) +
                " status=" + acceptance.Reason);

            if (debugTries && !string.IsNullOrWhiteSpace(artifactPath))
            {
                logger.Log(AppLogLevel.Debug, "Optimizer try artifact: " + artifactPath);
            }
        }

        private static void LogBestCandidate(ILogger logger, string bucket, Candidate candidate)
        {
            if (logger == null || candidate == null)
            {
                return;
            }

            logger.Log(
                AppLogLevel.Info,
                "Optimizer best " + bucket + " updated: parameter=" + candidate.ParameterValue.ToString(CultureInfo.InvariantCulture) +
                " metric=" + (candidate.Metric.HasValue ? candidate.Metric.Value.ToString("0.######", CultureInfo.InvariantCulture) : "n/a") +
                " bytes=" + (candidate.Bytes.HasValue ? candidate.Bytes.Value.ToString(CultureInfo.InvariantCulture) : "n/a"));
        }

        private static void LogSummary(ILogger logger, int tries, int maxTries, Candidate chosen, string chosenFrom, bool passed, double target, string metricDirection, string strategy)
        {
            if (logger == null)
            {
                return;
            }

            var chosenParamText = chosen != null ? chosen.ParameterValue.ToString(CultureInfo.InvariantCulture) : "n/a";
            var chosenMetricText = (chosen != null && chosen.Metric.HasValue) ? chosen.Metric.Value.ToString("0.######", CultureInfo.InvariantCulture) : "n/a";
            var chosenBytesText = (chosen != null && chosen.Bytes.HasValue) ? chosen.Bytes.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
            var endedEarlyText = (tries < maxTries) ? "true" : "false";

            string stopReason;
            if (passed)
            {
                stopReason = (tries >= maxTries) ? "maxTries_reached_with_pass" : "pass_found";
            }
            else if (string.Equals(chosenFrom, "input", StringComparison.OrdinalIgnoreCase))
            {
                stopReason = "no_pass_fallbackToInput";
            }
            else if (string.Equals(chosenFrom, "best_any", StringComparison.OrdinalIgnoreCase))
            {
                stopReason = "no_pass_fallbackToBest";
            }
            else
            {
                stopReason = "no_pass_no_fallback";
            }

            logger.Log(
                AppLogLevel.Info,
                "Search summary: tries=" + tries +
                " endedEarly=" + endedEarlyText +
                " stopReason=" + stopReason +
                " chosenFrom=" + (chosenFrom ?? "none") +
                " chosenParam=" + chosenParamText +
                " chosenMetric=" + chosenMetricText +
                " chosenBytes=" + chosenBytesText +
                " passed=" + (passed ? "true" : "false") +
                " target=" + target.ToString("0.######", CultureInfo.InvariantCulture) +
                " metricDirection=" + metricDirection +
                " strategy=" + strategy);
        }

        private static double GetDouble(ParameterValues p, string key, double defaultValue)
        {
            var s = p.GetString(key, null);
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;

            double v;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : defaultValue;
        }

        private static ParameterValues ExtractPrefixedParameters(ParameterValues source, string prefix)
        {
            var result = new ParameterValues();
            if (source == null)
            {
                return result;
            }

            var map = source.ToDictionary();
            foreach (var kv in map)
            {
                if (kv.Key != null && kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var key = kv.Key.Substring(prefix.Length);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        result.Set(key, kv.Value);
                    }
                }
            }
            return result;
        }

        private static ParameterValues CloneParams(ParameterValues p)
        {
            var result = new ParameterValues();
            if (p == null)
            {
                return result;
            }

            var map = p.ToDictionary();
            foreach (var kv in map)
            {
                result.Set(kv.Key, kv.Value);
            }
            return result;
        }

        private static double? TryGetMetric(StepOutput output, string key)
        {
            if (output == null || output.Metrics == null) return null;
            if (string.IsNullOrWhiteSpace(key)) key = "metric";

            object raw;
            if (!output.Metrics.TryGetValue(key, out raw) || raw == null)
            {
                return null;
            }

            try
            {
                if (raw is double) return (double)raw;
                if (raw is float) return (double)(float)raw;
                if (raw is int) return (double)(int)raw;
                if (raw is long) return (double)(long)raw;
                if (raw is string)
                {
                    double v;
                    if (double.TryParse((string)raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    {
                        return v;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool IsBetterAcceptable(Candidate a, Candidate b)
        {
            if (a.Bytes.HasValue && b.Bytes.HasValue && a.Bytes.Value != b.Bytes.Value)
            {
                return a.Bytes.Value < b.Bytes.Value;
            }

            if (a.Metric.HasValue && b.Metric.HasValue && a.Metric.Value != b.Metric.Value)
            {
                return a.Metric.Value > b.Metric.Value;
            }

            return false;
        }

        private static bool IsBetterAny(Candidate a, Candidate b)
        {
            if (a.Bytes.HasValue && b.Bytes.HasValue && a.Bytes.Value != b.Bytes.Value)
            {
                return a.Bytes.Value < b.Bytes.Value;
            }

            if (a.Metric.HasValue && b.Metric.HasValue && a.Metric.Value != b.Metric.Value)
            {
                return a.Metric.Value > b.Metric.Value;
            }

            return false;
        }

        private static string ExpandPattern(string pattern, string name, string format)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                pattern = "{name}.{format}";
            }

            return pattern.Replace("{name}", name).Replace("{format}", format);
        }

        private static string GetExtensionNoDot(string path)
        {
            var ext = Path.GetExtension(path) ?? string.Empty;
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            if (string.IsNullOrWhiteSpace(ext)) ext = "out";
            return ext;
        }

        private static long? SafeGetLength(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                return new FileInfo(path).Length;
            }
            catch
            {
                return null;
            }
        }

        private sealed class AcceptanceInfo
        {
            public AcceptanceInfo(bool passed, string reason)
            {
                Passed = passed;
                Reason = reason ?? string.Empty;
            }

            public bool Passed { get; }
            public string Reason { get; }
        }

        private sealed class Candidate
        {
            public Candidate(int parameterValue, Artifact artifact, long? bytes, double? metric, string error)
            {
                ParameterValue = parameterValue;
                Artifact = artifact;
                Bytes = bytes;
                Metric = metric;
                Error = error;
            }

            public static Candidate Fail(int value, string error)
            {
                return new Candidate(value, null, null, null, error);
            }

            public int ParameterValue { get; }
            public Artifact Artifact { get; }
            public long? Bytes { get; }
            public double? Metric { get; }
            public string Error { get; }
            public bool IsFailure { get { return !string.IsNullOrWhiteSpace(Error); } }
        }

        private interface ISearchStrategy
        {
            Task<StrategyResult> RunAsync(int min, int max, int maxTries, bool qualityImprovesWithHigher, Func<int, Task<Candidate>> evaluate, Action<Candidate> consider, Func<Candidate, bool> passes, CancellationToken ct);
        }

        private sealed class StrategyResult
        {
            public bool IsFailure { get; private set; }
            public string ErrorMessage { get; private set; }

            public static StrategyResult Ok()
            {
                return new StrategyResult();
            }

            public static StrategyResult Fail(string message)
            {
                return new StrategyResult { IsFailure = true, ErrorMessage = message };
            }
        }

        private static void EmitCandidateStats(
            StepContext context,
            StepInput input,
            PipelineStepDefinition currentStepDefinition,
            int tryIndex,
            int maxTries,
            string searchKey,
            int candidateValue,
            double? metricValue,
            long? candidateBytes,
            bool passed,
            string outcome,
            string error)
        {
            if (context == null || context.StatsSink == null)
            {
                return;
            }

            string runId = null;
            string presetId = null;
            string fileId = null;
            string filePath = input == null || input.Original == null ? null : input.Original.Locator;
            if (context.Environment != null)
            {
                context.Environment.TryGetValue("RunId", out runId);
                context.Environment.TryGetValue("PresetId", out presetId);
                context.Environment.TryGetValue("FileId", out fileId);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    context.Environment.TryGetValue("InputSourcePath", out filePath);
                }
            }

            var evt = new StatsEvent(StatsEventKind.CandidateEvaluated)
            {
                RunId = runId,
                PresetId = presetId,
                FileId = fileId,
                FilePath = filePath,
                StepId = currentStepDefinition == null ? "optimizer/search_parameter" : currentStepDefinition.StepId,
                StepDisplayName = currentStepDefinition == null ? "Search Parameter Optimizer" : currentStepDefinition.DisplayName,
                Message = "Optimizer candidate evaluated"
            }
            .Add("tryIndex", tryIndex.ToString(CultureInfo.InvariantCulture))
            .Add("maxTries", maxTries.ToString(CultureInfo.InvariantCulture))
            .Add("parameterKey", searchKey)
            .Add("parameterValue", candidateValue.ToString(CultureInfo.InvariantCulture))
            .Add("metricValue", metricValue.HasValue ? metricValue.Value.ToString(CultureInfo.InvariantCulture) : null)
            .Add("candidateBytes", candidateBytes.HasValue ? candidateBytes.Value.ToString(CultureInfo.InvariantCulture) : null)
            .Add("passed", passed ? "true" : "false")
            .Add("outcome", outcome)
            .Add("error", error);

            context.StatsSink.Emit(evt);
        }

        private sealed class BinarySearchStrategy : ISearchStrategy
        {
            public async Task<StrategyResult> RunAsync(int min, int max, int maxTries, bool qualityImprovesWithHigher, Func<int, Task<Candidate>> evaluate, Action<Candidate> consider, Func<Candidate, bool> passes, CancellationToken ct)
            {
                var low = min;
                var high = max;
                var tries = 0;

                while (low <= high && tries < maxTries)
                {
                    ct.ThrowIfCancellationRequested();
                    var mid = low + ((high - low) / 2);
                    var cand = await evaluate(mid).ConfigureAwait(false);
                    consider(cand);

                    if (passes(cand))
                    {
                        if (qualityImprovesWithHigher)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            low = mid + 1;
                        }
                    }
                    else
                    {
                        if (qualityImprovesWithHigher)
                        {
                            low = mid + 1;
                        }
                        else
                        {
                            high = mid - 1;
                        }
                    }

                    tries++;
                }

                return StrategyResult.Ok();
            }
        }

        private sealed class BracketThenBinaryStrategy : ISearchStrategy
        {
            public async Task<StrategyResult> RunAsync(int min, int max, int maxTries, bool qualityImprovesWithHigher, Func<int, Task<Candidate>> evaluate, Action<Candidate> consider, Func<Candidate, bool> passes, CancellationToken ct)
            {
                var tries = 0;
                var cMin = await evaluate(min).ConfigureAwait(false);
                consider(cMin);
                tries++;
                if (tries >= maxTries) return StrategyResult.Ok();

                var cMax = await evaluate(max).ConfigureAwait(false);
                consider(cMax);
                tries++;
                if (tries >= maxTries) return StrategyResult.Ok();

                var minPass = passes(cMin);
                var maxPass = passes(cMax);

                if (qualityImprovesWithHigher)
                {
                    if (minPass || !maxPass)
                    {
                        return StrategyResult.Ok();
                    }
                }
                else
                {
                    if (maxPass || !minPass)
                    {
                        return StrategyResult.Ok();
                    }
                }

                var low = min + 1;
                var high = max - 1;

                while (low <= high && tries < maxTries)
                {
                    ct.ThrowIfCancellationRequested();
                    var mid = low + ((high - low) / 2);
                    var cand = await evaluate(mid).ConfigureAwait(false);
                    consider(cand);

                    if (passes(cand))
                    {
                        if (qualityImprovesWithHigher)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            low = mid + 1;
                        }
                    }
                    else
                    {
                        if (qualityImprovesWithHigher)
                        {
                            low = mid + 1;
                        }
                        else
                        {
                            high = mid - 1;
                        }
                    }

                    tries++;
                }

                return StrategyResult.Ok();
            }
        }

        private sealed class CoarseToFineStrategy : ISearchStrategy
        {
            public async Task<StrategyResult> RunAsync(int min, int max, int maxTries, bool qualityImprovesWithHigher, Func<int, Task<Candidate>> evaluate, Action<Candidate> consider, Func<Candidate, bool> passes, CancellationToken ct)
            {
                var span = max - min;
                var coarseStep = Math.Max(1, span / 4);
                var tries = 0;
                Candidate lastPassing = null;
                Candidate lastFailing = null;

                for (var value = min; value <= max && tries < maxTries; value += coarseStep)
                {
                    ct.ThrowIfCancellationRequested();
                    var cand = await evaluate(value).ConfigureAwait(false);
                    consider(cand);
                    if (passes(cand))
                    {
                        lastPassing = cand;
                        if (qualityImprovesWithHigher)
                        {
                            break;
                        }
                    }
                    else
                    {
                        lastFailing = cand;
                    }
                    tries++;
                }

                if (lastPassing == null || tries >= maxTries)
                {
                    return StrategyResult.Ok();
                }

                var low = lastFailing != null ? Math.Min(lastFailing.ParameterValue, lastPassing.ParameterValue) : min;
                var high = Math.Max(lastPassing.ParameterValue, low);

                while (low <= high && tries < maxTries)
                {
                    ct.ThrowIfCancellationRequested();
                    var mid = low + ((high - low) / 2);
                    var cand = await evaluate(mid).ConfigureAwait(false);
                    consider(cand);

                    if (passes(cand))
                    {
                        if (qualityImprovesWithHigher)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            low = mid + 1;
                        }
                    }
                    else
                    {
                        if (qualityImprovesWithHigher)
                        {
                            low = mid + 1;
                        }
                        else
                        {
                            high = mid - 1;
                        }
                    }

                    tries++;
                }

                return StrategyResult.Ok();
            }
        }
    }
}
