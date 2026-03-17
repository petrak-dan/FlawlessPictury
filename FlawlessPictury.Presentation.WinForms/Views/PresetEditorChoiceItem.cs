namespace FlawlessPictury.Presentation.WinForms.Views
{
    internal sealed class PresetEditorChoiceItem
    {
        public PresetEditorChoiceItem(string id, string displayText)
        {
            Id = id;
            DisplayText = displayText;
        }

        public string Id { get; private set; }

        public string DisplayText { get; private set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(DisplayText) ? (Id ?? string.Empty) : DisplayText;
        }
    }
}
