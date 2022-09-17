using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80IndexRegisterY : Z8016BitRegisterBase, IZ80IndexRegisterY
{
    public Z80IndexRegisterY(IMasterSystemMemory memory) : base(memory) { }
}