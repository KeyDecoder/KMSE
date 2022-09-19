﻿using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.General;

public class Z80AfRegister : IZ80AfRegister
{
    private readonly IMasterSystemMemory _memory;

    public Z80AfRegister(IMasterSystemMemory memory)
    {
        _memory = memory;
        Flags = new Z80FlagsManager();
        Accumulator = new Z80Accumulator(Flags, memory);
    }

    public IZ80Accumulator Accumulator { get; }
    public IZ80FlagsManager Flags { get; }

    public ushort Value => (ushort)(Flags.Value + (Accumulator.Value << 8));
    public byte High => Accumulator.Value;
    public byte Low => Flags.Value;
    public ushort ShadowValue => (ushort)(Flags.ShadowValue + (Accumulator.ShadowValue << 8));

    public void Reset()
    {
        Flags.Reset();
        Accumulator.Reset();
    }

    // TODO: Remove any methods not used, since mixed with flags, very few operations act on AF as a 16 bit register

    public void Set(ushort value)
    {
        var (high, low) = Bitwise.ToBytes(value);
        Flags.Set(low);
        Accumulator.Set(high);
    }

    public void Set(IZ8016BitRegister register)
    {
        Set(register.Value);
    }

    public void SetHigh(byte value)
    {
        Accumulator.Set(value);
    }

    public void SetLow(byte value)
    {
        Flags.Set(value);
    }

    public void SetFromDataInMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        var low = _memory[location];
        location++;
        var high = _memory[location];

        Flags.Set(low);
        Accumulator.Set(high);
    }

    public void SetFromDataInMemory(IZ8016BitRegister register, byte offset = 0)
    {
        SetFromDataInMemory(register.Value, offset);
    }

    public void SaveToMemory(ushort address, byte offset = 0)
    {
        var location = (ushort)(address + offset);
        _memory[location] = Accumulator.Value;
        _memory[(ushort)(location + 1)] = Flags.Value;
    }

    public void SwapWithShadow()
    {
        Flags.SwapWithShadow();
        Accumulator.SwapWithShadow();
    }

    public Z80Register AsRegister()
    {
        return new Z80Register
        {
            Low = Flags.Value,
            High = Accumulator.Value
        };
    }

    public Z80Register ShadowAsRegister()
    {
        return new Z80Register
        {
            Low = Flags.ShadowValue,
            High = Accumulator.ShadowValue
        };
    }
}