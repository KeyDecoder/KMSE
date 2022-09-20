using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

/// <summary>
///     Base class for a 16 bit register which is composed of two 8 bit registers
/// </summary>
public abstract class Z8016BitGeneralPurposeRegisterBase : Z8016BitRegisterBase, IZ8016BitGeneralPurposeRegister
{
    protected Z8016BitGeneralPurposeRegisterBase(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags) { }

    protected abstract IZ808BitRegister HighRegister { get; }
    protected abstract IZ808BitRegister LowRegister { get; }

    public override ushort Value => (ushort)(LowRegister.Value + (HighRegister.Value << 8));
    public override byte High => HighRegister.Value;
    public override byte Low => LowRegister.Value;
    public ushort ShadowValue => (ushort)(LowRegister.ShadowValue + (HighRegister.ShadowValue << 8));

    public void Reset()
    {
        LowRegister.Reset();
        HighRegister.Reset();
    }

    public override void Set(ushort value)
    {
        var (high, low) = Bitwise.ToBytes(value);
        LowRegister.Set(low);
        HighRegister.Set(high);
    }

    public void Set(IZ8016BitRegister register)
    {
        Set(register.Value);
    }

    public void SetHigh(byte value)
    {
        HighRegister.Set(value);
    }

    public void SetLow(byte value)
    {
        LowRegister.Set(value);
    }

    public void SetFromDataInMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        var low = Memory[location];
        location++;
        var high = Memory[location];

        LowRegister.Set(low);
        HighRegister.Set(high);
    }

    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0)
    {
        SetFromDataInMemory(register.Value, offset);
    }

    public void SaveToMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        Memory[location] = LowRegister.Value;
        Memory[(ushort)(location + 1)] = HighRegister.Value;
    }

    public void SwapWithShadow()
    {
        LowRegister.SwapWithShadow();
        HighRegister.SwapWithShadow();
    }

    public Z80Register AsRegister()
    {
        return new Z80Register
        {
            Low = LowRegister.Value,
            High = HighRegister.Value
        };
    }

    public Z80Register ShadowAsRegister()
    {
        return new Z80Register
        {
            Low = LowRegister.ShadowValue,
            High = HighRegister.ShadowValue
        };
    }

    public void Increment()
    {
        var currentValue = Value;
        currentValue++;
        Set(currentValue);
    }

    public void Decrement()
    {
        var currentValue = Value;
        currentValue--;
        Set(currentValue);
    }
}