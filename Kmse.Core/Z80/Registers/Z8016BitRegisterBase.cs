using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public abstract class Z8016BitRegisterBase : Z80RegisterBase
{
    protected readonly IMasterSystemMemory Memory;

    protected Z8016BitRegisterBase(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(flags)
    {
        Memory = memory;
    }

    public abstract ushort Value { get; }
    public abstract byte High { get; }
    public abstract byte Low { get; }
    public abstract void Set(ushort value);

    public void ResetBitByRegisterLocation(int bit, int offset)
    {
        // Clear bit of value in memory pointed to by HL register
        var value = Memory[(ushort)(Value + offset)];
        Bitwise.Clear(ref value, bit);
        Memory[(ushort)(Value + offset)] = value;
    }

    public void SetBitByRegisterLocation(int bit, int offset)
    {
        // Clear bit of value in memory pointed to by HL register
        var value = Memory[(ushort)(Value + offset)];
        Bitwise.Set(ref value, bit);
        Memory[(ushort)(Value + offset)] = value;
    }

    public void TestBitByRegisterLocation(int bit, int offset)
    {
        var value = Memory[(ushort)(Value + offset)];
        var bitSet = Bitwise.IsSet(value, bit);

        Flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, !bitSet);
        Flags.SetFlag(Z80StatusFlags.HalfCarryH);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);

        // This behaviour is not documented
        Flags.SetClearFlagConditional(Z80StatusFlags.SignS, bit == 7 && bitSet);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, !bitSet);
    }

    public void Add(ushort source, bool withCarry = false)
    {
        int valueWithCarry = source;
        if (withCarry && Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }

        var newValue = Value + valueWithCarry;

        // Half carry for 16 bit occurs if the result of adding the lower of the higher 8 bit value means it will set the next higher bit (13th bit and basically overflows)
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH,
            (Value & 0x0FFF) + (valueWithCarry & 0x0FFF) > 0x0FFF);

        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        // Carry occurs if the result of adding the higher nibbles means it will set the next higher bit (17th bit and basically overflows)
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC,
            (Value & 0xFFFF) + (valueWithCarry & 0xFFFF) > 0xFFFF);

        if (withCarry)
        {
            Flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((ushort)newValue, 15));
            Flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFFFF) == 0);
            Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV,
                ((Value ^ valueWithCarry) & 0x8000) == 0 &&
                ((Value ^ (newValue & 0xFFFF)) & 0x8000) != 0);
        }

        Set((ushort)newValue);
    }

    public void Subtract(ushort source, bool withCarry = false)
    {
        int valueWithCarry = source;
        if (withCarry && Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }

        var newValue = Value - valueWithCarry;

        Flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((ushort)newValue, 15));
        Flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFFFF) == 0);

        // Half carry for 16 bit occurs if the result of adding the lower of the higher 8 bit value means it will set the next higher bit (13th bit and basically overflows)
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (((Value ^ newValue ^ source) >> 8) & 0x10) != 0);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV,
            ((source ^ Value) & (Value ^ newValue) & 0x8000) != 0);

        Flags.SetFlag(Z80StatusFlags.AddSubtractN);
        // Carry occurs if the result of adding the higher nibbles means it will set the next higher bit (17th bit and basically overflows)
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, (newValue & 0x10000) != 0);

        Set((ushort)newValue);
    }

    public void RotateLeftCircular(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        RotateLeftCircular(ref value);
        Memory[location] = value;
    }

    public void RotateLeft(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        RotateLeft(ref value);
        Memory[location] = value;
    }

    public void RotateRightCircular(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        RotateRightCircular(ref value);
        Memory[location] = value;
    }

    public void RotateRight(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        RotateRight(ref value);
        Memory[location] = value;
    }

    public void ShiftLeftArithmetic(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        ShiftLeftArithmetic(ref value);
        Memory[location] = value;
    }

    public void ShiftRightArithmetic(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        ShiftRightArithmetic(ref value);
        Memory[location] = value;
    }

    public void ShiftLeftLogical(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        ShiftLeftLogical(ref value);
        Memory[location] = value;
    }

    public void ShiftRightLogical(int offset)
    {
        var location = (ushort)(Value + offset);
        var value = Memory[location];
        ShiftRightLogical(ref value);
        Memory[location] = value;
    }
}