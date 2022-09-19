namespace Kmse.Core.Z80.Registers.General;

public interface IZ80BcRegister : IZ8016BitGeneralPurposeRegister
{
    IZ808BitGeneralPurposeRegister B { get; }
    IZ808BitGeneralPurposeRegister C { get; }
}