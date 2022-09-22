using Kmse.Core.Utilities;
using Kmse.Core.Z80.Model;

namespace Kmse.Core.Z80.Registers.General;

public class Z80FlagsManager : IZ80FlagsManager
{
    protected byte InternalShadowValue;
    protected byte InternalValue;

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
    }

    public void Set(byte value)
    {
        Value = value;
    }

    public void SwapWithShadow()
    {
        (Value, ShadowValue) = (ShadowValue, Value);
    }

    public void SetFlag(Z80StatusFlags flags)
    {
        Value |= (byte)flags;
    }

    public void ClearFlag(Z80StatusFlags flags)
    {
        Value &= (byte)~flags;
    }

    public void InvertFlag(Z80StatusFlags flag)
    {
        SetClearFlagConditional(flag, !IsFlagSet(flag));
    }

    public void SetClearFlagConditional(Z80StatusFlags flags, bool condition)
    {
        if (condition)
        {
            SetFlag(flags);
        }
        else
        {
            ClearFlag(flags);
        }
    }

    public bool IsFlagSet(Z80StatusFlags flags)
    {
        var currentSetFlags = (Z80StatusFlags)Value & flags;
        return currentSetFlags == flags;
    }

    public void SetParityFromValue(byte value)
    {
        // Count the number of 1 bits in the value
        // If odd, then clear flag and if even, then set flag
        var bitsSet = 0;
        for (var i = 0; i < 8; i++)
        {
            if (Bitwise.IsSet(value, i))
            {
                bitsSet++;
            }
        }

        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, bitsSet == 0 || bitsSet % 2 == 0);
    }

    public void SetIfZero(byte value)
    {
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, value == 0);
    }

    public void SetIfZero(ushort value)
    {
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, value == 0);
    }

    public void SetIfZero(int value)
    {
        SetClearFlagConditional(Z80StatusFlags.ZeroZ, value == 0);
    }

    public void SetIfNegative(byte value)
    {
        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(value, 7));
    }

    public void SetIfNegative(ushort value)
    {
        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(value, 15));
    }

    public void SetIfNegative(int twosComplementValue)
    {
        SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(twosComplementValue, 7));
    }

    public void SetIfIncrementOverflow(byte value)
    {
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, value == 0x7F);
    }

    public void SetIfDecrementOverflow(byte value)
    {
        SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, value == 0x80);
    }

    public void SetIfHalfCarry(byte currentValue, byte operand, int changedValue)
    {
        // Half carry occurs if the result of subtracting the higher nibbles means it will set the next lower bit (basically underflows)
        // We check if the subtraction means that adding higher nibbles sets bit 3
        // This is then combined with the DAA instruction which adjusts the result to get the valid value
        //https://retrocomputing.stackexchange.com/questions/4693/why-does-the-z80-have-a-half-carry-bit
        SetClearFlagConditional(Z80StatusFlags.HalfCarryH, ((currentValue ^ changedValue ^ operand) & 0x10) != 0);
    }
}