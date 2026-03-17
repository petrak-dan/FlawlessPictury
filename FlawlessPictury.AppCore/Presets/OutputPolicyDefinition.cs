using System;
using System.ComponentModel;

namespace FlawlessPictury.AppCore.Presets
{
    /// <summary>
    /// Host-owned output policy applied after the pipeline finishes.
    /// This is intentionally not a normal plugin step.
    /// </summary>
    public sealed class OutputPolicyDefinition
    {
        public OutputPolicyDefinition()
        {
            DirectoryMode = OutputDirectoryMode.RelativeToInput;
            DirectoryPath = @".\Flawless";
            NamingPattern = string.Empty;
            PreserveSourceFileTimes = false;
        }

        [Category("Directory")]
        [DisplayName("Directory mode")]
        [Description("Whether the directory path is resolved relative to each input file or treated as an absolute directory.")]
        public OutputDirectoryMode DirectoryMode { get; set; }

        [Category("Directory")]
        [DisplayName("Directory path")]
        [Description(@"Relative example: .\Flawless. Absolute example: C:\Results\Thesis.")]
        public string DirectoryPath { get; set; }

        [Category("Naming")]
        [DisplayName("Naming pattern (planned)")]
        [Description("Saved in the preset for advanced output naming rules. Current runtime builds do not apply it.")]
        public string NamingPattern { get; set; }

        [Category("File attributes")]
        [DisplayName("Preserve source file times")]
        [Description("After the final output file is committed, copy creation / last write / last access timestamps from the original source file.")]
        public bool PreserveSourceFileTimes { get; set; }

        public static OutputPolicyDefinition CreateDefault()
        {
            return new OutputPolicyDefinition();
        }
        public bool IsDefault()
        {
            return DirectoryMode == OutputDirectoryMode.RelativeToInput
                && string.Equals(string.IsNullOrWhiteSpace(DirectoryPath) ? @".\Flawless" : DirectoryPath, @".\Flawless", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(NamingPattern)
                && !PreserveSourceFileTimes;
        }


        public static OutputPolicyDefinition Clone(OutputPolicyDefinition source)
        {
            if (source == null)
            {
                return CreateDefault();
            }

            return new OutputPolicyDefinition
            {
                DirectoryMode = source.DirectoryMode,
                DirectoryPath = source.DirectoryPath,
                NamingPattern = source.NamingPattern,
                PreserveSourceFileTimes = source.PreserveSourceFileTimes
            };
        }

        public string GetDisplayText()
        {
            var path = string.IsNullOrWhiteSpace(DirectoryPath) ? @".\Flawless" : DirectoryPath;
            return DirectoryMode == OutputDirectoryMode.AbsoluteDirectory
                ? path
                : path + " (relative to input)";
        }
    }
}
