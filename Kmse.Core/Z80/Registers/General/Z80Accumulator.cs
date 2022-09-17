using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.General;

public class Z80Accumulator : Z808BitRegister, IZ80Accumulator
{
    private readonly IZ80FlagsManager _flags;

    public Z80Accumulator(IZ80FlagsManager flags, IMasterSystemMemory memory)
        : base(memory)
    {
        _flags = flags;
    }

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

        _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newAValue, 7));
        _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newAValue == 0);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetParityFromValue(newAValue);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);

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

        _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newAValue, 7));
        _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newAValue == 0);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetParityFromValue(newAValue);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);

        Memory[hl.Value] = newHlValue;
        Value = newAValue;
    }

    private void LoadSpecial8BitRegisterToAccumulator(byte sourceData, bool interruptFlipFlop2Status)
    {
        Value = sourceData;

        // Check flags since copying from special register into accumulator
        _flags.SetClearFlagConditional(Z80StatusFlags.SignS, !Bitwise.IsSet(sourceData, 7));
        _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, sourceData == 0);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, interruptFlipFlop2Status);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }
}