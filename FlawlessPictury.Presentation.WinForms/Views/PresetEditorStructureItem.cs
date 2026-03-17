using System;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    internal enum PresetEditorStructureItemKind
    {
        TopLevelStep = 0,
        ChildStep = 1,
        OutputPolicy = 2,
        FinalCommit = 3
    }

    internal sealed class PresetEditorStructureItem
    {
        public PresetEditorStructureItem(PresetEditorStructureItemKind kind, string key, string displayText, int stepIndex, string slotKey)
        {
            Kind = kind;
            Key = key ?? string.Empty;
            DisplayText = displayText ?? string.Empty;
            StepIndex = stepIndex;
            SlotKey = slotKey ?? string.Empty;
        }

        public PresetEditorStructureItemKind Kind { get; private set; }

        public string Key { get; private set; }

        public string DisplayText { get; private set; }

        public int StepIndex { get; private set; }

        public string SlotKey { get; private set; }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
