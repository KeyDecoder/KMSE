using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public interface IZ8016BitRegister
{
    public ushort Value { get; }
    public byte High { get; }
    public byte Low { get; }

    public void Reset();
    public void Set(ushort value);
    public void Set(IZ8016BitRegister register);
    public void SetHigh(byte value);
    public void SetLow(byte value);
    public void SetFromDataInMemory(ushort address, byte offset = 0);
    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0);
    public void SaveToMemory(ushort address, byte offset = 0);
    public Z80Register AsRegister();

    void Increment();
    void Decrement();
}