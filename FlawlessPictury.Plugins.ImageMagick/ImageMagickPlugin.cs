using System;
using System.Collections.Generic;
using FlawlessPictury.AppCore.Plugins;
using FlawlessPictury.AppCore.Plugins.Capabilities;
using FlawlessPictury.AppCore.Plugins.Execution;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Artifacts;
using FlawlessPictury.Plugins.ImageMagick.Steps;

namespace FlawlessPictury.Plugins.ImageMagick
{
    /// <summary>
    /// Provides plugin capabilities for the associated toolset.
    /// </summary>
    public sealed class ImageMagickPlugin : IPlugin
    {
        /// <summary>
        /// Gets the stable plugin identifier.
        /// </summary>
        public const string PluginId = "imagemagick";

        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string ConvertCapabilityId = "convert";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string CompareMetricCapabilityId = "compare_metric";
        /// <summary>
        /// Gets the stable capability identifier.
        /// </summary>
        public const string ViewImageCapabilityId = "view_image";

        private static readonly PluginMetadata _meta = new PluginMetadata(
            pluginId: PluginId,
            displayName: "ImageMagick Tools",
            version: new Version(0, 2, 1, 0))
        {
            Author = "FlawlessPictury",
            Description = "Generic ImageMagick-based capabilities (convert, compare_metric) driven by presets."
        };

        /// <inheritdoc />
        public PluginMetadata GetMetadata()
        {
            return _meta;
        }

        /// <inheritdoc />
        public IEnumerable<CapabilityDescriptor> GetCapabilities()
        {
            yield return BuildConvert();
            yield return BuildCompareMetric();
            yield return BuildViewImage();
        }

