using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80DeRegister : Z8016BitCombinedRegisterBase, IZ80DeRegister
{
    public Z80DeRegister(IMasterSystemMemory memory)
        : base(memory)
    {
        D = new Z808BitRegister(memory);
        E = new Z808BitRegister(memory);
    }

    protected override IZ808BitRegister HighRegister => D;
    protected override IZ808BitRegister LowRegister => E;
    public IZ808BitRegister D { get; }
    public IZ808BitRegister E { get; }
}