using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.Z80;

public class Z80CpuRegisters
{
    public IZ80ProgramCounter Pc { get; set; }
    public IZ80StackManager Stack { get; set; }
    public IZ80AfRegister Af { get; set; }
    public IZ80BcRegister Bc { get; set; }
    public IZ80DeRegister De { get; set; }
    public IZ80HlRegister Hl { get; set; }
    public IZ80IndexRegisterX IX { get; set; }
    public IZ80IndexRegisterY IY { get; set; }
    public IZ80MemoryRefreshRegister R { get; set; }
    public IZ80InterruptPageAddressRegister I { get; set; }
}