using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80MemoryRefreshRegister : Z808BitRegister, IZ80MemoryRefreshRegister
{
    public Z80MemoryRefreshRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}