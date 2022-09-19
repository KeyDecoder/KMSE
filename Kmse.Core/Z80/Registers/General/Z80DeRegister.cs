using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80DeRegister : Z8016BitGeneralPurposeRegisterBase, IZ80DeRegister
{
    public Z80DeRegister(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags)
    {
        D = new Z808BitGeneralPurposeRegister(memory, flags);
        E = new Z808BitGeneralPurposeRegister(memory, flags);
    }

    protected override IZ808BitRegister HighRegister => D;
    protected override IZ808BitRegister LowRegister => E;
    public IZ808BitGeneralPurposeRegister D { get; }
    public IZ808BitGeneralPurposeRegister E { get; }
}