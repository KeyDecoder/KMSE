using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80;

/// <summary>
///     Math and logic operations
/// </summary>
public partial class Z80Cpu
{
    private void DecimalAdjustAccumulator()
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
        var currentValue = _af.High;
        if (_af.High > 0x99 || IsFlagSet(Z80StatusFlags.CarryC))
        {
            factor |= (0x06 << 4);
            SetFlag(Z80StatusFlags.CarryC);
        }
        else
        {
            factor &= 0x0F;
            ClearFlag(Z80StatusFlags.CarryC);
        }

        if ((_af.High & 0x0F) > 9 || IsFlagSet(Z80StatusFlags.HalfCarryH))
        {
            factor |= 0x06;
        }
        else
        {
            factor &= 0xF0;
        }

        if (!IsFlagSet(Z80StatusFlags.AddSubtractN))
        {
            _af.High += factor;
        }
        else
        {
            _af.High -= factor;
        }

        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, Bitwise.IsSet(currentValue ^ _af.High, 4));
        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(_af.High, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, _af.High == 0);
        SetParityFromValue(_af.High);
    }

    private void InvertAccumulatorRegister()
    {
        var onesComplementValue = ~_af.High & 0xFF;

        SetFlag(Z80StatusFlags.HalfCarryH);
        SetFlag(Z80StatusFlags.AddSubtractN);

        _af.High = (byte)onesComplementValue;
    }

    private void NegateAccumulatorRegister()
    {
        var twosComplementValue = (0 - _af.High) & 0xFF;

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(twosComplementValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, twosComplementValue == 0);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (_af.High & 0x0F) > 0);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _af.High == 0x80);
        SetFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, _af.High != 0x00);

        _af.High = (byte)twosComplementValue;
    }

    private void ResetBitByOpCode(byte opCode)
    {
        var bit = (opCode & 0x38) >> 3;
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to reset");
        }
        var register = opCode & 0x07;

        if (register == 0x06)
        {
            ResetBitByRegisterLocation(_hl, bit, 0);
            // Accessing (HL) increases cycle count
            _currentCycleCount += 7;
            return;
        }

        switch (register)
        {
            case 0:
                Bitwise.Clear(ref _bc.High, bit);
                break;
            case 1:
                Bitwise.Clear(ref _bc.Low, bit);
                break;
            case 2:
                Bitwise.Clear(ref _de.High, bit);
                break;
            case 3:
                Bitwise.Clear(ref _de.Low, bit);
                break;
            case 4:
                Bitwise.Clear(ref _hl.High, bit);
                break;
            case 5:
                Bitwise.Clear(ref _hl.Low, bit);
                break;
            case 7:
                Bitwise.Clear(ref _af.High, bit);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Register id {register} is not a valid register to reset");
        }
    }

    private void ResetBitByRegisterLocation(Z80Register register, int bit, int offset)
    {
        // Clear bit of value in memory pointed to by HL register
        var value = _memory[(ushort)(register.Word + offset)];
        Bitwise.Clear(ref value, bit);
        _memory[(ushort)(register.Word + offset)] = value;
    }

    private void SetBitByOpCode(byte opCode)
    {
        var bit = (opCode & 0x38) >> 3;
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to set");
        }
        var register = opCode & 0x07;

        if (register == 0x06)
        {
            SetBitByRegisterLocation(_hl, bit, 0);
            // Accessing (HL) increases cycle count
            _currentCycleCount += 7;
            return;
        }

        switch (register)
        {
            case 0:
                Bitwise.Set(ref _bc.High, bit);
                break;
            case 1:
                Bitwise.Set(ref _bc.Low, bit);
                break;
            case 2:
                Bitwise.Set(ref _de.High, bit);
                break;
            case 3:
                Bitwise.Set(ref _de.Low, bit);
                break;
            case 4:
                Bitwise.Set(ref _hl.High, bit);
                break;
            case 5:
                Bitwise.Set(ref _hl.Low, bit);
                break;
            case 7:
                Bitwise.Set(ref _af.High, bit);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Register id {register} is not a valid register to set");
        }
    }

    private void SetBitByRegisterLocation(Z80Register register, int bit, int offset)
    {
        // Clear bit of value in memory pointed to by HL register
        var value = _memory[(ushort)(register.Word + offset)];
        Bitwise.Set(ref value, bit);
        _memory[(ushort)(register.Word + offset)] = value;
    }

    private void TestBitByOpCode(byte opCode)
    {
        var bit = (opCode & 0x38) >> 3;
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to test");
        }
        var register = opCode & 0x07;

        if (register == 0x06)
        {
            TestBitByRegisterLocation(_hl, bit, 0);
            // Testing bit via (HL) memory location increases cycle count
            _currentCycleCount += 4;
            return;
        }

        var valueToCheck = register switch
        {
            0 => _bc.High,
            1 => _bc.Low,
            2 => _de.High,
            3 => _de.Low,
            4 => _hl.High,
            5 => _hl.Low,
            7 => _af.High,
            _ => throw new ArgumentOutOfRangeException($"Register id {register} is not a valid register to test bit on")
    };
        var bitSet = Bitwise.IsSet(valueToCheck, bit);
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, !bitSet);
        SetFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);

        // This behaviour is not documented
        SetClearFlagConditional(Z80StatusFlags.SignS, (bit == 7 && bitSet));
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, !bitSet);
    }

    private void TestBitByRegisterLocation(Z80Register register, int bit, int offset)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        var bitSet = Bitwise.IsSet(value, bit);

        SetClearFlagConditional(Z80StatusFlags.ZeroZ, !bitSet);
        SetFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);

        // This behaviour is not documented
        SetClearFlagConditional(Z80StatusFlags.SignS, bit == 7 && bitSet);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, !bitSet);
    }

    private void AddValueAtRegisterMemoryLocationTo8BitRegister(Z80Register register, int offset, ref byte destination, bool withCarry = false)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        AddValueTo8BitRegister(value, ref destination, withCarry);
    }

    private void AddValueTo8BitRegister(byte value, ref byte destination, bool withCarry = false)
    {
        var valueWithCarry = value;
        if (withCarry && IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }
        int newValue = destination + valueWithCarry;

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        // Half carry occurs if the result of adding the lower nibbles means it will set the next higher bit (basically overflows)
        // This is since the adding is done in two 4 bit operations not 1 8 bit operation internally
        // This is then combined with the DAA instruction which adjusts the result to get the valid value
        //https://retrocomputing.stackexchange.com/questions/4693/why-does-the-z80-have-a-half-carry-bit
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (((destination ^ newValue ^ value) & 0x10) != 0));
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ destination ^ 0x80) & (destination ^ newValue) & 0x80) != 0);

        ClearFlag(Z80StatusFlags.AddSubtractN);
        // A carry is same as half carry just on the overall value
        // Since we stored the sum as a 32 bit integer, we can see if went past the max value and bit 7 must have carried over into bit 8
        SetClearFlagConditional(Z80StatusFlags.CarryC, newValue > 0xFF);

        destination = (byte)newValue;
    }

    private void Add16BitRegisterTo16BitRegister(Z80Register source, ref Z80Register destination, bool withCarry = false)
    {   
        var valueWithCarry = source.Word;
        if (withCarry && IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }
        int newValue = destination.Word + valueWithCarry;

        // Half carry for 16 bit occurs if the result of adding the lower of the higher 8 bit value means it will set the next higher bit (13th bit and basically overflows)
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH,
            (destination.Word & 0x0FFF) + (valueWithCarry & 0x0FFF) > 0x0FFF);

        ClearFlag(Z80StatusFlags.AddSubtractN);
        // Carry occurs if the result of adding the higher nibbles means it will set the next higher bit (17th bit and basically overflows)
        SetClearFlagConditional(Z80StatusFlags.CarryC,
            (destination.Word & 0xFFFF) + (valueWithCarry & 0xFFFF) > 0xFFFF);

        if (withCarry)
        {
            SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((ushort)newValue, 15));
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFFFF) == 0);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV,
                ((destination.Word ^ valueWithCarry) & 0x8000) == 0 &&
                ((destination.Word ^ (newValue & 0xFFFF)) & 0x8000) != 0);
        }

        destination.Word = (ushort)newValue;
    }

    private void SubtractValueAtRegisterMemoryLocationFrom8BitRegister(Z80Register register, int offset,
        ref byte destination, bool withCarry = false)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        SubtractValueFrom8BitRegister(value, ref destination, withCarry);
    }

    private void SubtractValueFrom8BitRegister(byte value, ref byte destination, bool withCarry = false)
    {
        var valueWithCarry = value;
        if (withCarry && IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }
        int newValue = destination - valueWithCarry;

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((byte)newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        // Half carry occurs if the result of subtracting the higher nibbles means it will set the next lower bit (basically underflows)
        // We check if the subtraction means that adding higher nibbles sets bit 3
        // This is then combined with the DAA instruction which adjusts the result to get the valid value
        //https://retrocomputing.stackexchange.com/questions/4693/why-does-the-z80-have-a-half-carry-bit
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((destination ^ newValue ^ value) & 0x10) != 0);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ destination) & (destination ^ newValue) & 0x80) != 0);
        SetFlag(Z80StatusFlags.AddSubtractN);

        // Subtraction went negative, so carried over into next bit
        SetClearFlagConditional(Z80StatusFlags.CarryC, destination < valueWithCarry);

        destination = (byte)newValue;
    }

    private void Sub16BitRegisterFrom16BitRegister(Z80Register source, ref Z80Register destination, bool withCarry = false)
    {
        var valueWithCarry = source.Word;
        if (withCarry && IsFlagSet(Z80StatusFlags.CarryC))
        {
            valueWithCarry += 0x01;
        }
        int newValue = destination.Word - valueWithCarry;

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((ushort)newValue, 15));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFFFF) == 0);

        // Half carry for 16 bit occurs if the result of adding the lower of the higher 8 bit value means it will set the next higher bit (13th bit and basically overflows)
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((((destination.Word ^ newValue ^ valueWithCarry) >> 8) & 0x10) != 0));
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((source.Word ^ destination.Word) & (destination.Word ^ newValue) & 0x8000) != 0);

        SetFlag(Z80StatusFlags.AddSubtractN);
        // Carry occurs if the result of adding the higher nibbles means it will set the next higher bit (17th bit and basically overflows)
        SetClearFlagConditional(Z80StatusFlags.CarryC, ((newValue& 0x10000) != 0));

        destination.Word = (ushort)newValue;
    }

    private void Compare8Bit(byte value, byte valueToCompareTo)
    {
        // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
        var difference = valueToCompareTo - value;

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((byte)difference, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (difference & 0xFF) == 0);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((valueToCompareTo ^ difference ^ value) & 0x10) != 0);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ valueToCompareTo) & (valueToCompareTo ^ difference) & 0x80) != 0);
        SetFlag(Z80StatusFlags.AddSubtractN);
        // Subtraction went negative, so carried over into next bit
        //SetClearFlagConditional(Z80StatusFlags.CarryC, difference < 0);
        SetClearFlagConditional(Z80StatusFlags.CarryC, valueToCompareTo < value);
    }

    private void CompareIncrement()
    {
        var value = _memory[_hl.Word];
        // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
        var difference = _af.High - (sbyte)value;

        Increment16Bit(ref _hl);
        Decrement16Bit(ref _bc);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((byte)difference, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (difference & 0xFF) == 0);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((_af.High ^ difference ^ value) & 0x10) != 0);
        SetFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Word != 0);
    }

    private void CompareDecrement()
    {
        var value = _memory[_hl.Word];
        // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
        var difference = _af.High - (sbyte)value;

        Decrement16Bit(ref _hl);
        Decrement16Bit(ref _bc);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet((byte)difference, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, _af.High == value);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((_af.High ^ difference ^ value) & 0x10) != 0);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Word != 0);
        SetFlag(Z80StatusFlags.AddSubtractN);
    }

    private void Compare8BitToMemoryLocationFrom16BitRegister(Z80Register register, int offset, byte valueToCompareTo)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        Compare8Bit(value, valueToCompareTo);
    }

    private void And8Bit(byte value, byte valueToAndAgainst, ref byte register)
    {
        var newValue = (byte)(value & valueToAndAgainst);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        SetFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        ClearFlag(Z80StatusFlags.CarryC);

        register = newValue;
    }

    private void And8BitToMemoryLocationFrom16BitRegister(Z80Register register, int offset, byte valueToAndAgainst,
        ref byte registerToStoreValueIn)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        And8Bit(value, valueToAndAgainst, ref registerToStoreValueIn);
    }

    private void Or8Bit(byte value, byte valueToAndAgainst, ref byte register)
    {
        var newValue = (byte)(value | valueToAndAgainst);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        ClearFlag(Z80StatusFlags.CarryC);

        register = newValue;
    }

    private void Or8BitToMemoryLocationFrom16BitRegister(Z80Register register, int offset, byte valueToOrAgainst,
        ref byte registerToStoreValueIn)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        Or8Bit(value, valueToOrAgainst, ref registerToStoreValueIn);
    }

    private void Xor8Bit(byte value, byte valueToXorAgainst, ref byte register)
    {
        var newValue = (byte)(value ^ valueToXorAgainst);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        ClearFlag(Z80StatusFlags.CarryC);

        register = newValue;
    }

    private void Xor8BitToMemoryLocationFrom16BitRegister(Z80Register register, int offset, byte valueToXorAgainst,
        ref byte registerToStoreValueIn)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        Xor8Bit(value, valueToXorAgainst, ref registerToStoreValueIn);
    }

    private void RotateLeftCircularAccumulator()
    {
        // Rotate A left by 1 bit, bit 7 is copied to carry flag and bit 0
        // This is special method since RLCA flags are set differently to RLC r instruction
        var newValue = (byte)(_af.High << 1);
        var bit7Set = Bitwise.IsSet(_af.High, 7);
        if (bit7Set)
        {
            // Copy bit 7 to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        _af.High = newValue;
    }

    private void RotateLeftAccumulator()
    {
        // Rotate A left by 1 bit, bit 7 is copied to carry flag and carry flag copied to bit 0
        // This is special method since RLA flags are set differently to RL r instruction
        var newValue = (byte)(_af.High << 1);
        var bit7Set = Bitwise.IsSet(_af.High, 7);
        if (IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        _af.High = newValue;
    }

    private void RotateRightCircularAccumulator()
    {
        // Rotate A right by 1 bit, bit 0 is copied to carry flag and bit 7
        // This is special method since RRCA flags are set differently to RRC r instruction
        var newValue = (byte)(_af.High >> 1);
        var bit0Set = Bitwise.IsSet(_af.High, 0);
        if (bit0Set)
        {
            // Copy bit 0 to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        _af.High = newValue;
    }

    private void RotateRightAccumulator()
    {
        // Rotate A right by 1 bit, bit 0 is copied to carry flag and carry flag copied to bit 7
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(_af.High >> 1);
        var bit0Set = Bitwise.IsSet(_af.High, 0);
        if (IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        _af.High = newValue;
    }

    private void RotateLeftCircular(ref byte register)
    {
        // Rotate register left by 1 bit, bit 7 is copied to carry flag and bit 0
        var newValue = (byte)(register << 1);
        var bit7Set = Bitwise.IsSet(register, 7);
        if (bit7Set)
        {
            // Copy bit 7 to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        register = newValue;
    }

    private void RotateLeft(ref byte register)
    {
        // Rotate register left by 1 bit, bit 7 is copied to carry flag and carry flag copied to bit 0
        var newValue = (byte)(register << 1);
        var bit7Set = Bitwise.IsSet(register, 7);
        if (IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        register = newValue;
    }

    private void RotateRightCircular(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag and bit 7
        var newValue = (byte)(register >> 1);
        var bit0Set = Bitwise.IsSet(register, 0);
        if (bit0Set)
        {
            // Copy bit 0 to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        register = newValue;
    }

    private void RotateRight(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag and carry flag copied to bit 7
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(register >> 1);
        var bit0Set = Bitwise.IsSet(register, 0);
        if (IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        register = newValue;
    }

    private void RotateLeftCircular16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        RotateLeftCircular(ref value);
        _memory[location] = value;
    }

    private void RotateLeft16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        RotateLeft(ref value);
        _memory[location] = value;
    }

    private void RotateRightCircular16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        RotateRightCircular(ref value);
        _memory[location] = value;
    }

    private void RotateRight16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        RotateRight(ref value);
        _memory[location] = value;
    }

    private void ShiftLeftArithmetic(ref byte register)
    {
        // Shift register left by 1 bit, bit 7 is copied to carry flag
        var newValue = (byte)(register << 1);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 7));

        register = newValue;
    }

    private void ShiftRightArithmetic(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(register >> 1);
        var bit7Set = Bitwise.IsSet(register, 7);

        if (bit7Set)
        {
            // If bit 7 is set in original then leave as set even as we shift right
            Bitwise.Set(ref newValue, 7);
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 0));

        register = newValue;
    }

    private void ShiftLeftArithmetic16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        ShiftLeftArithmetic(ref value);
        _memory[location] = value;
    }

    private void ShiftRightArithmetic16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        ShiftRightArithmetic(ref value);
        _memory[location] = value;
    }

    private void ShiftLeftLogical(ref byte register)
    {
        // Shift register left by 1 bit, bit 7 is copied to carry flag
        var newValue = (byte)(register << 1);

        // The difference between shift left logical and shift left arithmetic is this sets bit 0
        Bitwise.Set(ref newValue, 0);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 7));

        register = newValue;
    }

    private void ShiftRightLogical(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(register >> 1);

        // The difference between shift right logical and shift right arithmetic is this does maintain bit 7 when shifting and just clears it
        Bitwise.Clear(ref newValue, 7);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 0));

        register = newValue;
    }

    private void ShiftLeftLogical16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        ShiftLeftLogical(ref value);
        _memory[location] = value;
    }

    private void ShiftRightLogical16BitRegisterMemoryLocation(Z80Register register, int offset)
    {
        var location = (ushort)(register.Word + offset);
        var value = _memory[location];
        ShiftRightLogical(ref value);
        _memory[location] = value;
    }

    private void RotateLeftDigit()
    {
        var value = _memory[_hl.Word];
        var hll = value & 0x0F;
        var hlh = (byte)((value & 0xF0) >> 4);
        var al = _af.High & 0x0F;
        var ah = (byte)((_af.High & 0xF0) >> 4);

        // HL is lower bits of HL copied to high bits + lower 4 bits of A 
        var newHlValue = (byte)((hll << 4) + al);
        // A is higher bits of A left same + higher bits of hl 
        var newAValue = (byte)((ah << 4) + hlh);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newAValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, newAValue == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newAValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);

        _memory[_hl.Word] = newHlValue;
        _af.High = newAValue;
    }

    private void RotateRightDigit()
    {
        var value = _memory[_hl.Word];
        var hll = value & 0x0F;
        var hlh = (byte)((value & 0xF0) >> 4);
        var al = _af.High & 0x0F;
        var ah = (byte)((_af.High & 0xF0) >> 4);

        // HL is lower bits of HL copied to high bits + lower 4 bits of A 
        var newHlValue = (byte)((al << 4) + hlh);
        // A is higher bits of A left same + higher bits of hl 
        var newAValue = (byte)((ah << 4) + hll);

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newAValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, newAValue == 0);
        ClearFlag(Z80StatusFlags.HalfCarryH);
        SetParityFromValue(newAValue);
        ClearFlag(Z80StatusFlags.AddSubtractN);

        _memory[_hl.Word] = newHlValue;
        _af.High = newAValue;
    }
}