using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers;

public abstract class Z808BitRegister : Z80RegisterBase, IZ808BitRegister
{
    protected readonly IMasterSystemMemory Memory;
    protected byte InternalShadowValue;
    protected byte InternalValue;

    protected Z808BitRegister(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(flags)
    {
        Memory = memory;
    }

    public byte Value
    {
        get => InternalValue;
        protected set => InternalValue = value;
    }

    public byte ShadowValue
    {
        get => InternalShadowValue;
        protected set => InternalShadowValue = value;
    }

    public void Reset()
    {
        Value = 0;
        ShadowValue = 0;
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