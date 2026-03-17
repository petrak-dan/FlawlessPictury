using System;
using System.Globalization;
using System.Threading;

namespace FlawlessPictury.AppCore.CrossCutting
{
    /// <summary>
    /// Ambient processing scope used to annotate log messages related to a file-processing slot.
    /// Compatibility surface intentionally includes both older slot-based members and newer prefix-based members.
    /// </summary>
    public static class ProcessingLogScope
    {
        private sealed class ScopeState
        {
            public int? SlotNumber;
            public int FileIndex;
            public int FileCount;
            public string FilePath;
            public string Prefix;
            public ScopeState Previous;
        }

        private sealed class ScopeCookie : IDisposable
        {
            private readonly ScopeState _previous;
            private bool _disposed;

            public ScopeCookie(ScopeState previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _current.Value = _previous;
            }
        }

        private static readonly AsyncLocal<ScopeState> _current = new AsyncLocal<ScopeState>();

        public static int? CurrentSlotNumber
        {
            get { return _current.Value == null ? (int?)null : _current.Value.SlotNumber; }
        }

        public static int CurrentFileIndex
        {
            get { return _current.Value == null ? 0 : _current.Value.FileIndex; }
        }

        public static int CurrentFileCount
        {
            get { return _current.Value == null ? 0 : _current.Value.FileCount; }
        }

        public static string CurrentFilePath
        {
            get { return _current.Value == null ? string.Empty : (_current.Value.FilePath ?? string.Empty); }
        }

        public static string CurrentPrefix
        {
            get
            {
                var state = _current.Value;
                if (state == null)
                {
                    return string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(state.Prefix))
                {
                    return state.Prefix;
                }

                if (state.SlotNumber.HasValue && state.SlotNumber.Value > 0)
                {
                    if (state.FileIndex > 0)
                    {
                        return "Thread " + state.SlotNumber.Value.ToString(CultureInfo.InvariantCulture) + ", File " + state.FileIndex.ToString(CultureInfo.InvariantCulture) + ": ";
                    }

                    return "Thread " + state.SlotNumber.Value.ToString(CultureInfo.InvariantCulture) + ": ";
                }

                return string.Empty;
            }
        }

        public static IDisposable Begin(int slotNumber)
        {
            return Push(slotNumber, 0, 0, string.Empty);
        }

        public static IDisposable Push(string prefix)
        {
            var previous = _current.Value;
            _current.Value = new ScopeState
            {
                SlotNumber = previous == null ? (int?)null : previous.SlotNumber,
                FileIndex = previous == null ? 0 : previous.FileIndex,
                FileCount = previous == null ? 0 : previous.FileCount,
                FilePath = previous == null ? string.Empty : previous.FilePath,
                Prefix = prefix ?? string.Empty,
                Previous = previous
            };

            return new ScopeCookie(previous);
        }

        public static IDisposable Push(int slotNumber, int fileIndex, int fileCount, string filePath)
        {
            var previous = _current.Value;
            var normalizedSlot = slotNumber > 0 ? (int?)slotNumber : null;
            string prefix;
            if (normalizedSlot.HasValue)
            {
                prefix = fileIndex > 0
                    ? "Thread " + normalizedSlot.Value.ToString(CultureInfo.InvariantCulture) + ", File " + fileIndex.ToString(CultureInfo.InvariantCulture) + ": "
                    : "Thread " + normalizedSlot.Value.ToString(CultureInfo.InvariantCulture) + ": ";
            }
            else
            {
                prefix = string.Empty;
            }

            _current.Value = new ScopeState
            {
                SlotNumber = normalizedSlot,
                FileIndex = fileIndex,
                FileCount = fileCount,
                FilePath = filePath ?? string.Empty,
                Prefix = prefix,
                Previous = previous
            };

            return new ScopeCookie(previous);
        }
    }
}
