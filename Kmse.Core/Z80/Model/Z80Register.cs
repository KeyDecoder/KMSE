using System.Runtime.InteropServices;

namespace Kmse.Core.Z80.Model;

// TODO: Rename to Unsigned16BitValue
[StructLayout(LayoutKind.Explicit)]
public struct Z80Register
{
    [FieldOffset(0)] public byte Low;
    [FieldOffset(1)] public byte High;
    [FieldOffset(0)] public ushort Word;
}