using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using FlawlessPictury.AppCore.Plugins.Parameters;
using FlawlessPictury.AppCore.Plugins.Pipeline;
using FlawlessPictury.AppCore.Presets;

namespace FlawlessPictury.Infrastructure.Presets
{
    /// <summary>
    /// Converts preset JSON files to and from <see cref="PresetDefinition"/>.
    /// </summary>
    internal static class PresetJsonSerializer
    {
        public static PresetDefinition LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Preset path is required.", nameof(path));
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var settings = new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                };

                var serializer = new DataContractJsonSerializer(typeof(PresetFileDto), settings);
                var dto = serializer.ReadObject(fs) as PresetFileDto;
                return ConvertToDefinition(dto, path);
            }
        }

        public static void SaveToFile(PresetDefinition preset, string path)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Preset path is required.", nameof(path));
            }

            var dto = ConvertToDto(preset);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var settings = new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                };

                var serializer = new DataContractJsonSerializer(typeof(PresetFileDto), settings);
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, false, true, "  "))
                {
                    serializer.WriteObject(writer, dto);
                    writer.Flush();
                }
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        private static PresetDefinition ConvertToDefinition(PresetFileDto dto, string sourcePath)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PresetId) || dto.Pipeline == null || dto.Pipeline.Steps == null || dto.Pipeline.Steps.Count == 0)
            {
                return null;
            }

            var displayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.PresetId : dto.DisplayName;
            var pipeline = ConvertPipelineToDefinition(dto.Pipeline, displayName);
            if (pipeline == null || pipeline.Steps == null || pipeline.Steps.Count == 0)
            {
                return null;
            }

            var preset = new PresetDefinition(dto.PresetId, displayName, pipeline)
            {
                Description = dto.Description,
                IsReadOnly = dto.IsReadOnly,
                SourcePath = sourcePath,
                OutputPolicy = ConvertOutputPolicyToDefinition(dto.OutputPolicy),
                PreviewProviderRef = ConvertCapabilityReferenceToDefinition(dto.PreviewProvider)
            };

            if (dto.PropertiesProviders != null)
            {
                foreach (var provider in dto.PropertiesProviders)
                {
                    var converted = ConvertCapabilityReferenceToDefinition(provider);
                    if (converted != null && !converted.IsEmpty)
                    {
                        preset.PropertiesProviderRefs.Add(converted);
                    }
                }
            }

            return preset;
        }

        private static PresetFileDto ConvertToDto(PresetDefinition preset)
        {
            return new PresetFileDto
            {
                PresetId = preset.PresetId,
                DisplayName = string.Equals(preset.DisplayName, preset.PresetId, StringComparison.Ordinal) ? null : preset.DisplayName,
                Description = string.IsNullOrWhiteSpace(preset.Description) ? null : preset.Description,
                IsReadOnly = preset.IsReadOnly,
                OutputPolicy = ConvertOutputPolicyToDto(preset.OutputPolicy),
                Pipeline = ConvertPipelineToDto(preset.Pipeline, preset.DisplayName),
                PreviewProvider = ConvertCapabilityReferenceToDto(preset.PreviewProviderRef),
                PropertiesProviders = ConvertCapabilityReferenceListToDto(preset.PropertiesProviderRefs)
            };
        }

        private static PipelineDefinition ConvertPipelineToDefinition(PipelineFileDto dto, string defaultName)
        {
            if (dto == null)
            {
                return null;
            }

            var pipeline = new PipelineDefinition
            {
                Name = string.IsNullOrWhiteSpace(dto.Name) ? defaultName : dto.Name
            };

            if (dto.Steps != null)
            {
                for (var i = 0; i < dto.Steps.Count; i++)
                {
                    var step = ConvertStepToDefinition(dto.Steps[i]);
                    if (step == null)
                    {
                        return null;
                    }

                    pipeline.Steps.Add(step);
                }
            }

            return pipeline;
        }

        private static PipelineFileDto ConvertPipelineToDto(PipelineDefinition pipeline, string defaultName)
        {
            if (pipeline == null)
            {
                return null;
            }

            var dto = new PipelineFileDto
            {
                Name = string.IsNullOrWhiteSpace(pipeline.Name) || string.Equals(pipeline.Name, defaultName, StringComparison.Ordinal) ? null : pipeline.Name,
                Steps = new List<PipelineStepFileDto>()
            };

            if (pipeline != null && pipeline.Steps != null)
            {
                for (var i = 0; i < pipeline.Steps.Count; i++)
                {
                    var step = pipeline.Steps[i];
                    if (step != null)
                    {
                        dto.Steps.Add(ConvertStepToDto(step));
                    }
                }
            }

            return dto;
        }

        private static PipelineStepDefinition ConvertStepToDefinition(PipelineStepFileDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PluginId) || string.IsNullOrWhiteSpace(dto.CapabilityId))
            {
                return null;
            }

            var values = new ParameterValues();
            if (dto.Parameters != null)
            {
                foreach (var kv in dto.Parameters)
                {
                    values.Set(kv.Key, ConvertParameterValue(kv.Value));
                }
            }

            var step = new PipelineStepDefinition(dto.PluginId, dto.CapabilityId)
            {
                StepId = string.IsNullOrWhiteSpace(dto.StepId) ? Guid.NewGuid().ToString("N") : dto.StepId,
                SlotKey = dto.SlotKey,
                DisplayName = dto.DisplayName,
                Parameters = values,
                ReferenceAnchor = ParseReferenceAnchor(dto.ReferenceAnchor)
            };

            if (dto.ChildSteps != null)
            {
                for (var i = 0; i < dto.ChildSteps.Count; i++)
                {
                    var child = ConvertStepToDefinition(dto.ChildSteps[i]);
                    if (child == null)
                    {
                        return null;
                    }

                    step.ChildSteps.Add(child);
                }
            }

            return step;
        }

        private static PipelineStepFileDto ConvertStepToDto(PipelineStepDefinition step)
        {
            var dto = new PipelineStepFileDto
            {
                StepId = null,
                PluginId = step.PluginId,
                CapabilityId = step.CapabilityId,
                SlotKey = string.IsNullOrWhiteSpace(step.SlotKey) ? null : step.SlotKey,
                DisplayName = ShouldPersistDisplayName(step) ? step.DisplayName : null,
                ReferenceAnchor = step.ReferenceAnchor == ReferenceAnchorKind.None ? null : step.ReferenceAnchor.ToString(),
                Parameters = SerializeParameterValues(step.Parameters),
                ChildSteps = new List<PipelineStepFileDto>()
            };

            if (step.ChildSteps != null)
            {
                for (var i = 0; i < step.ChildSteps.Count; i++)
                {
                    var child = step.ChildSteps[i];
                    if (child != null)
                    {
                        dto.ChildSteps.Add(ConvertStepToDto(child));
                    }
                }
            }

            if (dto.Parameters != null && dto.Parameters.Count == 0)
            {
                dto.Parameters = null;
            }

            if (dto.ChildSteps != null && dto.ChildSteps.Count == 0)
            {
                dto.ChildSteps = null;
            }

            return dto;
        }

        private static CapabilityReference ConvertCapabilityReferenceToDefinition(CapabilityReferenceFileDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PluginId) || string.IsNullOrWhiteSpace(dto.CapabilityId))
            {
                return null;
            }

            return new CapabilityReference(dto.PluginId, dto.CapabilityId);
        }

        private static CapabilityReferenceFileDto ConvertCapabilityReferenceToDto(CapabilityReference reference)
        {
            if (reference == null || reference.IsEmpty)
            {
                return null;
            }

            return new CapabilityReferenceFileDto
            {
                PluginId = reference.PluginId,
                CapabilityId = reference.CapabilityId
            };
        }

        private static List<CapabilityReferenceFileDto> ConvertCapabilityReferenceListToDto(IList<CapabilityReference> references)
        {
            var list = new List<CapabilityReferenceFileDto>();
            if (references == null)
            {
                return null;
            }

            for (var i = 0; i < references.Count; i++)
            {
                var dto = ConvertCapabilityReferenceToDto(references[i]);
                if (dto != null)
                {
                    list.Add(dto);
                }
            }

            return list.Count == 0 ? null : list;
        }

        private static OutputPolicyDefinition ConvertOutputPolicyToDefinition(OutputPolicyFileDto dto)
        {
            if (dto == null)
            {
                return OutputPolicyDefinition.CreateDefault();
            }

            OutputDirectoryMode mode;
            if (!Enum.TryParse(dto.DirectoryMode, true, out mode))
            {
                mode = OutputDirectoryMode.RelativeToInput;
            }

            return new OutputPolicyDefinition
            {
                DirectoryMode = mode,
                DirectoryPath = string.IsNullOrWhiteSpace(dto.DirectoryPath) ? @".\Flawless" : dto.DirectoryPath,
                NamingPattern = dto.NamingPattern,
                PreserveSourceFileTimes = dto.PreserveSourceFileTimes
            };
        }

        private static OutputPolicyFileDto ConvertOutputPolicyToDto(OutputPolicyDefinition policy)
        {
            policy = policy ?? OutputPolicyDefinition.CreateDefault();
            if (policy.IsDefault())
            {
                return null;
            }

            return new OutputPolicyFileDto
            {
                DirectoryMode = policy.DirectoryMode == OutputDirectoryMode.RelativeToInput ? null : policy.DirectoryMode.ToString(),
                DirectoryPath = string.Equals(policy.DirectoryPath, @".\Flawless", StringComparison.Ordinal) ? null : policy.DirectoryPath,
                NamingPattern = string.IsNullOrWhiteSpace(policy.NamingPattern) ? null : policy.NamingPattern,
                PreserveSourceFileTimes = policy.PreserveSourceFileTimes
            };
        }

        private static bool ShouldPersistDisplayName(PipelineStepDefinition step)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.DisplayName))
            {
                return false;
            }

            return !string.Equals(step.DisplayName, step.PluginId + "/" + step.CapabilityId, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> SerializeParameterValues(ParameterValues values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
            {
                return result;
            }

            foreach (var kv in values.ToDictionary())
            {
                result[kv.Key] = ConvertParameterValueToString(kv.Value);
            }

            return result;
        }

        private static string ConvertParameterValueToString(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool)
            {
                return ((bool)value) ? "true" : "false";
            }

            if (value is string)
            {
                return (string)value;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static object ConvertParameterValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int i;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                return i;
            }

            double d;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }

            return value;
        }

        private static ReferenceAnchorKind ParseReferenceAnchor(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ReferenceAnchorKind.None;
            }

            ReferenceAnchorKind parsed;
            if (Enum.TryParse(raw, true, out parsed))
            {
                return parsed;
            }

            return ReferenceAnchorKind.None;
        }

        [DataContract]
        private sealed class PresetFileDto
        {
            [DataMember(Order = 1)]
            public string PresetId { get; set; }

            [DataMember(Order = 2)]
            public string DisplayName { get; set; }

            [DataMember(Order = 3)]
            public string Description { get; set; }

            [DataMember(Order = 4, EmitDefaultValue = false)]
            public bool IsReadOnly { get; set; }

            [DataMember(Order = 5, EmitDefaultValue = false)]
            public OutputPolicyFileDto OutputPolicy { get; set; }

            [DataMember(Order = 6)]
            public PipelineFileDto Pipeline { get; set; }

            [DataMember(Order = 7, EmitDefaultValue = false)]
            public CapabilityReferenceFileDto PreviewProvider { get; set; }

            [DataMember(Order = 8, EmitDefaultValue = false)]
            public List<CapabilityReferenceFileDto> PropertiesProviders { get; set; }

            [DataMember(Name = "inspectionPipeline", EmitDefaultValue = false)]
            public object LegacyInspectionPipeline { get; set; }
        }

        [DataContract]
        private sealed class CapabilityReferenceFileDto
        {
            [DataMember(Order = 1)]
            public string PluginId { get; set; }

            [DataMember(Order = 2)]
            public string CapabilityId { get; set; }
        }

        [DataContract]
        private sealed class OutputPolicyFileDto
        {
            [DataMember(Order = 1, EmitDefaultValue = false)]
            public string DirectoryMode { get; set; }

            [DataMember(Order = 2, EmitDefaultValue = false)]
            public string DirectoryPath { get; set; }

            [DataMember(Order = 3, EmitDefaultValue = false)]
            public string NamingPattern { get; set; }

            [DataMember(Order = 4, EmitDefaultValue = false)]
            public bool PreserveSourceFileTimes { get; set; }
        }

        [DataContract]
        private sealed class PipelineFileDto
        {
            [DataMember(Order = 1)]
            public string Name { get; set; }

            [DataMember(Order = 2)]
            public List<PipelineStepFileDto> Steps { get; set; }
        }

        [DataContract]
        private sealed class PipelineStepFileDto
        {
            [DataMember(Order = 1, EmitDefaultValue = false)]
            public string StepId { get; set; }

            [DataMember(Order = 2)]
            public string PluginId { get; set; }

            [DataMember(Order = 3)]
            public string CapabilityId { get; set; }

            [DataMember(Order = 4, EmitDefaultValue = false)]
            public string SlotKey { get; set; }

            [DataMember(Order = 5, EmitDefaultValue = false)]
            public string DisplayName { get; set; }

            [DataMember(Order = 6, EmitDefaultValue = false)]
            public string ReferenceAnchor { get; set; }

            [DataMember(Order = 7)]
            public Dictionary<string, string> Parameters { get; set; }

            [DataMember(Order = 8, EmitDefaultValue = false)]
            public List<PipelineStepFileDto> ChildSteps { get; set; }
        }
    }
}
