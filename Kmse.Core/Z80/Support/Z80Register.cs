using System.Runtime.InteropServices;

namespace Kmse.Core.Z80.Support;

[StructLayout(LayoutKind.Explicit)]
public struct Z80Register
{
    [FieldOffset(0)] public byte Low;
    [FieldOffset(1)] public byte High;
    [FieldOffset(0)] public ushort Word;
}