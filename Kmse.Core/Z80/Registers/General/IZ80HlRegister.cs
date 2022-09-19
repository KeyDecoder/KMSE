namespace Kmse.Core.Z80.Registers.General;

public interface IZ80HlRegister : IZ8016BitGeneralPurposeRegister
{
    IZ808BitGeneralPurposeRegister H { get; }
    IZ808BitGeneralPurposeRegister L { get; }

    void SwapWithDeRegister(IZ80DeRegister register);
}