using System.ComponentModel;

namespace FlawlessPictury.AppCore.Presets
{
    /// <summary>
    /// Describes how the preset resolves its final output directory.
    /// </summary>
    public enum OutputDirectoryMode
    {
        [Description("Relative to each input file")]
        RelativeToInput = 0,

        [Description("Absolute directory")]
        AbsoluteDirectory = 1
    }
}
