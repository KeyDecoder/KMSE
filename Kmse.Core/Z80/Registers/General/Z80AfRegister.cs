using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80AfRegister : Z8016BitCombinedRegisterBase, IZ80AfRegister
{
    public Z80AfRegister(IMasterSystemMemory memory)
        : base(memory)
    {
        Flags = new Z80FlagsManager(memory);
        Accumulator = new Z80Accumulator(Flags, memory);
    }

    protected override IZ808BitRegister HighRegister => Accumulator;
    protected override IZ808BitRegister LowRegister => Flags;
    public IZ80Accumulator Accumulator { get; }
    public IZ80FlagsManager Flags { get; }
}