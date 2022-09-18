﻿using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public class Z808BitGeneralPurposeRegister : Z808BitRegister, IZ808BitGeneralPurposeRegister
{
    protected readonly IZ80FlagsManager Flags;

    public Z808BitGeneralPurposeRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory)
    {
        Flags = flags;
    }

    public void Increment()
    {
        var oldValue = Value;
        var newValue = (byte)(Value + 1);
        Set(newValue);
        CheckIncrementFlags(newValue, oldValue);
    }

    private void CheckIncrementFlags(byte newValue, byte oldValue)
    {
        Flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        Flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
        // Set half carry is carry from bit 3
        // Basically if all 4 lower bits are set, then incrementing means it would set bit 5 which in the high nibble
        // https://en.wikipedia.org/wiki/Half-carry_flag
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x0F);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x7F);
        Flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    public void Decrement()
    {
        var oldValue = Value;
        var newValue = (byte)(Value - 1);
        Set(newValue);
        CheckDecrementFlags(newValue, oldValue);
    }

    private void CheckDecrementFlags(byte newValue, byte oldValue)
    {
        Flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
        Flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
        // Set half carry is borrow from bit 4
        // Basically if all 4 lower bits are clear, then decrementing would essentially set all the lower bits
        // ie. 0x20 - 1 = 0x1F
        // https://en.wikipedia.org/wiki/Half-carry_flag
        // This could also check by seeing if the new value & 0x0F == 0x0F means all the lower bits were set
        Flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x00);
        Flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x80);
        Flags.SetFlag(Z80StatusFlags.AddSubtractN);
    }
}