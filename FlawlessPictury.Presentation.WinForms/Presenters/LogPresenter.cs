using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FlawlessPictury.AppCore.CrossCutting;
using FlawlessPictury.Presentation.WinForms.CrossCutting;
using FlawlessPictury.Presentation.WinForms.Views;

namespace FlawlessPictury.Presentation.WinForms.Presenters
{
    /// <summary>
    /// Presenter controlling the modeless log window.
    /// </summary>
    public sealed class LogPresenter
    {
        private readonly ILogView _view;
        private readonly UiLogHub _logHub;
        private readonly ILogger _logger;
        private readonly int _maxUiLines;
        private readonly List<LogEventArgs> _allEntries;
        private List<LogEventArgs> _visibleEntries;
        private object _owner;

        public LogPresenter(ILogView view, UiLogHub logHub, ILogger logger, int maxUiLines)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _logHub = logHub ?? throw new ArgumentNullException(nameof(logHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _maxUiLines = Math.Max(100, maxUiLines);
            _allEntries = new List<LogEventArgs>(_maxUiLines);
            _visibleEntries = new List<LogEventArgs>(_maxUiLines);

            _view.SetRowProvider(GetRowAt);
            _view.ClearRequested += (s, e) => Clear();
            _view.CopyAllRequested += (s, e) => CopyAll();
            _view.SaveAsRequested += (s, e) => SaveAs();
            _view.CloseRequested += (s, e) => _view.HideWindow();
            _view.AutoScrollChanged += (s, e) => ScrollToEndIfNeeded();
            _view.ShowDebugMessagesChanged += (s, e) => ApplyVisibleFilter();

            var history = _logHub.GetBufferedEvents();
            if (history != null && history.Count > 0)
            {
                _allEntries.AddRange(history);
                EnforceCap();
            }

            ApplyVisibleFilter();

            _logHub.LogEmitted += (s, evt) =>
            {
                _allEntries.Add(evt);
                EnforceCap();

                var shouldShow = ShouldShow(evt);
                if (shouldShow)
                {
                    _visibleEntries.Add(evt);
                    _view.SetRowCount(_visibleEntries.Count);

                    if (_view.IsVisible)
                    {
                        _view.RefreshRows();
                        ScrollToEndIfNeeded();
                    }
                }

                if (evt.Level >= LogLevel.Error)
                {
                    AutoShowOnError();
                }
            };
        }

        public void SetOwner(object owner)
        {
            _owner = owner;
        }

        public void Show(object owner)
        {
            _owner = owner ?? _owner;
            _view.ShowModeless(_owner);
            _view.RefreshRows();
            _view.BringToFrontAndActivate();
            ScrollToEndIfNeeded();
        }

        public void Toggle(object owner)
        {
            if (_view.IsVisible)
            {
                _view.HideWindow();
                return;
            }

            Show(owner);
        }

        private void AutoShowOnError()
        {
            if (!_view.IsVisible)
            {
                _view.ShowModeless(_owner);
            }

            _view.RefreshRows();
            _view.BringToFrontAndActivate();
            ScrollToEndIfNeeded();
        }

        private void ApplyVisibleFilter()
        {
            _visibleEntries = _allEntries.Where(ShouldShow).ToList();
            _view.SetRowCount(_visibleEntries.Count);
            _view.RefreshRows();
            ScrollToEndIfNeeded();
        }

        private bool ShouldShow(LogEventArgs entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (entry.Level == LogLevel.Debug && !_view.ShowDebugMessages)
            {
                return false;
            }

            return true;
        }

        private void ScrollToEndIfNeeded()
        {
            if (_view.AutoScrollEnabled && _visibleEntries.Count > 0)
            {
                _view.EnsureRowVisible(_visibleEntries.Count - 1);
            }
        }

        private void Clear()
        {
            _allEntries.Clear();
            _visibleEntries.Clear();
            _logHub.ClearBuffer();

            _view.SetRowCount(0);
            _view.RefreshRows();

        }

        private void CopyAll()
        {
            try
            {
                var text = string.Join(Environment.NewLine, _visibleEntries.Select(e => e.FormattedLine).ToArray());
                Clipboard.SetText(text ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, "Failed to copy log to clipboard.", ex);
            }
        }

        private void SaveAs()
        {
            try
            {
                var path = _view.PromptSaveAsPath(AppDomain.CurrentDomain.BaseDirectory, BuildDefaultLogFileName());
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                File.WriteAllLines(path, _visibleEntries.Select(e => e.FormattedLine).ToArray());
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warn, "Failed to save log export.", ex);
            }
        }

        private static string BuildDefaultLogFileName()
        {
            return "FlawlessPictury_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        }

        private LogEventArgs GetRowAt(int index)
        {
            if (index < 0 || index >= _visibleEntries.Count)
            {
                return null;
            }

            return _visibleEntries[index];
        }

        private void EnforceCap()
        {
            if (_allEntries.Count <= _maxUiLines)
            {
                return;
            }

            var removeCount = _allEntries.Count - _maxUiLines;
            if (removeCount > 0)
            {
                _allEntries.RemoveRange(0, removeCount);
            }
        }
    }
}
