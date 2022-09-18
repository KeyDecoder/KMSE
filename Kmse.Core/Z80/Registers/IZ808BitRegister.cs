namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Base interface for Z80 8-bit register with core handling for the value
/// </summary>
public interface IZ808BitRegister
{
    public byte Value { get; }
    public byte ShadowValue { get; }

    public void Reset();
    public void Set(byte value);
    public void Set(IZ808BitRegister register);
    public void SetFromDataInMemory(ushort address, byte offset = 0);
    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0);
    public void SaveToMemory(ushort address, byte offset = 0);
    public void SaveToMemory(IZ8016BitRegister register, byte offset = 0);
    public void SwapWithShadow();
}