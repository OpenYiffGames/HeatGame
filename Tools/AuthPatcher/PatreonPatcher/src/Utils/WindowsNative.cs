using System.Runtime.InteropServices;

static class WindowsNative
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern int MessageBoxA(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern bool GetOpenFileNameA(ref OPENFILENAMEA lpofn);

    [StructLayout(LayoutKind.Sequential)]
    public struct OPENFILENAMEA
    {
        public uint lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
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
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }
}
