using System;
using FlawlessPictury.Presentation.WinForms.CrossCutting;

namespace FlawlessPictury.Presentation.WinForms.Views
{
    /// <summary>
    /// Passive View contract for the modeless log window.
    /// </summary>
    public interface ILogView
    {
        event EventHandler ClearRequested;
        event EventHandler CopyAllRequested;
        event EventHandler SaveAsRequested;
        event EventHandler CloseRequested;
        event EventHandler AutoScrollChanged;
        event EventHandler ShowDebugMessagesChanged;

        bool AutoScrollEnabled { get; }
        bool ShowDebugMessages { get; }
        bool IsVisible { get; }

        void ShowModeless(object owner);
        void HideWindow();
        void BringToFrontAndActivate();

        void SetRowCount(int count);
        void SetRowProvider(Func<int, LogEventArgs> provider);

        void RefreshRows();
        void EnsureRowVisible(int index);
        string PromptSaveAsPath(string initialDirectory, string initialFileName);
    }
}
