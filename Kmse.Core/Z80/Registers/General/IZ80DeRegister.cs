namespace Kmse.Core.Z80.Registers.General;

public interface IZ80DeRegister : IZ8016BitGeneralPurposeRegister
{
    IZ808BitGeneralPurposeRegister D { get; }
    IZ808BitGeneralPurposeRegister E { get; }
}