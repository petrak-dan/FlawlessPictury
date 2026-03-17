using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FlawlessPictury.AppCore.Stats;

namespace FlawlessPictury.Infrastructure.Stats
{
    /// <summary>
    /// Writes one compact summary row per processed file for later analysis.
    /// </summary>
    public sealed class CsvStatsSink : IRunStatsSink, IDisposable
    {
        private readonly object _sync = new object();
        private readonly string _filePath;
        private readonly Dictionary<string, FileSummary> _summaries = new Dictionary<string, FileSummary>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _delimiter;
        private readonly bool _writeExcelSeparatorHint;
        private bool _headerWritten;

        public CsvStatsSink(string directoryPath)
            : this(directoryPath, ';', true)
        {
        }

        public CsvStatsSink(string directoryPath, char delimiter, bool writeExcelSeparatorHint)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path is required.", nameof(directoryPath));
            }

            _delimiter = delimiter.ToString();
            _writeExcelSeparatorHint = writeExcelSeparatorHint;

            Directory.CreateDirectory(directoryPath);
            _filePath = Path.Combine(directoryPath, "stats_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public void Emit(StatsEvent statsEvent)
        {
            if (statsEvent == null)
            {
                return;
            }

            lock (_sync)
            {
                EnsureHeader();

                if (!IsFileScoped(statsEvent))
                {
                    return;
                }

                var key = BuildSummaryKey(statsEvent);
                FileSummary summary;
                if (!_summaries.TryGetValue(key, out summary))
                {
                    summary = new FileSummary();
                    _summaries[key] = summary;
                }

                ApplyEvent(summary, statsEvent);

                if (statsEvent.Kind == StatsEventKind.FileCompleted || statsEvent.Kind == StatsEventKind.FileFailed)
                {
                    if (_writtenKeys.Add(key))
                    {
                        File.AppendAllText(_filePath, BuildRow(summary), Encoding.UTF8);
                    }

                    _summaries.Remove(key);
                }
            }
        }

        private void EnsureHeader()
        {
            if (_headerWritten)
            {
                return;
            }

            if (_writeExcelSeparatorHint)
            {
                File.AppendAllText(_filePath, "sep=" + _delimiter + Environment.NewLine, Encoding.UTF8);
            }

            var header = string.Join(_delimiter, new[]
            {
                "RunId",
                "PresetId",
                "InputFileName",
                "OutputFileName",
                "Status",
                "UsedOriginalFallback",
                "WarningCount",
                "InputBytes",
                "OutputBytes",
                "BytesSaved",
                "TryCount",
                "WinningParameterKey",
                "WinningParameterValue",
                "StartedAt",
                "EndedAt",
                "DurationMs"
            }.Select(EscapeValue));

            File.AppendAllText(_filePath, header + Environment.NewLine, Encoding.UTF8);
            _headerWritten = true;
        }

        private static bool IsFileScoped(StatsEvent statsEvent)
        {
            return !string.IsNullOrWhiteSpace(statsEvent.FileId)
                || !string.IsNullOrWhiteSpace(statsEvent.FilePath)
                || statsEvent.Kind == StatsEventKind.FileStarted
                || statsEvent.Kind == StatsEventKind.FileCompleted
                || statsEvent.Kind == StatsEventKind.FileFailed
                || statsEvent.Kind == StatsEventKind.StepStarted
                || statsEvent.Kind == StatsEventKind.StepCompleted
                || statsEvent.Kind == StatsEventKind.CandidateEvaluated;
        }

        private static string BuildSummaryKey(StatsEvent statsEvent)
        {
            return (statsEvent.RunId ?? string.Empty) + "|" +
                   (statsEvent.FileId ?? string.Empty) + "|" +
                   (statsEvent.FilePath ?? string.Empty);
        }

        private static void ApplyEvent(FileSummary summary, StatsEvent statsEvent)
        {
            if (!string.IsNullOrWhiteSpace(statsEvent.RunId))
            {
                summary.RunId = statsEvent.RunId;
            }

            if (!string.IsNullOrWhiteSpace(statsEvent.PresetId))
            {
                summary.PresetId = statsEvent.PresetId;
            }

            if (!string.IsNullOrWhiteSpace(statsEvent.FilePath))
            {
                summary.InputFileName = SafeGetFileName(statsEvent.FilePath);
            }

            var inputFileName = GetDataValue(statsEvent, "inputFileName");
            if (!string.IsNullOrWhiteSpace(inputFileName))
            {
                summary.InputFileName = inputFileName;
            }

            var outputFileName = GetDataValue(statsEvent, "outputFileName", "finalOutputFileName");
            if (!string.IsNullOrWhiteSpace(outputFileName))
            {
                summary.OutputFileName = outputFileName;
            }

            var status = GetDataValue(statsEvent, "status");
            if (!string.IsNullOrWhiteSpace(status))
            {
                summary.Status = status;
            }

            var fallbackText = GetDataValue(statsEvent, "usedOriginalFallback", "usedInputFallback", "fallbackToInput");
            bool fallbackValue;
            if (TryParseBoolean(fallbackText, out fallbackValue))
            {
                summary.UsedOriginalFallback = fallbackValue;
            }

            int warningCount;
            if (TryParseInt(GetDataValue(statsEvent, "warningCount"), out warningCount))
            {
                summary.WarningCount = warningCount;
            }

            long inputBytes;
            if (TryParseLong(GetDataValue(statsEvent, "inputBytes", "originalBytes"), out inputBytes))
            {
                summary.InputBytes = inputBytes;
            }

            long outputBytes;
            if (TryParseLong(GetDataValue(statsEvent, "outputBytes"), out outputBytes))
            {
                summary.OutputBytes = outputBytes;
            }

            long bytesSaved;
            if (TryParseLong(GetDataValue(statsEvent, "bytesSaved", "savedBytes"), out bytesSaved))
            {
                summary.BytesSaved = bytesSaved;
            }
            else if (summary.InputBytes.HasValue && summary.OutputBytes.HasValue)
            {
                summary.BytesSaved = Math.Max(0L, summary.InputBytes.Value - summary.OutputBytes.Value);
            }

            int tryCount;
            if (TryParseInt(GetDataValue(statsEvent, "tryCount"), out tryCount))
            {
                summary.TryCount = Math.Max(summary.TryCount ?? 0, tryCount);
            }

            var winningParameterKey = GetDataValue(statsEvent, "winningParameterKey", "chosenParameterKey");
            if (!string.IsNullOrWhiteSpace(winningParameterKey))
            {
                summary.WinningParameterKey = winningParameterKey;
            }

            var winningParameterValue = GetDataValue(statsEvent, "winningParameterValue", "chosenParameter");
            if (!string.IsNullOrWhiteSpace(winningParameterValue))
            {
                summary.WinningParameterValue = winningParameterValue;
            }

            if (statsEvent.Kind == StatsEventKind.CandidateEvaluated)
            {
                summary.CandidateEventCount++;

                int tryIndex;
                if (TryParseInt(GetDataValue(statsEvent, "tryIndex"), out tryIndex))
                {
                    summary.TryCount = Math.Max(summary.TryCount ?? 0, tryIndex);
                }
                else
                {
                    summary.TryCount = Math.Max(summary.TryCount ?? 0, summary.CandidateEventCount);
                }
            }


            int durationMs;
            if (TryParseInt(GetDataValue(statsEvent, "durationMs"), out durationMs))
            {
                summary.DurationMs = durationMs;
            }

            if (statsEvent.Kind == StatsEventKind.FileStarted)
            {
                summary.StartedAt = statsEvent.TimestampUtc;
                if (string.IsNullOrWhiteSpace(summary.Status))
                {
                    summary.Status = "Running";
                }
            }
            else if (statsEvent.Kind == StatsEventKind.FileCompleted)
            {
                summary.EndedAt = statsEvent.TimestampUtc;
                if (string.IsNullOrWhiteSpace(summary.Status) || string.Equals(summary.Status, "Running", StringComparison.OrdinalIgnoreCase))
                {
                    summary.Status = summary.OutputBytes.HasValue && summary.OutputBytes.Value == 0 ? "NoOutput" : "Completed";
                }
            }
            else if (statsEvent.Kind == StatsEventKind.FileFailed)
            {
                summary.EndedAt = statsEvent.TimestampUtc;
                summary.Status = "Failed";
            }

            if ((!summary.DurationMs.HasValue || summary.DurationMs.Value < 0) && summary.StartedAt.HasValue && summary.EndedAt.HasValue)
            {
                summary.DurationMs = Math.Max(0, (int)(summary.EndedAt.Value - summary.StartedAt.Value).TotalMilliseconds);
            }
        }

        private static string GetDataValue(StatsEvent statsEvent, params string[] keys)
        {
            if (statsEvent == null || statsEvent.Data == null || keys == null)
            {
                return null;
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string value;
                if (statsEvent.Data.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private string BuildRow(FileSummary summary)
        {
            var values = new[]
            {
                summary.RunId,
                summary.PresetId,
                summary.InputFileName,
                summary.OutputFileName,
                summary.Status,
                summary.UsedOriginalFallback.HasValue ? (summary.UsedOriginalFallback.Value ? "true" : "false") : string.Empty,
                summary.WarningCount.HasValue ? summary.WarningCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                summary.InputBytes.HasValue ? summary.InputBytes.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                summary.OutputBytes.HasValue ? summary.OutputBytes.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                summary.BytesSaved.HasValue ? summary.BytesSaved.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                summary.TryCount.HasValue ? summary.TryCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                summary.WinningParameterKey,
                summary.WinningParameterValue,
                FormatExcelFriendlyDateTime(summary.StartedAt),
                FormatExcelFriendlyDateTime(summary.EndedAt),
                summary.DurationMs.HasValue ? summary.DurationMs.Value.ToString(CultureInfo.InvariantCulture) : string.Empty
            };

            return string.Join(_delimiter, values.Select(EscapeValue)) + Environment.NewLine;
        }

        private static bool TryParseBoolean(string text, out bool value)
        {
            if (bool.TryParse(text, out value))
            {
                return true;
            }

            if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            value = false;
            return false;
        }

        private static bool TryParseLong(string text, out long value)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseInt(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string SafeGetFileName(string path)
        {
            try
            {
                return Path.GetFileName(path) ?? string.Empty;
            }
            catch
            {
                return path ?? string.Empty;
            }
        }

        private static string FormatExcelFriendlyDateTime(DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            return value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private string EscapeValue(string value)
        {
            value = value ?? string.Empty;

            var mustQuote = value.IndexOf('"') >= 0
                || value.IndexOf('\r') >= 0
                || value.IndexOf('\n') >= 0
                || value.Contains(_delimiter)
                || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])));

            if (!mustQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public void Dispose()
        {
        }

        private sealed class FileSummary
        {
            public string RunId;
            public string PresetId;
            public string InputFileName;
            public string OutputFileName;
            public string Status;
            public bool? UsedOriginalFallback;
            public int? WarningCount;
            public long? InputBytes;
            public long? OutputBytes;
            public long? BytesSaved;
            public int? TryCount;
            public string WinningParameterKey;
            public string WinningParameterValue;
            public DateTime? StartedAt;
            public DateTime? EndedAt;
            public int? DurationMs;
            public int CandidateEventCount;
        }
    }
}
