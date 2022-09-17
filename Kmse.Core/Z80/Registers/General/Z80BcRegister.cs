using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80BcRegister : Z8016BitCombinedRegisterBase, IZ80BcRegister
{
    public Z80BcRegister(IMasterSystemMemory memory)
        : base(memory)
    {
        B = new Z808BitRegister(memory);
        C = new Z808BitRegister(memory);
    }

    protected override IZ808BitRegister HighRegister => B;
    protected override IZ808BitRegister LowRegister => C;
    public IZ808BitRegister B { get; }
    public IZ808BitRegister C { get; }
}