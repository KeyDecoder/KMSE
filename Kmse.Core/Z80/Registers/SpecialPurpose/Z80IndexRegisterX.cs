using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80IndexRegisterX : Z8016BitRegisterBase, IZ80IndexRegisterX
{
    public Z80IndexRegisterX(IMasterSystemMemory memory) : base(memory) { }
}