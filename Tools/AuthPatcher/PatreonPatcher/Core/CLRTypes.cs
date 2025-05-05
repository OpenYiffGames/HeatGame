using System.Runtime.InteropServices;

namespace PatreonPatcher.Core;

[StructLayout(LayoutKind.Explicit)]
internal struct IMAGE_COR_ILMETHOD
{
    [FieldOffset(0)] public byte Tiny_Flags_CodeSize;

    [FieldOffset(0)] public uint Fat_BitField;
    [FieldOffset(4)] public uint Fat_CodeSize;
    [FieldOffset(8)] public uint Fat_LocalVarSigTok;
}
