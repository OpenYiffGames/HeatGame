using System.Runtime.InteropServices;

namespace PatreonPatcher.Core.Helpers;

internal static class WindowsNative
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern int MessageBoxA(nint hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern bool GetOpenFileNameA(ref OPENFILENAMEA lpofn);

    [StructLayout(LayoutKind.Sequential)]
    public struct OPENFILENAMEA
    {
        public uint lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public uint nMaxCustFilter;
        public uint nFilterIndex;
        public string lpstrFile;
        public uint nMaxFile;
        public string lpstrFileTitle;
        public uint nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public uint Flags;
        public ushort nFileOffset;
        public ushort nFileExtension;
        public string lpstrDefExt;
        public nint lCustData;
        public nint lpfnHook;
        public string lpTemplateName;
        public nint pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }
}
