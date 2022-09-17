using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80InterruptPageAddressRegister : Z808BitRegister, IZ80InterruptPageAddressRegister
{
    public Z80InterruptPageAddressRegister(IMasterSystemMemory memory) : base(memory) { }
}