using System.Runtime.InteropServices;

namespace Kmse.Core.Z80.Model;

[StructLayout(LayoutKind.Explicit)]
public struct Unsigned16BitValue
{
    [FieldOffset(0)] public byte Low;
    [FieldOffset(1)] public byte High;
    [FieldOffset(0)] public ushort Word;
}