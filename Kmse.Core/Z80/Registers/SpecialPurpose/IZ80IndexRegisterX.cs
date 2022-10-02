namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public interface IZ80IndexRegisterX : IZ8016BitSpecialRegister
{
    void IncrementHigh();
    void IncrementLow();
    void DecrementHigh();
    void DecrementLow();
}