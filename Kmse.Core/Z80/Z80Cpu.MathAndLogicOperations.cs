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
            factor |= 0x06 << 4;
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
        var bit = opCode & 0x38;
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
            case 0: Bitwise.Clear(ref _bc.High, bit); break;
            case 1: Bitwise.Clear(ref _bc.Low, bit); break;
            case 2: Bitwise.Clear(ref _de.High, bit); break;
            case 3: Bitwise.Clear(ref _de.Low, bit); break;
            case 4: Bitwise.Clear(ref _hl.High, bit); break;
            case 5: Bitwise.Clear(ref _hl.Low, bit); break;
            case 7: Bitwise.Clear(ref _af.High, bit); break;
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
        var bit = opCode & 0x38;
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
            case 0: Bitwise.Set(ref _bc.High, bit); break;
            case 1: Bitwise.Set(ref _bc.Low, bit); break;
            case 2: Bitwise.Set(ref _de.High, bit); break;
            case 3: Bitwise.Set(ref _de.Low, bit); break;
            case 4: Bitwise.Set(ref _hl.High, bit); break;
            case 5: Bitwise.Set(ref _hl.Low, bit); break;
            case 7: Bitwise.Set(ref _af.High, bit); break;
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
        var bit = opCode & 0x38;
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
            _ => 0
        };
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, !Bitwise.IsSet(valueToCheck, bit));
        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    private void TestBitByRegisterLocation(Z80Register register, int bit, int offset)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, !Bitwise.IsSet(value, bit));
        ClearFlag(Z80StatusFlags.HalfCarryH);
        ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    private void AddValueAtRegisterMemoryLocationTo8BitRegister(Z80Register register, int offset, ref byte destination, bool checkFlags = false)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        AddValueTo8BitRegister(value, ref destination, checkFlags);
    }

    private void AddValueTo8BitRegister(byte value, ref byte destination, bool checkFlags = false)
    {
        int newValue = destination + value;

        if (!checkFlags)
        {
            destination = (byte)newValue;
            return;
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (destination & 0x0F) + (newValue & 0x0F) > 0x0F);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, ((value ^ destination ^ 0x80) & (destination ^ newValue) & 0x80) != 0);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, newValue > 0xFF);

        destination = (byte)newValue;
    }

    private void Add16BitRegisterTo16BitRegister(Z80Register source, ref Z80Register destination,
        bool checkFlags = false)
    {
        int newValue = destination.Word + source.Word;

        if (!checkFlags)
        {
            destination.Word = (ushort)newValue;
            return;
        }

        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (destination.Word & 0xFFF) + (newValue & 0xFFF) > 0xFFF);
        ClearFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, newValue > 0xFFFF);

        destination.Word = (ushort)newValue;
    }

    private void SubtractValueAtRegisterMemoryLocationFrom8BitRegister(Z80Register register, int offset, ref byte destination, bool checkFlags = false)
    {
        var value = _memory[(ushort)(register.Word + offset)];
        SubtractValueFrom8BitRegister(value, ref destination, checkFlags);
    }

    private void SubtractValueFrom8BitRegister(byte value, ref byte destination, bool checkFlags = false)
    {
        int newValue = destination - value;

        if (!checkFlags)
        {
            destination = (byte)newValue;
            return;
        }

        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, (newValue & 0xFF) == 0);
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((destination ^ newValue ^ value) & 0x10) != 0);
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, (((value ^ destination) & (destination ^ newValue) & 0x80) != 0));
        SetFlag(Z80StatusFlags.AddSubtractN);
        SetClearFlagConditional(Z80StatusFlags.CarryC, newValue < 0);

        destination = (byte)newValue;
    }
}