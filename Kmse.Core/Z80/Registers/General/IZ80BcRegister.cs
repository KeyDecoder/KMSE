namespace Kmse.Core.Z80.Registers.General;

public interface IZ80BcRegister : IZ8016BitCombinedRegister
{
    IZ808BitRegister B { get; }
    IZ808BitRegister C { get; }
}