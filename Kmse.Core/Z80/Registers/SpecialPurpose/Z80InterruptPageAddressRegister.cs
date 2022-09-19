using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80InterruptPageAddressRegister : Z808BitRegister, IZ80InterruptPageAddressRegister
{
    public Z80InterruptPageAddressRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}