using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80HlRegister : Z8016BitCombinedRegisterBase, IZ80HlRegister
{
    public Z80HlRegister(IMasterSystemMemory memory)
        : base(memory)
    {
        H = new Z808BitRegister(memory);
        L = new Z808BitRegister(memory);
    }

    protected override IZ808BitRegister HighRegister => H;
    protected override IZ808BitRegister LowRegister => L;
    public IZ808BitRegister H { get; }
    public IZ808BitRegister L { get; }
}