using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80DeRegister : Z8016BitGeneralPurposeRegisterBase, IZ80DeRegister
{
    public Z80DeRegister(IMasterSystemMemory memory, IZ80FlagsManager flags,
        Func<IZ808BitGeneralPurposeRegister> registerFactory)
        : base(memory, flags)
    {
        D = registerFactory();
        E = registerFactory();
    }

    protected override IZ808BitRegister HighRegister => D;
    protected override IZ808BitRegister LowRegister => E;
    public IZ808BitGeneralPurposeRegister D { get; }
    public IZ808BitGeneralPurposeRegister E { get; }
}