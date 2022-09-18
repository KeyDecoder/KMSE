namespace Kmse.Core.Z80.Registers.General;

public interface IZ80HlRegister : IZ8016BitCombinedRegister
{
    IZ808BitRegister H { get; }
    IZ808BitRegister L { get; }

    void SwapWithDeRegister(IZ80DeRegister register);
}