using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.Sample.Steps;

namespace FlawlessPictury.Plugins.Sample
{
    /// <summary>
    /// Sample plugin exposing basic file-oriented capabilities.
    /// </summary>
    public sealed class SamplePlugin : IPlugin
    {
        private const string PluginId = "sample";

        /// <inheritdoc />
        public PluginMetadata GetMetadata()
        {
            return new PluginMetadata(PluginId, "Sample Tools", new Version(0, 2, 0))
            {
                Description = "Sample plugin for testing pipelines (copy file, analyze file).",
                Author = "FlawlessPictury"
            };
        }

        /// <inheritdoc />
        public IEnumerable<CapabilityDescriptor> GetCapabilities()
        {
            // Copy File capability
            var copy = new CapabilityDescriptor("copy_file", "Copy File (Safe Output)", OperationKind.Transformer)
            {
                Description = "Copies the input artifact's file to the pipeline output folder with optional naming changes.",
                Effect = OperationEffect.ProducesNewArtifact,
                RequiredRepresentation = AppCore.Plugins.Artifacts.RepresentationKind.FilePath,
                ProducedRepresentation = AppCore.Plugins.Artifacts.RepresentationKind.FilePath
            };

            copy.AddInput("*/*").AddOutput("*/*");

            copy.Parameters
                .Add(new ParameterDefinition("suffix", "Filename Suffix", ParameterType.String)
                {
                    Description = "Appended to the base filename (before extension). Default: _out",
                    DefaultValue = "_out"
                })
                .Add(new ParameterDefinition("extension", "Force Extension", ParameterType.String)
                {
                    Description = "Optional forced extension (e.g., .jpg). Leave empty to keep original extension.",
                    DefaultValue = ""
                });

            yield return copy;

            // Analyze File capability
            var info = new CapabilityDescriptor("analyze_file", "Analyze File (Sample)", OperationKind.Analyzer)
            {
                Description = "Emits basic file metrics (size). Does not modify the artifact.",
                Effect = OperationEffect.NoChange,
                RequiredRepresentation = AppCore.Plugins.Artifacts.RepresentationKind.FilePath,
                ProducedRepresentation = AppCore.Plugins.Artifacts.RepresentationKind.MetadataOnly
            };

            info.AddInput("*/*").AddOutput("*/*");
            yield return info;
        }

        /// <inheritdoc />
        public IStep CreateStep(string capabilityId, ParameterValues parameters)
        {
            if (string.Equals(capabilityId, "copy_file", StringComparison.OrdinalIgnoreCase))
            {
                return new CopyFileStep(parameters);
            }

            if (string.Equals(capabilityId, "analyze_file", StringComparison.OrdinalIgnoreCase))
            {
                return new FileInfoAnalyzerStep();
            }

            throw new NotSupportedException($"Unknown capability id: {capabilityId}");
        }
    }
}
