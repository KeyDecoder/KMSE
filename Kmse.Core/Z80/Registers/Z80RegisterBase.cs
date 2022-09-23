using Kmse.Core.Utilities;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers;

public abstract class Z80RegisterBase
{
    protected readonly IZ80FlagsManager Flags;

    protected Z80RegisterBase(IZ80FlagsManager flags)
    {
        Flags = flags;
    }

    protected void RotateLeftCircular(ref byte register)
    {
        // Rotate register left by 1 bit, bit 7 is copied to carry flag and bit 0
        var newValue = (byte)(register << 1);
        var bit7Set = Bitwise.IsSet(register, 7);
        if (bit7Set)
        {
            // Copy bit 7 to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);

        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        register = newValue;
    }

    protected void RotateLeft(ref byte register)
    {
        // Rotate register left by 1 bit, bit 7 is copied to carry flag and carry flag copied to bit 0
        var newValue = (byte)(register << 1);
        var bit7Set = Bitwise.IsSet(register, 7);
        if (Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 0
            Bitwise.Set(ref newValue, 0);
        }

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit7Set);

        register = newValue;
    }

    protected void RotateRightCircular(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag and bit 7
        var newValue = (byte)(register >> 1);
        var bit0Set = Bitwise.IsSet(register, 0);
        if (bit0Set)
        {
            // Copy bit 0 to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        register = newValue;
    }

    protected void RotateRight(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag and carry flag copied to bit 7
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(register >> 1);
        var bit0Set = Bitwise.IsSet(register, 0);
        if (Flags.IsFlagSet(Z80StatusFlags.CarryC))
        {
            // Copy carry flag to bit 7
            Bitwise.Set(ref newValue, 7);
        }

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, bit0Set);

        register = newValue;
    }

    protected void ShiftLeftArithmetic(ref byte register)
    {
        // Shift register left by 1 bit, bit 7 is copied to carry flag
        var newValue = (byte)(register << 1);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 7));

        register = newValue;
    }

    protected void ShiftRightArithmetic(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag
        var newValue = (byte)(register >> 1);
        var bit7Set = Bitwise.IsSet(register, 7);

        // If bit 7 is set in original then leave as set even as we shift right
        if (bit7Set)
        {
            Bitwise.Set(ref newValue, 7);
        }

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 0));

        register = newValue;
    }

    protected void ShiftLeftLogical(ref byte register)
    {
        // Shift register left by 1 bit, bit 7 is copied to carry flag
        var newValue = (byte)(register << 1);

        // The difference between shift left logical and shift left arithmetic is this sets bit 0
        Bitwise.Set(ref newValue, 0);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 7));

        register = newValue;
    }

    protected void ShiftRightLogical(ref byte register)
    {
        // Rotate register right by 1 bit, bit 0 is copied to carry flag
        // This is special method since RRA flags are set differently to RR r instruction
        var newValue = (byte)(register >> 1);

        // The difference between shift right logical and shift right arithmetic is arithmetic always maintains bit 7 when shifting and logical shifting always clears bit 7
        Bitwise.Clear(ref newValue, 7);

        Flags.SetIfNegative(newValue);
        Flags.SetIfZero(newValue);
        Flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        Flags.SetParityFromValue(newValue);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
        Flags.SetClearFlagConditional(Z80StatusFlags.CarryC, Bitwise.IsSet(register, 0));

        register = newValue;
    }
}