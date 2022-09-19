using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;

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

    // TODO: Add generic methods for setting flags for sign, zero, carry to cut down on duplication as much as possible etc
}