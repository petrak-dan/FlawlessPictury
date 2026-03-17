using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FlawlessPictury.Infrastructure.CrossCutting
{
    /// <summary>
    /// Resolves the executable path and directory in a robust way.
    ///
    /// Goals:
    /// - Avoid relying on Environment.CurrentDirectory (can change).
    /// - In portable mode, resolve Logs/Presets/Plugins relative to the running EXE.
    /// - Work with UNC paths (System.IO.Path supports UNC).
    /// </summary>
    public static class ExecutablePathResolver
    {
        /// <summary>
        /// Returns the best-effort absolute path to the running executable.
        /// </summary>
        public static string GetExecutablePath()
        {
            try
            {
                var entry = Assembly.GetEntryAssembly();
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Location))
                {
                    return entry.Location;
                }
            }
            catch
            {
            }

            try
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
            catch
            {
            }

            // Last resort: may be a directory path.
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Returns the best-effort absolute directory containing the running executable.
        /// </summary>
        public static string GetExecutableDirectory()
        {
            var path = GetExecutablePath();

            if (string.IsNullOrWhiteSpace(path))
            {
                return NormalizeDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }

            try
            {
                if (Directory.Exists(path))
                {
                    return NormalizeDirectory(path);
                }
            }
            catch
            {
            }

            try
            {
                return NormalizeDirectory(Path.GetDirectoryName(path));
            }
            catch
            {
                return NormalizeDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
        }

        private static string NormalizeDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(dir);
            }
            catch
            {
                return dir;
            }
        }
    }
}
