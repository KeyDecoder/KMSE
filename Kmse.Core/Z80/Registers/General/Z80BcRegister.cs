using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80BcRegister : Z8016BitGeneralPurposeRegisterBase, IZ80BcRegister
{
    public Z80BcRegister(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags)
    {
        B = new Z808BitGeneralPurposeRegister(memory, flags);
        C = new Z808BitGeneralPurposeRegister(memory, flags);
    }

    protected override IZ808BitRegister HighRegister => B;
    protected override IZ808BitRegister LowRegister => C;
    public IZ808BitGeneralPurposeRegister B { get; }
    public IZ808BitGeneralPurposeRegister C { get; }
}