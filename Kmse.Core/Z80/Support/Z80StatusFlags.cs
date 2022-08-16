namespace Kmse.Core.Z80.Support;

[Flags]
public enum Z80StatusFlags : byte
{
    // Carry
    CarryC = 1 << 0,

    // Subtract 
    AddSubtractN = 1 << 1,

    // Parity or Overflow 
    ParityOverflowPV = 1 << 2,

    // Not Used
    NotUsedX3 = 1 << 3,

    // Half Carry 
    HalfCarryH = 1 << 4,

    // Not Used  
    NotUsedX5 = 1 << 5,

    // Zero 
    ZeroZ = 1 << 6,

    // Sign
    SignS = 1 << 7
}