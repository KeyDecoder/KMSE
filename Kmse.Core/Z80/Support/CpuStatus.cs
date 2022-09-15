namespace Kmse.Core.Z80.Support;

public class CpuStatus
{
    public int CurrentCycleCount { get; init; }
    public bool Halted { get; init; }

    public Z80Register Af { get; init; }
    public Z80Register Bc { get; init; }
    public Z80Register De { get; init; }
    public Z80Register Hl { get; init; }
    public Z80Register AfShadow { get; init; }
    public Z80Register BcShadow { get; init; }
    public Z80Register DeShadow { get; init; }
    public Z80Register HlShadow { get; init; }
    public Z80Register Ix { get; init; }
    public Z80Register Iy { get; init; }
    public ushort Pc { get; init; }
    public Z80Register StackPointer { get; init; }
    public byte IRegister { get; init; }
    public byte RRegister { get; init; }
    public bool InterruptFlipFlop1 { get; init; }

    public bool InterruptFlipFlop2 { get; init; }
    public byte InterruptMode { get; init; }

    public bool NonMaskableInterruptStatus { get; init; }
    public bool MaskableInterruptStatus { get; init; }

}