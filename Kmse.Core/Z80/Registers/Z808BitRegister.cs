using Kmse.Core.Memory;

namespace Kmse.Core.Z80.Registers;

public class Z808BitRegister : IZ808BitRegister
{
    protected readonly IMasterSystemMemory Memory;
    public byte Value { get; protected set; }
    public byte ShadowValue { get; protected set; }

    public Z808BitRegister(IMasterSystemMemory memory)
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

    public void SetFromDataInMemory(ushort address, byte offset = 0)
    {
        Value = Memory[(ushort)(address + offset)];
    }

    public void SwapWithShadow()
    {
        (Value, ShadowValue) = (ShadowValue, Value);
    }
}