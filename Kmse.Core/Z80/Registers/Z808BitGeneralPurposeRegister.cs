using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers;

public class Z808BitGeneralPurposeRegister : Z808BitRegister, IZ808BitGeneralPurposeRegister
{
    public Z808BitGeneralPurposeRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }

    public void Increment()
    {
        var oldValue = Value;
        var newValue = (byte)(Value + 1);
        Set(newValue);
        CheckIncrementFlags(newValue, oldValue);
    }

    public void Decrement()
    {
        var oldValue = Value;
        var newValue = (byte)(Value - 1);
        Set(newValue);
        CheckDecrementFlags(newValue, oldValue);
    }

    public void DecrementNoFlags()
    {
        var newValue = (byte)(Value - 1);
        Set(newValue);
    }

    public void ClearBit(int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to reset");
        }

        Bitwise.Clear(ref InternalValue, bit);
    }

    public void SetBit(int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to reset");
        }

        Bitwise.Set(ref InternalValue, bit);
    }

    public void RotateLeftCircular()
    {
        RotateLeftCircular(ref InternalValue);
    }

    public void RotateLeft()
    {
        RotateLeft(ref InternalValue);
    }

    public void RotateRightCircular()
    {
        RotateRightCircular(ref InternalValue);
    }

    public void RotateRight()
    {
        RotateRight(ref InternalValue);
    }

    public void ShiftLeftArithmetic()
    {
        ShiftLeftArithmetic(ref InternalValue);
    }

    public void ShiftRightArithmetic()
    {
        ShiftRightArithmetic(ref InternalValue);
    }

    public void ShiftLeftLogical()
    {
        ShiftLeftLogical(ref InternalValue);
    }

    public void ShiftRightLogical()
    {
        ShiftRightLogical(ref InternalValue);
    }

    private void CheckIncrementFlags(byte newValue, byte oldValue)
    {
        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        // Set half carry is carry from bit 3
        // Basically if all 4 lower bits are set, then incrementing means it would set bit 5 which in the high nibble
        // https://en.wikipedia.org/wiki/Half-carry_flag
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x0F);
        Flags.SetIfIncrementOverflow(oldValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    private void CheckDecrementFlags(byte newValue, byte oldValue)
    {
        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        // Set half carry is borrow from bit 4
        // Basically if all 4 lower bits are clear, then decrementing would essentially set all the lower bits
        // ie. 0x20 - 1 = 0x1F
        // https://en.wikipedia.org/wiki/Half-carry_flag
        // This could also check by seeing if the new value & 0x0F == 0x0F means all the lower bits were set
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x00);
        Flags.SetIfDecrementOverflow(oldValue);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);
    }
}