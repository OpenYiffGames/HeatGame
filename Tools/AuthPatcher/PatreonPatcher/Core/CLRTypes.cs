using System.Runtime.InteropServices;

namespace PatreonPatcher.Core;

[StructLayout(LayoutKind.Explicit)]
internal struct IMAGE_COR_ILMETHOD
{
    [FieldOffset(0)] public byte TinyFatFormat;

    [FieldOffset(0)] public byte Tiny_FlagsAndCodeSize;

    [FieldOffset(0)] public ushort Fat_FlagsAndSize;
    [FieldOffset(2)] public ushort Fat_MaxStack;
    [FieldOffset(4)] public uint Fat_CodeSize;
    [FieldOffset(8)] public uint Fat_LocalVarSigTok;
}
