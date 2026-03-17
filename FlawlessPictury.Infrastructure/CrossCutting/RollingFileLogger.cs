using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using FlawlessPictury.AppCore.CrossCutting;

namespace FlawlessPictury.Infrastructure.CrossCutting
{
    /// <summary>
    /// Rolling file logger with size-based rotation.
    /// </summary>
    public sealed class RollingFileLogger : ILogger, IDisposable
    {
        private readonly string _directory;
        private readonly string _baseFileName; // e.g. "FlawlessPictury.log"
        private readonly long _maxFileBytes;
        private readonly int _maxFiles;

        private readonly BlockingCollection<LogWriteRequest> _queue;
        private readonly Thread _worker;
        private volatile bool _disposed;

        public RollingFileLogger(string directory, string baseFileName, long maxFileBytes, int maxFiles)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory is required.", nameof(directory));
            }

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                throw new ArgumentException("Base file name is required.", nameof(baseFileName));
            }

            _directory = directory;
            _baseFileName = baseFileName;
            _maxFileBytes = Math.Max(256 * 1024, maxFileBytes); // minimum 256KB
            _maxFiles = Math.Max(1, maxFiles);

            _queue = new BlockingCollection<LogWriteRequest>(boundedCapacity: 10_000);

            _worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "FlawlessPictury.RollingFileLogger"
            };
            _worker.Start();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _queue.CompleteAdding();
            }
            catch
            {
            }

            try
            {
                if (!_worker.Join(2500))
                {
                    // Best effort; do not hang app shutdown.
                }
            }
            catch
            {
            }

            try
            {
                _queue.Dispose();
            }
            catch
            {
            }
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (_disposed)
            {
                return;
            }

            var ts = DateTime.Now;
            var effectiveMessage = message ?? string.Empty;
            var prefix = ProcessingLogScope.CurrentPrefix;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                effectiveMessage = prefix + effectiveMessage;
            }

            // English-only diagnostic format (stable for debugging).
            var line = exception == null
                ? $"[{ts:yyyy-MM-dd HH:mm:ss.fff}] {level}: {effectiveMessage}"
                : $"[{ts:yyyy-MM-dd HH:mm:ss.fff}] {level}: {effectiveMessage} | {exception.GetType().Name}: {exception.Message}";

            // Include full exception details on next line for file logs.
            var extra = exception == null ? null : exception.ToString();

            var req = new LogWriteRequest(line, extra);

            try
            {
                // If the queue is full, we drop the log rather than blocking UI / pipeline.
                _queue.TryAdd(req);
            }
            catch
            {
                // Swallow logging failures.
            }
        }

        private void WorkerLoop()
        {
            FileStream stream = null;
            StreamWriter writer = null;

            try
            {
                Directory.CreateDirectory(_directory);

                foreach (var req in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        EnsureWriter(ref stream, ref writer);

                        writer.WriteLine(req.Line);

                        if (!string.IsNullOrWhiteSpace(req.Extra))
                        {
                            writer.WriteLine(req.Extra);
                        }

                        writer.Flush();

                        // Rotate if needed
                        if (stream.Length >= _maxFileBytes)
                        {
                            SafeClose(ref writer, ref stream);
                            RotateFiles();
                        }
                    }
                    catch
                    {
                        // If writing fails (e.g., permission, disk full), keep running without crashing.
                        SafeClose(ref writer, ref stream);
                        Thread.Sleep(250);
                    }
                }
            }
            catch
            {
                // Worker should never throw out.
            }
            finally
            {
                SafeClose(ref writer, ref stream);
            }
        }

        private void EnsureWriter(ref FileStream stream, ref StreamWriter writer)
        {
            if (writer != null && stream != null)
            {
                return;
            }

            var path = Path.Combine(_directory, _baseFileName);

            stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = false };
        }

        private void RotateFiles()
        {
            var basePath = Path.Combine(_directory, _baseFileName);

            // Delete oldest
            var oldest = GetRotatedPath(basePath, _maxFiles);
            TryDeleteFile(oldest);

            // Shift down from N-1 to N, etc.
            for (int i = _maxFiles - 1; i >= 1; i--)
            {
                var src = GetRotatedPath(basePath, i);
                var dst = GetRotatedPath(basePath, i + 1);
                TryMoveFile(src, dst);
            }

            // Move current to .1
            TryMoveFile(basePath, GetRotatedPath(basePath, 1));
        
            TryAppendRotationNotice(basePath);
        }

        
private void TryAppendRotationNotice(string basePath)
{
    try
    {
        var ts = DateTime.Now;
        var line = $"[{ts:yyyy-MM-dd HH:mm:ss.fff}] {LogLevel.Debug}: Log rotated. Current log: {basePath}";
        File.AppendAllText(basePath, line + Environment.NewLine, Encoding.UTF8);
    }
    catch
    {
        // Best effort only.
    }
}

private static string GetRotatedPath(string basePath, int index)
        {
            // "FlawlessPictury.log" -> "FlawlessPictury.1.log"
            var dir = Path.GetDirectoryName(basePath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(basePath);
            var ext = Path.GetExtension(basePath); // includes '.'

            return Path.Combine(dir, name + "." + index + ext);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static void TryMoveFile(string src, string dst)
        {
            try
            {
                if (!File.Exists(src))
                {
                    return;
                }

                // If destination exists, delete it first (best effort).
                TryDeleteFile(dst);

                File.Move(src, dst);
            }
            catch
            {
            }
        }

        private static void SafeClose(ref StreamWriter writer, ref FileStream stream)
        {
            try { if (writer != null) writer.Dispose(); } catch { }
            try { if (stream != null) stream.Dispose(); } catch { }

            writer = null;
            stream = null;
        }

        private sealed class LogWriteRequest
        {
            public LogWriteRequest(string line, string extra)
            {
                Line = line ?? string.Empty;
                Extra = extra;
            }

            public string Line { get; }
            public string Extra { get; }
        }
    }
}
