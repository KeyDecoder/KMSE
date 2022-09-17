namespace Kmse.Core.Z80.Registers;

public interface IZ808BitRegister
{
    public byte Value { get; }
    public byte ShadowValue { get; }

    public void Reset();
    public void Set(byte value);
    public void SetFromDataInMemory(ushort address, byte offset = 0);
    public void SwapWithShadow();
}