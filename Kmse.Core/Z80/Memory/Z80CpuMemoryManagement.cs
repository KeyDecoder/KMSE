using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Memory;

public class Z80CpuMemoryManagement : IZ80CpuMemoryManagement
{
    private readonly IZ80FlagsManager _flags;
    private readonly IMasterSystemMemory _memory;

    public Z80CpuMemoryManagement(IMasterSystemMemory memory, IZ80FlagsManager flags)
    {
        _memory = memory;
        _flags = flags;
    }

    public void CopyMemory(IZ8016BitRegister source, IZ8016BitRegister destination)
    {
        _memory[destination.Value] = _memory[source.Value];
    }

    public byte ReadFromMemory(IZ8016BitRegister register, byte offset = 0)
    {
        return _memory[(ushort)(register.Value + offset)];
    }

    public void WriteToMemory(IZ8016BitRegister register, byte value, byte offset = 0)
    {
        _memory[(ushort)(register.Value + offset)] = value;
    }

    public void IncrementMemory(IZ8016BitRegister register, byte offset = 0)
    {
        var address = (ushort)(register.Value + offset);
        var value = _memory[address];
        var newValue = (byte)(value + 1);

        _memory[address] = newValue;

        _flags.SetIfNegative(newValue);
        _flags.SetIfZero(newValue);
        // Set half carry is carry from bit 3
        // Basically if all 4 lower bits are set, then incrementing means it would set bit 5 which in the high nibble
        // https://en.wikipedia.org/wiki/Half-carry_flag
        _flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (value & 0x0F) == 0x0F);
        _flags.SetIfIncrementOverflow(value);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    public void DecrementMemory(IZ8016BitRegister register, byte offset = 0)
    {
        var address = (ushort)(register.Value + offset);
        var value = _memory[address];
        var newValue = (byte)(value - 1);

        _memory[address] = newValue;

        _flags.SetIfNegative(newValue);
        _flags.SetIfZero(newValue);

        // Set half carry is borrow from bit 4
        // Basically if all 4 lower bits are clear, then decrementing would essentially set all the lower bits
        // ie. 0x20 - 1 = 0x1F
        // https://en.wikipedia.org/wiki/Half-carry_flag
        // This could also check by seeing if the new value & 0x0F == 0x0F means all the lower bits were set
        _flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (value & 0x0F) == 0x00);
        _flags.SetIfDecrementOverflow(value);
        _flags.SetFlag(Z80StatusFlags.AddSubtractN);
    }
}