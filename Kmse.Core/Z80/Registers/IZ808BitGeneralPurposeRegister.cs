namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Interface for a Z80 8 bit general purpose register which can have operations on the value
///     This distinguishes is from the Flag register which cannot do various math operations on it
/// </summary>
public interface IZ808BitGeneralPurposeRegister : IZ808BitRegister
{
    void Increment();
    void Decrement();
}