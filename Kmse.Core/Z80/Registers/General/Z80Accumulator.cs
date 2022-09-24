using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.Z80.Registers.General;

public class Z80Accumulator : Z808BitGeneralPurposeRegister, IZ80Accumulator
{
    public Z80Accumulator(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags) { }

    public void SetFromInterruptRegister(IZ80InterruptPageAddressRegister register, bool interruptFlipFlop2Status)
    {
        LoadSpecial8BitRegisterToAccumulator(register.Value, interruptFlipFlop2Status);
    }

    public void SetFromMemoryRefreshRegister(IZ80MemoryRefreshRegister register, bool interruptFlipFlop2Status)
    {
        LoadSpecial8BitRegisterToAccumulator(register.Value, interruptFlipFlop2Status);
    }

    public void RotateLeftDigit(IZ80HlRegister hl)
    {
        var value = Memory[hl.Value];
        var hll = value & 0x0F;
        var hlh = (byte)((value & 0xF0) >> 4);
        var al = Value & 0x0F;
        var ah = (byte)((Value & 0xF0) >> 4);

        // HL is lower bits of HL copied to high bits + lower 4 bits of A 
        var newHlValue = (byte)((hll << 4) + al);
        // A is higher bits of A left same + higher bits of hl 
        var newAValue = (byte)((ah << 4) + hlh);

        Flags.SetIfNegative(newAValue);
        Flags.SetIfZero(newAValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newAValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);

        Memory[hl.Value] = newHlValue;
        Value = newAValue;
    }

    public void RotateRightDigit(IZ80HlRegister hl)
    {
        var value = Memory[hl.Value];
        var hll = value & 0x0F;
        var hlh = (byte)((value & 0xF0) >> 4);
        var al = Value & 0x0F;
        var ah = (byte)((Value & 0xF0) >> 4);

        // HL is lower bits of HL copied to high bits + lower 4 bits of A 
        var newHlValue = (byte)((al << 4) + hlh);
        // A is higher bits of A left same + higher bits of hl 
        var newAValue = (byte)((ah << 4) + hll);

        Flags.SetIfNegative(newAValue);
        Flags.SetIfZero(newAValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newAValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);

        Memory[hl.Value] = newHlValue;
        Value = newAValue;
    }

