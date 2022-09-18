using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers;

public abstract class Z808BitRegister : IZ808BitRegister
{
    protected readonly IMasterSystemMemory Memory;
    public byte Value { get; protected set; }
    public byte ShadowValue { get; protected set; }

    protected Z808BitRegister(IMasterSystemMemory memory)
    {
        Memory = memory;
    }

    public void Reset()
    {
        Value = 0;
    }

    public void Set(byte value)
    {
        Value = value;
    }

    public void Set(IZ808BitRegister register)
    {
        Set(register.Value);
    }

    public void SetFromDataInMemory(ushort address, byte offset = 0)
    {
        Value = Memory[(ushort)(address + offset)];
    }

    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0)
    {
        SetFromDataInMemory(register.Value, offset);
    }

    public void SaveToMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        Memory[location] = Value;
    }

    public void SaveToMemory(IZ8016BitRegister register, byte offset = 0)
    {
        SaveToMemory(register.Value, offset);
    }

    public void SwapWithShadow()
    {
        (Value, ShadowValue) = (ShadowValue, Value);
    }
}