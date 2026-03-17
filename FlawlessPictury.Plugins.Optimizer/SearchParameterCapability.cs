using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Artifacts;

namespace FlawlessPictury.Plugins.Optimizer
{
    /// <summary>
    /// Builds the capability descriptor for the search-parameter orchestrator.
    /// </summary>
    internal static class SearchParameterCapability
    {
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string Id = "search_parameter";

        /// <summary>
        /// Creates the capability descriptor for the optimizer step.
        /// </summary>
        public static CapabilityDescriptor Build()
        {
            var cap = new CapabilityDescriptor(
                id: Id,
                displayName: "Search Parameter Optimizer",
                kind: OperationKind.Orchestrator);

            cap.Description = "Searches one encoder parameter by repeatedly running an encoder child step and a metric child step through the engine. Child steps are preferred when provided; legacy encoder.* / metric.* parameters remain supported for compatibility.";
            cap.RequiredRepresentation = RepresentationKind.FilePath;
            cap.ProducedRepresentation = RepresentationKind.FilePath;
            cap.Effect = OperationEffect.ProducesNewArtifact;

            cap.AddInput("*/*");
            cap.AddOutput("*/*");

            cap.Parameters.Add(new ParameterDefinition("encoder.pluginId", "encoder.pluginId", ParameterType.String)
            {
                Description = "PluginId of the encoder capability (for example 'imagemagick').",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition("encoder.capabilityId", "encoder.capabilityId", ParameterType.String)
            {
                Description = "CapabilityId of the encoder (for example 'convert').",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition("metric.pluginId", "metric.pluginId", ParameterType.String)
            {
                Description = "PluginId of the metric capability (for example 'imagemagick').",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition("metric.capabilityId", "metric.capabilityId", ParameterType.String)
            {
                Description = "CapabilityId of the metric capability (for example 'compare_metric').",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition("search.parameterKey", "search.parameterKey", ParameterType.String)
            {
                Description = "Name of the child encoder parameter to vary. Prefer parameters marked searchable by the encoder capability.",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition("search.min", "search.min", ParameterType.Int32)
            {
                Description = "Minimum value for the searched parameter.",
                IsRequired = true,
                DefaultValue = "0",
                MinValue = "0",
                MaxValue = "100000"
            });

            cap.Parameters.Add(new ParameterDefinition("search.max", "search.max", ParameterType.Int32)
            {
                Description = "Maximum value for the searched parameter.",
                IsRequired = true,
                DefaultValue = "100",
                MinValue = "0",
                MaxValue = "100000"
            });

            cap.Parameters.Add(new ParameterDefinition("search.maxTries", "search.maxTries", ParameterType.Int32)
            {
                Description = "Maximum number of encoder and metric attempts.",
                IsRequired = true,
                DefaultValue = "10",
                MinValue = "1",
                MaxValue = "1000"
            });

            var strategy = new ParameterDefinition("search.strategy", "search.strategy", ParameterType.Enum)
            {
                Description = "Search strategy.",
                IsRequired = true,
                DefaultValue = "BinarySearch"
            };
            strategy.AllowedValues.Add("BinarySearch");
            strategy.AllowedValues.Add("BracketThenBinary");
            strategy.AllowedValues.Add("CoarseToFine");
            cap.Parameters.Add(strategy);

            var qdir = new ParameterDefinition("search.qualityDirection", "search.qualityDirection", ParameterType.Enum)
            {
                Description = "Whether higher or lower parameter values produce higher quality.",
                IsRequired = true,
                DefaultValue = "HigherIsBetter",
                IsAdvanced = true
            };
            qdir.AllowedValues.Add("HigherIsBetter");
            qdir.AllowedValues.Add("LowerIsBetter");
            cap.Parameters.Add(qdir);

            cap.Parameters.Add(new ParameterDefinition("constraint.metricTarget", "constraint.metricTarget", ParameterType.Decimal)
            {
                Description = "Required metric threshold. Interpretation depends on the metric direction.",
                IsRequired = true,
                DefaultValue = "0.99",
                MinValue = "0",
                MaxValue = "1"
            });

            var dir = new ParameterDefinition("constraint.metricDirection", "constraint.metricDirection", ParameterType.Enum)
            {
                Description = "Whether higher metric values are better or lower metric values are better.",
                IsRequired = true,
                DefaultValue = "HigherIsBetter"
            };
            dir.AllowedValues.Add("HigherIsBetter");
            dir.AllowedValues.Add("LowerIsBetter");
            cap.Parameters.Add(dir);

            cap.Parameters.Add(new ParameterDefinition("constraint.metricKey", "constraint.metricKey", ParameterType.String)
            {
                Description = "Key in StepOutput.Metrics produced by the metric capability.",
                DefaultValue = "metric",
                IsAdvanced = true
            });

            cap.Parameters.Add(new ParameterDefinition("objective.requireSmallerThanInput", "objective.requireSmallerThanInput", ParameterType.Boolean)
            {
                Description = "If true, output larger than the input is rejected and the original is preserved when needed.",
                IsRequired = true,
                DefaultValue = "true",
                IsAdvanced = true
            });

            cap.Parameters.Add(new ParameterDefinition("objective.fallbackToBest", "objective.fallbackToBest", ParameterType.Boolean)
            {
                Description = "If true, the best candidate is used when no candidate meets the threshold.",
                IsRequired = true,
                DefaultValue = "true",
                IsAdvanced = true
            });

            cap.Parameters.Add(new ParameterDefinition("output.format", "output.format", ParameterType.String)
            {
                Description = "Optional output format label used for naming.",
                IsAdvanced = true
            });

            cap.Parameters.Add(new ParameterDefinition("output.namePattern", "output.namePattern", ParameterType.String)
            {
                Description = "Output filename pattern. Supports {name} and {format}.",
                DefaultValue = "{name}.{format}",
                IsAdvanced = true
            });

            cap.Parameters.Add(new ParameterDefinition("log.debugTries", "log.debugTries", ParameterType.Boolean)
            {
                Description = "If true, emits additional debug-only try details.",
                IsRequired = true,
                DefaultValue = "false",
                IsAdvanced = true
            });

            return cap;
        }
    }
}
