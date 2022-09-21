namespace Kmse.Core.Z80.Model;

public class CpuStatus
{
    public int CurrentCycleCount { get; init; }
    public bool Halted { get; init; }

    public Unsigned16BitValue Af { get; init; }
    public Unsigned16BitValue Bc { get; init; }
    public Unsigned16BitValue De { get; init; }
    public Unsigned16BitValue Hl { get; init; }
    public Unsigned16BitValue AfShadow { get; init; }
    public Unsigned16BitValue BcShadow { get; init; }
    public Unsigned16BitValue DeShadow { get; init; }
    public Unsigned16BitValue HlShadow { get; init; }
    public Unsigned16BitValue Ix { get; init; }
    public Unsigned16BitValue Iy { get; init; }
    public ushort Pc { get; init; }
    public ushort StackPointer { get; init; }
    public byte IRegister { get; init; }
    public byte RRegister { get; init; }
    public bool InterruptFlipFlop1 { get; init; }

    public bool InterruptFlipFlop2 { get; init; }
    public byte InterruptMode { get; init; }

    public bool NonMaskableInterruptStatus { get; init; }
    public bool MaskableInterruptStatus { get; init; }

}