        /// <inheritdoc />
        public IStep CreateStep(string capabilityId, ParameterValues parameters)
        {
            if (string.IsNullOrWhiteSpace(capabilityId))
            {
                throw new ArgumentNullException(nameof(capabilityId));
            }

            parameters = parameters ?? new ParameterValues();

            if (string.Equals(capabilityId, ConvertCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new ConvertStep(parameters);
            }

            if (string.Equals(capabilityId, CompareMetricCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new CompareMetricStep(parameters);
            }

            if (string.Equals(capabilityId, ViewImageCapabilityId, StringComparison.OrdinalIgnoreCase))
            {
                return new ViewImageStep(parameters);
            }

            throw new NotSupportedException("Unknown capability id: " + capabilityId);
        }

        private static CapabilityDescriptor BuildConvert()
        {
            var cap = new CapabilityDescriptor(
                id: ConvertCapabilityId,
                displayName: "Convert (ImageMagick)",
                kind: OperationKind.Transformer);

            cap.Description = "Runs 'magick' with preset-defined options and writes a new output file.";
            cap.RequiredRepresentation = RepresentationKind.FilePath;
            cap.ProducedRepresentation = RepresentationKind.FilePath;
            cap.Effect = OperationEffect.ProducesNewArtifact;
            cap.AddRole(CapabilityRole.OptimizationEncoder);

            cap.AddInput("image/*");
            cap.AddOutput("image/*");

            cap.Parameters = BuildCommonSchema(includeMetric: false);

            cap.Parameters.Add(new ParameterDefinition(Keys.Format, "format", ParameterType.String)
            {
                Description = "Output format/extension (e.g., jpg, png, webp). Used for {format} in outputNamePattern.",
                DefaultValue = "jpg",
                IsRequired = true
            });

            cap.Parameters.Add(new ParameterDefinition(Keys.OutputNamePattern, "outputNamePattern", ParameterType.String)
            {
                Description = "Output filename pattern. Placeholders: {name}, {format}. Example: {name}.{format}",
                DefaultValue = "{name}.{format}",
                IsAdvanced = true
            });

            return cap;
        }

        private static CapabilityDescriptor BuildCompareMetric()
        {
            var cap = new CapabilityDescriptor(
                id: CompareMetricCapabilityId,
                displayName: "Compare Metric (ImageMagick)",
                kind: OperationKind.Analyzer);

            cap.Description = "Runs 'magick compare -metric <metric>' between Reference and Primary and emits a numeric metric.";
            cap.RequiredRepresentation = RepresentationKind.FilePath;
            cap.ProducedRepresentation = RepresentationKind.FilePath;
            cap.Effect = OperationEffect.NoChange;
            cap.AddRole(CapabilityRole.OptimizationMetric);

            cap.AddInput("image/*");

            cap.Parameters = BuildCommonSchema(includeMetric: true);

            cap.Parameters.Add(new ParameterDefinition(Keys.MetricKey, "metricKey", ParameterType.String)
            {
                Description = "Output metrics dictionary key name.",
                DefaultValue = "metric",
                IsAdvanced = true
            });

            return cap;
        }


        private static CapabilityDescriptor BuildViewImage()
        {
            var cap = new CapabilityDescriptor(
                id: ViewImageCapabilityId,
                displayName: "View Image (ImageMagick)",
                kind: OperationKind.Analyzer);

            cap.Description = "Renders a preview image directly through ImageMagick and emits PNG bytes for the UI.";
            cap.RequiredRepresentation = RepresentationKind.FilePath;
            cap.ProducedRepresentation = RepresentationKind.FilePath;
            cap.Effect = OperationEffect.NoChange;
            cap.AddRole(CapabilityRole.PreviewProvider);
            cap.AddInput("image/*");
            cap.Parameters = new ParameterSchema();
            cap.Parameters.Add(new ParameterDefinition(Keys.ExecutablePath, "executablePath", ParameterType.Path)
            {
                Description = "Path to magick.exe. Use 'magick' to auto-detect Tools/ImageMagick/magick.exe then PATH.",
                DefaultValue = "magick",
                IsRequired = true,
                IsAdvanced = true
            });
            return cap;
        }

        private static ParameterSchema BuildCommonSchema(bool includeMetric)
        {
            var schema = new ParameterSchema();

            schema.Add(new ParameterDefinition(Keys.ExecutablePath, "executablePath", ParameterType.Path)
            {
                Description = "Path to magick.exe. Use 'magick' to auto-detect Tools/ImageMagick/magick.exe then PATH.",
                DefaultValue = "magick",
                IsRequired = true,
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.Arguments, "arguments", ParameterType.String)
            {
                Description = "Raw ImageMagick option string appended before output path (advanced escape hatch).",
                DefaultValue = "",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.Quality, "quality", ParameterType.Int32)
            {
                Description = "Maps to -quality (format-dependent).",
                DefaultValue = "85",
                MinValue = "1",
                MaxValue = "100",
                IsAdvanced = true,
                IsSearchable = true
            });

            schema.Add(new ParameterDefinition(Keys.Strip, "strip", ParameterType.Boolean)
            {
                Description = "If true, adds -strip.",
                DefaultValue = "true",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.Interlace, "interlace", ParameterType.String)
            {
                Description = "Maps to -interlace. Common values: None, Plane.",
                DefaultValue = "Plane",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.Resize, "resize", ParameterType.String)
            {
                Description = "Maps to -resize. Example: 1920x1080>.",
                DefaultValue = "",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.AutoOrient, "autoOrient", ParameterType.Boolean)
            {
                Description = "If true, adds -auto-orient.",
                DefaultValue = "true",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.ColorSpace, "colorspace", ParameterType.String)
            {
                Description = "Maps to -colorspace. Example: sRGB.",
                DefaultValue = "",
                IsAdvanced = true
            });

            schema.Add(new ParameterDefinition(Keys.Define, "define", ParameterType.String)
            {
                Description = "Maps to -define <value>. For multiple defines, use arguments.",
                DefaultValue = "",
                IsAdvanced = true
            });

            ParameterDefinition inter;
            if (schema.TryGet(Keys.Interlace, out inter))
            {
                inter.AllowedValues.Add("None");
                inter.AllowedValues.Add("Plane");
            }

            if (includeMetric)
            {
                var metric = new ParameterDefinition(Keys.Metric, "metric", ParameterType.String)
                {
                    Description = "ImageMagick compare metric name (e.g., SSIM, PSNR, AE, MAE, RMSE, MSE).",
                    DefaultValue = "SSIM"
                };
                metric.AllowedValues.Add("SSIM");
                metric.AllowedValues.Add("PSNR");
                metric.AllowedValues.Add("AE");
                metric.AllowedValues.Add("MAE");
                metric.AllowedValues.Add("RMSE");
                metric.AllowedValues.Add("MSE");
                schema.Add(metric);
            }

            return schema;
        }

        internal static class Keys
        {
            public const string ExecutablePath = "executablePath";

            public const string Format = "format";
            public const string OutputNamePattern = "outputNamePattern";

            public const string Arguments = "arguments";
            public const string Quality = "quality";
            public const string Strip = "strip";
            public const string Interlace = "interlace";
            public const string Resize = "resize";
            public const string AutoOrient = "autoOrient";
            public const string ColorSpace = "colorspace";
            public const string Define = "define";

            public const string Metric = "metric";
            public const string MetricKey = "metricKey";
        }
    }
}
