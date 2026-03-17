using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.Plugins.GdiPlus.Steps;

namespace FlawlessPictury.Plugins.GdiPlus
{
    /// <summary>
    /// Lightweight built-in image capabilities using System.Drawing / GDI+.
    /// </summary>
    public sealed class GdiPlusPlugin : IPlugin
    {
        /// <summary>
        /// Gets the stable plugin identifier.
        /// </summary>
        public const string PluginId = "gdiplus";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string EncodeJpegCapabilityId = "encode_jpeg";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string TranscodeCapabilityId = "transcode_image";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string ViewImageCapabilityId = "view_image";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string ReadPropertiesCapabilityId = "read_image_properties";

        /// <inheritdoc />
        public PluginMetadata GetMetadata()
        {
            return new PluginMetadata(PluginId, "GDI+ Image Tools", new Version(0, 3, 0, 0))
            {
                Description = "Built-in image transcode, preview and property capabilities using System.Drawing (GDI+).",
                Author = "FlawlessPictury"
            };
        }

        /// <inheritdoc />
        public IEnumerable<CapabilityDescriptor> GetCapabilities()
        {
            yield return BuildEncodeJpeg();
            yield return BuildTranscode();
            yield return BuildViewImage();
            yield return BuildReadProperties();
        }

        /// <inheritdoc />
        public IStep CreateStep(string capabilityId, ParameterValues parameters)
        {
            if (string.Equals(capabilityId, EncodeJpegCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new EncodeJpegStep(parameters);
            }

            if (string.Equals(capabilityId, TranscodeCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new TranscodeImageStep(parameters);
            }

            if (string.Equals(capabilityId, ViewImageCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new ViewImageStep();
            }

            if (string.Equals(capabilityId, ReadPropertiesCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new ReadImagePropertiesStep();
            }

            throw new NotSupportedException("Unknown capability id: " + capabilityId);
        }

        private static CapabilityDescriptor BuildEncodeJpeg()
        {
            var encJpeg = new CapabilityDescriptor(EncodeJpegCapabilityId, "Encode JPEG (GDI+)", OperationKind.Transformer)
            {
                Description = "Re-encodes an image to JPEG with configurable quality.",
                Effect = OperationEffect.ProducesNewArtifact,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            }.AddRole(CapabilityRole.OptimizationEncoder);

            encJpeg.AddInput("image/*").AddOutput("image/jpeg");
            encJpeg.Parameters
                .Add(new ParameterDefinition("quality", "JPEG Quality", ParameterType.Int32)
                {
                    Description = "JPEG quality 1..100. Higher is better quality/larger file. Default: 85",
                    MinValue = "1",
                    MaxValue = "100",
                    IsSearchable = true
                }.WithDefault(85))
                .Add(new ParameterDefinition("suffix", "Filename Suffix", ParameterType.String)
                {
                    Description = "Appended to the base filename before .jpg (e.g., _q85). Default: _jpg"
                }.WithDefault("_jpg"));
            return encJpeg;
        }

        private static CapabilityDescriptor BuildTranscode()
        {
            var cap = new CapabilityDescriptor(TranscodeCapabilityId, "Transcode Image (GDI+)", OperationKind.Transformer)
            {
                Description = "Transcodes supported raster formats using built-in GDI+ codecs.",
                Effect = OperationEffect.ProducesNewArtifact,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            }.AddRole(CapabilityRole.OptimizationEncoder);

            cap.AddInput("image/*").AddOutput("image/*");
            var format = new ParameterDefinition("format", "Format", ParameterType.String)
            {
                Description = "Output image format.",
                DefaultValue = "jpg",
                IsRequired = true
            };
            format.AllowedValues.Add("jpg");
            format.AllowedValues.Add("png");
            format.AllowedValues.Add("bmp");
            format.AllowedValues.Add("gif");
            format.AllowedValues.Add("tif");
            cap.Parameters.Add(format);
            cap.Parameters.Add(new ParameterDefinition("quality", "JPEG Quality", ParameterType.Int32)
            {
                Description = "Used when format is JPEG. Ignored by formats without a quality concept.",
                DefaultValue = "85",
                MinValue = "1",
                MaxValue = "100",
                IsSearchable = true
            });
            cap.Parameters.Add(new ParameterDefinition("suffix", "Filename Suffix", ParameterType.String)
            {
                Description = "Appended to the base filename before the new extension.",
                DefaultValue = "_out"
            });
            return cap;
        }

        private static CapabilityDescriptor BuildViewImage()
        {
            var cap = new CapabilityDescriptor(ViewImageCapabilityId, "View Image (GDI+)", OperationKind.Analyzer)
            {
                Description = "Loads an image and emits an encoded preview payload for the UI.",
                Effect = OperationEffect.NoChange,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            }.AddRole(CapabilityRole.PreviewProvider);
            cap.AddInput("image/*");
            return cap;
        }

        private static CapabilityDescriptor BuildReadProperties()
        {
            var cap = new CapabilityDescriptor(ReadPropertiesCapabilityId, "Read Image Properties (GDI+)", OperationKind.Analyzer)
            {
                Description = "Reads basic image properties using GDI+.",
                Effect = OperationEffect.NoChange,
                RequiredRepresentation = RepresentationKind.FilePath,
                ProducedRepresentation = RepresentationKind.FilePath
            }.AddRole(CapabilityRole.PropertiesProvider);
            cap.AddInput("image/*");
            return cap;
        }
    }
}
