using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80HlRegister : Z8016BitGeneralPurposeRegisterBase, IZ80HlRegister
{
    public Z80HlRegister(IMasterSystemMemory memory, IZ80FlagsManager flags, Func<IZ808BitGeneralPurposeRegister> registerFactory)
        : base(memory, flags)
    {
        H = registerFactory();
        L = registerFactory();
    }

    protected override IZ808BitRegister HighRegister => H;
    protected override IZ808BitRegister LowRegister => L;
    public IZ808BitGeneralPurposeRegister H { get; }
    public IZ808BitGeneralPurposeRegister L { get; }

    public void SwapWithDeRegister(IZ80DeRegister register)
    {
        var otherRegisterValue = register.Value;
        var thisRegisterValue = Value;
        register.Set(thisRegisterValue);
        Set(otherRegisterValue);
    }
}