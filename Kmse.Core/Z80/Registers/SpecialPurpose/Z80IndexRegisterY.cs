using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80IndexRegisterY : Z80IndexRegisterXy, IZ80IndexRegisterY
{
    public Z80IndexRegisterY(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}