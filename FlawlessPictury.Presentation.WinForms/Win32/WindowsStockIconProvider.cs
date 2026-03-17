using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FlawlessPictury.Presentation.WinForms.Win32
{
    /// <summary>
    /// Retrieves and caches Windows stock icons using SHGetStockIconInfo.
    /// </summary>
    public sealed class WindowsStockIconProvider : IDisposable
    {
        private readonly Dictionary<string, Icon> _cache = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a cached small stock icon.
        /// </summary>
        /// <param name="id">The stock icon identifier.</param>
        public Icon GetSmall(WindowsStockIconId id) => Get(id, small: true);

        /// <summary>
        /// Gets a cached large stock icon.
        /// </summary>
        /// <param name="id">The stock icon identifier.</param>
        public Icon GetLarge(WindowsStockIconId id) => Get(id, small: false);

        private Icon Get(WindowsStockIconId id, bool small)
        {
            var key = ((int)id).ToString() + (small ? ":s" : ":l");

            lock (_cache)
            {
                if (_cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            var flags = SHGSI.SHGSI_ICON | (small ? SHGSI.SHGSI_SMALLICON : SHGSI.SHGSI_LARGEICON);

            var info = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO)) };

            var hr = SHGetStockIconInfo((int)id, (uint)flags, ref info);
            if (hr != 0 || info.hIcon == IntPtr.Zero)
            {
                return null;
            }

            Icon managed = null;
            try
            {
                using (var tmp = Icon.FromHandle(info.hIcon))
                {
                    managed = (Icon)tmp.Clone();
                }
            }
            finally
            {
                try { DestroyIcon(info.hIcon); } catch { }
            }

            lock (_cache)
            {
                _cache[key] = managed;
            }

            return managed;
        }

        /// <summary>
        /// Releases cached icon instances.
        /// </summary>
        public void Dispose()
        {
            lock (_cache)
            {
                foreach (var kv in _cache)
                {
                    try { kv.Value.Dispose(); } catch { }
                }
                _cache.Clear();
            }
        }

        /// <summary>
        /// Minimal subset of <c>SHSTOCKICONID</c> values used by the application.
        /// </summary>
        public enum WindowsStockIconId
        {
            DocNoAssoc = 0,
            Application = 2,
            Folder = 3,
            FolderOpen = 4,
            Find = 22,
            Recycler = 31,
            ImageFiles = 72,
            Software = 82,
            Delete = 84,
            Warning = 78,
            Info = 79,
            Error = 80
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            public int iIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [Flags]
        private enum SHGSI : uint
        {
            SHGSI_ICON = 0x000000100,
            SHGSI_SMALLICON = 0x000000001,
            SHGSI_LARGEICON = 0x000000000
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetStockIconInfo(int siid, uint uFlags, ref SHSTOCKICONINFO psii);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
