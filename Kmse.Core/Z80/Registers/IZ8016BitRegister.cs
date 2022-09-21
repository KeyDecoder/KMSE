using Kmse.Core.Z80.Model;

namespace Kmse.Core.Z80.Registers;

public interface IZ8016BitRegister
{
    ushort Value { get; }
    byte High { get; }
    byte Low { get; }

    void Reset();
    void Set(ushort value);
    void Set(IZ8016BitRegister register);
    void SetHigh(byte value);
    void SetLow(byte value);
    void SetFromDataInMemory(ushort address, byte offset = 0);
    void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0);
    void SaveToMemory(ushort address, byte offset = 0);
    Z80Register AsRegister();
}