    public void DecimalAdjustAccumulator()
    {
        /*
        Credit to https://github.com/xdanieldzd/MasterFudge for referencing this document - https://worldofspectrum.org/faq/reference/z80reference.htm which explained how to do this in a clear and simple way

        The purpose of the DAA (Decimal Adjust Accumulator) instruction is to make an adjustment to the value in the A register, after performing a binary mathematical operation, such that the result is as if the operation were performed with BCD (Binary Coded Decimal) maths. The Z80 achieves this by adjusting the A register by a value which is dependent upon the value of the A register, the Carry flag, Half-Carry flag (carry from bit 3 to 4), and the N-flag (which defines whether the last operation was an add or subtract).

        The algorithm used is as follows:

        - If the A register is greater than 0x99, OR the Carry flag is SET, then
            The upper four bits of the Correction Factor are set to 6,
            and the Carry flag will be SET.
          Else
            The upper four bits of the Correction Factor are set to 0,
            and the Carry flag will be CLEARED.

        - If the lower four bits of the A register (A AND 0x0F) is greater than 9,
          OR the Half-Carry (H) flag is SET, then
            The lower four bits of the Correction Factor are set to 6.
          Else
            The lower four bits of the Correction Factor are set to 0.

        - This results in a Correction Factor of 0x00, 0x06, 0x60 or 0x66.
        - If the N flag is CLEAR, then
            ADD the Correction Factor to the A register.
          Else
            SUBTRACT the Correction Factor from the A register.

        - The Flags are set as follows:

          Carry:      Set/clear as in the first step above.
          Half-Carry: Set if the correction operation caused a binary carry/borrow
                      from bit 3 to bit 4.
                      For this purpose, may be calculated as:
                      Bit 4 of: A(before) XOR A(after).
          S,Z,P,5,3:  Set as for simple logic operations on the resultant A value.
          N:          Leave.

        */

        byte factor = 0;
        var currentValue = Value;
        if (Value > 0x99 || Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            factor |= 0x06 << 4;
            Flags.SetFlag(Z80StatusFlags.CarryC);
        }
        else
        {
            factor &= 0x0F;
            Flags.ClearFlag(Z80StatusFlags.CarryC);
        }

        if ((Value & 0x0F) > 9 || Flags.IsFlagSet(Z80StatusFlags.HalfCarryH))
        {
            factor |= 0x06;
        }
        else
        {
            factor &= 0xF0;
        }

        if (!Flags.IsFlagSet(Z80StatusFlags.AddSubtractN))
        {
            Value += factor;
        }
        else
        {
            Value -= factor;
        }

        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, Bitwise.IsSet(currentValue ^ Value, 4));
        Flags.SetIfNegative(Value);
        Flags.SetIfZero(Value);
        Flags.SetParityFromValue(Value);
    }

    public void InvertAccumulatorRegister()
    {
        var onesComplementValue = ~Value & 0xFF;

        Flags.SetFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);

        Value = (byte)onesComplementValue;
    }

    public void NegateAccumulatorRegister()
    {
        var twosComplementValue = (0 - Value) & 0xFF;

        Flags.SetIfTwosComplementNegative(twosComplementValue);
        Flags.SetIfZero(twosComplementValue);

        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (Value & 0x0F) > 0);
        Flags.SetIfDecrementOverflow(Value);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Value != 0x00);

        Value = (byte)twosComplementValue;
    }

    public void AddFromMemory(IZ8016BitRegister register, int offset, bool withCarry = false)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        Add(value, withCarry);
    }

    public void Add(byte value, bool withCarry = false)
    {
        int valueWithCarry = value;
        if (withCarry && Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry = value + 0x01;
        }

        var newValue = Value + valueWithCarry;

        Flags.SetIfNegative((byte)newValue);
        Flags.SetIfZero((byte)(newValue & 0xFF));
        Flags.SetIfHalfCarry(Value, value, newValue);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ Value ^ 0x80) & (Value ^ newValue) & 0x80) != 0);

        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        // A carry is same as half carry just on the overall value
        // Since we stored the sum as a 32 bit integer, we can see if went past the max value and bit 7 must have carried over into bit 8
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, newValue > 0xFF);

        Set((byte)newValue);
    }

    public void SubtractFromMemory(IZ8016BitRegister register, int offset, bool withCarry = false)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        Subtract(value, withCarry);
    }

    public void Subtract(byte value, bool withCarry = false)
    {
        int valueWithCarry = value;
        if (withCarry && Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }

        var newValue = Value - valueWithCarry;

        Flags.SetIfNegative((byte)newValue);
        Flags.SetIfZero((byte)(newValue & 0xFF));
        Flags.SetIfHalfCarry(Value, value, newValue);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ Value) & (Value ^ newValue) & 0x80) != 0);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);

        // Subtraction went negative, so carried over into next bit
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Value < valueWithCarry);

        Set((byte)newValue);
    }

    public void Compare(byte value)
    {
        // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
        var difference = Value - value;

        Flags.SetIfNegative((byte)difference);
        Flags.SetIfZero((byte)(difference & 0xFF));
        Flags.SetIfHalfCarry(Value, value, difference);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ Value) & (Value ^ difference) & 0x80) != 0);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);

        // Subtraction went negative, so carried over into next bit
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Value < value);
    }

    public void CompareFromMemory(IZ8016BitRegister register, int offset)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        Compare(value);
    }

    public void And(byte value, byte valueToAndAgainst)
    {
        var newValue = (byte)(value & valueToAndAgainst);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero((byte)(newValue & 0xFF));
        Flags.SetFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.ClearFlag(Z80StatusFlags.CarryC);

        Set(newValue);
    }

    public void AndFromMemory(IZ8016BitRegister register, int offset, byte valueToAndAgainst)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        And(value, valueToAndAgainst);
    }

    public void Or(byte value, byte valueToAndAgainst)
    {
        var newValue = (byte)(value | valueToAndAgainst);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero((byte)(newValue & 0xFF));
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.ClearFlag(Z80StatusFlags.CarryC);

        Set(newValue);
    }

    public void OrFromMemory(IZ8016BitRegister register, int offset, byte valueToOrAgainst)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        Or(value, valueToOrAgainst);
    }

    public void Xor(byte value, byte valueToXorAgainst)
    {
        var newValue = (byte)(value ^ valueToXorAgainst);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero((byte)(newValue & 0xFF));
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.ClearFlag(Z80StatusFlags.CarryC);

        Set(newValue);
    }

    public void XorFromMemory(IZ8016BitRegister register, int offset, byte valueToXorAgainst)
    {
        var value = Memory[(ushort)(register.Value + offset)];
        Xor(value, valueToXorAgainst);
    }

    public void RotateLeftCircularAccumulator()
    {
        // Rotate A left by 1 bit, bit 7 is copied to carry flag and bit 0
        // This is special method since RLCA flags are set differently to RLC r instruction
        var newValue = (byte)(Value << 1);
        var bit7Set = Bitwise.IsSet(Value, 7);
        if (bit7Set)
        {
            // Copy bit 7 to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        Value = newValue;
    }

    public void RotateLeftAccumulator()
    {
        // Rotate A left by 1 bit, bit 7 is copied to carry flag and carry flag copied to bit 0
        // This is special method since RLA flags are set differently to RL r instruction
        var newValue = (byte)(Value << 1);
        var bit7Set = Bitwise.IsSet(Value, 7);
        if (Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        Value = newValue;
    }

    public void RotateRightCircularAccumulator()
    {
        // Rotate A right by 1 bit, bit 0 is copied to carry flag and bit 7
        // This is special method since RRCA flags are set differently to RRC r instruction
        var newValue = (byte)(Value >> 1);
        var bit0Set = Bitwise.IsSet(Value, 0);
        if (bit0Set)
        {
            // Copy bit 0 to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        Value = newValue;
    }

    public void RotateRightAccumulator()
    {
        // Rotate A right by 1 bit, bit 0 is copied to carry flag and carry flag copied to bit 7
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(Value >> 1);
        var bit0Set = Bitwise.IsSet(Value, 0);
        if (Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        Value = newValue;
    }

    private void LoadSpecial8BitRegisterToAccumulator(byte sourceData, bool interruptFlipFlop2Status)
    {
        Value = sourceData;

        // Check flags since copying from special register into accumulator
        Flags.SetIfNegative(sourceData);
        Flags.SetIfZero(sourceData);

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, interruptFlipFlop2Status);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }
}