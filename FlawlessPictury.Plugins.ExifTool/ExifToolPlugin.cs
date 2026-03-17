using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.ExifTool.Steps;

namespace FlawlessPictury.Plugins.ExifTool
{
    /// <summary>
    /// Provides plugin capabilities for the associated toolset.
    /// </summary>
    public sealed class ExifToolPlugin : IPlugin
    {
        /// <summary>
        /// Gets the stable plugin identifier.
        /// </summary>
        public const string PluginId = "exiftool";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string CopyMetadataCapabilityId = "copy_metadata";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string ReadMetadataCapabilityId = "read_metadata";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string WriteMetadataCapabilityId = "write_metadata";

        /// <inheritdoc />
        public PluginMetadata GetMetadata()
        {
            return new PluginMetadata(PluginId, "ExifTool", new Version(1, 2, 0, 0))
            {
                Description = "Metadata copy, read and write capabilities powered by ExifTool.",
                Author = "FlawlessPictury"
            };
        }

        /// <inheritdoc />
        public IEnumerable<CapabilityDescriptor> GetCapabilities()
        {
            var copy = new CapabilityDescriptor(CopyMetadataCapabilityId, "Copy Metadata (ExifTool)", OperationKind.MetadataOperator)
            {
                Description = "Best-effort metadata copy from original source file to the current artifact using ExifTool.",
                Effect = OperationEffect.ProducesNewArtifact,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            };
            copy.AddInput("*/*").AddOutput("*/*");
            copy.Parameters.Add(new ParameterDefinition("executablePath", "Executable Path", ParameterType.Path) { DefaultValue = "exiftool", IsAdvanced = true });
            copy.Parameters.Add(new ParameterDefinition("excludeOrientationTag", "Exclude Orientation Tag", ParameterType.Boolean) { DefaultValue = "false", IsAdvanced = true, Description = "Exclude Orientation while copying metadata, useful when pixels were already auto-oriented." });
            yield return copy;

            var props = new CapabilityDescriptor(ReadMetadataCapabilityId, "Read Metadata (ExifTool)", OperationKind.Analyzer)
            {
                Description = "Reads metadata/tag properties from the current artifact using ExifTool.",
                Effect = OperationEffect.NoChange,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            }.AddRole(CapabilityRole.PropertiesProvider);
            props.AddInput("*/*");
            props.Parameters.Add(new ParameterDefinition("executablePath", "Executable Path", ParameterType.Path) { DefaultValue = "exiftool", IsAdvanced = true });
            yield return props;

            var write = new CapabilityDescriptor(WriteMetadataCapabilityId, "Write Metadata (ExifTool)", OperationKind.MetadataOperator)
            {
                Description = "Advanced metadata transformation step that applies raw ExifTool write arguments to the current artifact.",
                Effect = OperationEffect.ProducesNewArtifact,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            };
            write.AddInput("*/*").AddOutput("*/*");
            write.Parameters.Add(new ParameterDefinition("executablePath", "Executable Path", ParameterType.Path) { DefaultValue = "exiftool", IsAdvanced = true });
            write.Parameters.Add(new ParameterDefinition("arguments", "Arguments", ParameterType.String) { IsRequired = true, IsAdvanced = true, Description = "Raw ExifTool write arguments, for example: -Artist=John Doe -overwrite_original" });
            yield return write;
        }

        /// <inheritdoc />
        public IStep CreateStep(string capabilityId, ParameterValues parameters)
        {
            if (string.Equals(capabilityId, CopyMetadataCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new CopyMetadataFromOriginalStep(parameters);
            }
            if (string.Equals(capabilityId, ReadMetadataCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new ReadMetadataPropertiesStep(parameters);
            }
            if (string.Equals(capabilityId, WriteMetadataCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new WriteMetadataArgumentsStep(parameters);
            }
            throw new KeyNotFoundException("Unknown capability: " + capabilityId);
        }
    }
}
