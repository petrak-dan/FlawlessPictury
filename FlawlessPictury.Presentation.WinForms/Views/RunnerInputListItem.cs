namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Lightweight UI model for one input row in the Runner input list.
    /// </summary>
    public sealed class RunnerInputListItem
    {
        public RunnerInputListItem(string filePath, string statusText, string originalSizeText, string newSizeText, string newFileText)
            : this(filePath, statusText, originalSizeText, newSizeText, newFileText, string.Empty)
        {
        }

        public RunnerInputListItem(string filePath, string statusText, string originalSizeText, string newSizeText, string newFileText, string newFilePath)
        {
            FilePath = filePath ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            OriginalSizeText = originalSizeText ?? string.Empty;
            NewSizeText = newSizeText ?? string.Empty;
            NewFileText = newFileText ?? string.Empty;
            NewFilePath = newFilePath ?? string.Empty;
        }

        public string FilePath { get; }
        public string StatusText { get; }
        public string OriginalSizeText { get; }
        public string NewSizeText { get; }
        public string NewFileText { get; }
        public string NewFilePath { get; }
    }
}
