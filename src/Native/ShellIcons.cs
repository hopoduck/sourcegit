using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SourceGit.Native
{
    /// <summary>
    ///     Associated-program icons for files, as Windows Explorer shows them.
    /// </summary>
    public static class ShellIcons
    {
        public static Bitmap Get(string path)
        {
            if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(path))
                return null;

            // Files without an extension (Makefile, LICENSE, ...) share the generic icon.
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (s_cache.TryGetValue(ext, out var cached))
                return cached;

            Bitmap icon = null;
            try
            {
                icon = Load(string.IsNullOrEmpty(ext) ? "file" : $"file{ext}");
            }
            catch
            {
                // Fall back to the built-in icon.
            }

            s_cache[ext] = icon;
            return icon;
        }

        [SupportedOSPlatform("windows")]
        private static Bitmap Load(string fileName)
        {
            var info = new SHFILEINFO();
            var ret = SHGetFileInfo(fileName, FILE_ATTRIBUTE_NORMAL, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);
            if (ret == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                return null;

            try
            {
                if (!GetIconInfo(info.hIcon, out var iconInfo))
                    return null;

                try
                {
                    return iconInfo.hbmColor == IntPtr.Zero ? null : ToBitmap(iconInfo.hbmColor, iconInfo.hbmMask);
                }
                finally
                {
                    if (iconInfo.hbmColor != IntPtr.Zero)
                        DeleteObject(iconInfo.hbmColor);
                    if (iconInfo.hbmMask != IntPtr.Zero)
                        DeleteObject(iconInfo.hbmMask);
                }
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        }

        [SupportedOSPlatform("windows")]
        private static Bitmap ToBitmap(IntPtr hbmColor, IntPtr hbmMask)
        {
            if (GetObject(hbmColor, Marshal.SizeOf<BITMAP>(), out var bm) == 0 || bm.bmWidth <= 0 || bm.bmHeight <= 0)
                return null;

            var width = bm.bmWidth;
            var height = bm.bmHeight;
            var pixels = ReadPixels(hbmColor, width, height);
            if (pixels == null)
                return null;

            // Legacy icons carry no alpha channel; derive transparency from the AND mask instead.
            var hasAlpha = false;
            for (var i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0)
                {
                    hasAlpha = true;
                    break;
                }
            }

            if (!hasAlpha)
            {
                var mask = hbmMask != IntPtr.Zero ? ReadPixels(hbmMask, width, height) : null;
                for (var i = 3; i < pixels.Length; i += 4)
                    pixels[i] = (byte)(mask != null && mask[i - 1] != 0 ? 0 : 255);
            }

            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using (var fb = bitmap.Lock())
            {
                for (var y = 0; y < height; y++)
                    Marshal.Copy(pixels, y * width * 4, fb.Address + y * fb.RowBytes, width * 4);
            }

            return bitmap;
        }

        [SupportedOSPlatform("windows")]
        private static byte[] ReadPixels(IntPtr hbm, int width, int height)
        {
            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // top-down
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
            };

            var pixels = new byte[width * height * 4];
            var hdc = GetDC(IntPtr.Zero);
            try
            {
                var lines = GetDIBits(hdc, hbm, 0, (uint)height, pixels, ref header, DIB_RGB_COLORS);
                return lines == height ? pixels : null;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out BITMAP lpvObject);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFOHEADER lpbi, uint uUsage);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static readonly Dictionary<string, Bitmap> s_cache = new();
    }
}
