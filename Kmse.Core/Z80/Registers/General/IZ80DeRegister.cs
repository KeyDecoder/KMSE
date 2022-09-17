namespace Kmse.Core.Z80.Registers.General;

public interface IZ80DeRegister : IZ8016BitCombinedRegister
{
    IZ808BitRegister D { get; }
    IZ808BitRegister E { get; }
}