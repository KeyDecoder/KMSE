using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers.General;

public class Z80HlRegister : Z8016BitCombinedRegisterBase, IZ80HlRegister
{
    public Z80HlRegister(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory)
    {
        H = new Z808BitGeneralPurposeRegister(memory, flags);
        L = new Z808BitGeneralPurposeRegister(memory, flags);
    }

    protected override IZ808BitRegister HighRegister => H;
    protected override IZ808BitRegister LowRegister => L;
    public IZ808BitRegister H { get; }
    public IZ808BitRegister L { get; }

    public void SwapWithDeRegister(IZ80DeRegister register)
    {
        var otherRegisterValue = register.Value;
        var thisRegisterValue = Value;
        register.Set(thisRegisterValue);
        Set(otherRegisterValue);
    }
}