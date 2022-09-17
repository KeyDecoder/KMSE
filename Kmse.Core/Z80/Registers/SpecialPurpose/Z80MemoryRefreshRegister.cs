using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

// TODO: This probably should be a special base class since this the memory refresh register has alot of restrictions on it
public class Z80MemoryRefreshRegister : Z808BitRegister, IZ80MemoryRefreshRegister
{
    public Z80MemoryRefreshRegister(IMasterSystemMemory memory) : base(memory) { }
}