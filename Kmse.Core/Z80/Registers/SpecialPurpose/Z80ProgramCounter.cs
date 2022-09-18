using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80ProgramCounter : Z8016BitRegisterBase, IZ80ProgramCounter
{
    private readonly IZ80FlagsManager _flags;
    private readonly IMasterSystemMemory _memory;
    private readonly IZ80StackManager _stack;
    private readonly IZ80InstructionLogger _z80InstructionLogger;
    private Z80Register _pc;

    public Z80ProgramCounter(IMasterSystemMemory memory, IZ80InstructionLogger z80InstructionLogger,
        IZ80FlagsManager flags, IZ80StackManager stack)
        : base(memory)
    {
        _memory = memory;
        _z80InstructionLogger = z80InstructionLogger;
        _flags = flags;
        _stack = stack;
    }

    public byte GetNextInstruction()
    {
        return GetNextByteByProgramCounter();
    }

    public byte GetNextDataByte()
    {
        var data = GetNextByteByProgramCounter();
        _z80InstructionLogger.AddOperationData(data);
        return data;
    }

    public ushort GetNextTwoDataBytes()
    {
        ushort data = GetNextByteByProgramCounter();
        data += (ushort)(GetNextByteByProgramCounter() << 8);
        _z80InstructionLogger.AddOperationData(data);
        return data;
    }

    /// <summary>
    ///     Move program counter forward by the provided value
    /// </summary>
    /// <param name="offset">Add this offset to the current PC</param>
    public void MoveProgramCounterForward(ushort offset)
    {
        // If this goes above ushort max, we assume that when PC hits the limit it just wraps around instead of throwing an error or failing
        _pc.Word += offset;
    }

    /// <summary>
    ///     Move program counter backward by the provided value
    /// </summary>
    /// <param name="offset">Subtract this offset from the current PC</param>
    public void MoveProgramCounterBackward(ushort offset)
    {
        // If this goes below zero/negative, we assume that when PC hits -1 it just wraps around instead of throwing an error or failing
        _pc.Word -= offset;
    }

    public void SetAndSaveExisting(ushort address)
    {
        // Storing PC in Stack so can resume later
        _stack.PushRegisterToStack(this);

        // Update PC to execute from new address
        Set(address);
    }

    public void Set(Z80Register register)
    {
        // Update PC to execute from the value of the register
        Set(register.Word);
    }

    public void SetFromStack()
    {
        var register = new Z80Register();
        _stack.PopRegisterFromStack(this);
        Set(register.Word);
    }

    public bool Jump16BitIfFlagCondition(Z80StatusFlags flag, ushort address)
    {
        if (_flags.IsFlagSet(flag))
        {
            Set(address);
            return true;
        }

        return false;
    }

    public bool Jump16BitIfNotFlagCondition(Z80StatusFlags flag, ushort address)
    {
        if (!_flags.IsFlagSet(flag))
        {
            Set(address);
            return true;
        }

        return false;
    }

    public void JumpByOffset(byte offset)
    {
        var newPcLocation = _pc.Word;

        // Range is -126 to +129 so we need a signed version
        // However sbyte only goes from -128 to +127 but we need -126 to +129 so have to do this manually
        if (offset <= 129)
        {
            newPcLocation += offset;
        }
        else
        {
            // 256 minus our offset gives us a positive number for where it would rollover at 129
            // And we minus this since this would be negative number
            newPcLocation -= (ushort)(256 - offset);
        }

        // Note we don't need to add one here since we always increment the Pc after we read from it, so it's already pointing to next command at this point
        Set(newPcLocation);
    }

    public bool JumpByOffsetIfFlagHasStatus(Z80StatusFlags flag, byte offset, bool status)
    {
        if (_flags.IsFlagSet(flag) == status)
        {
            JumpByOffset(offset);
            return true;
            //_currentCycleCount += 12;
        }

        //_currentCycleCount += 7;
        return false;
    }

    public bool JumpByOffsetIfFlag(Z80StatusFlags flag, byte offset)
    {
        return JumpByOffsetIfFlagHasStatus(flag, offset, true);
    }

    public bool JumpByOffsetIfNotFlag(Z80StatusFlags flag, byte offset)
    {
        return JumpByOffsetIfFlagHasStatus(flag, offset, false);
    }

    public bool CallIfFlagCondition(Z80StatusFlags flag, ushort address)
    {
        if (_flags.IsFlagSet(flag))
        {
            SetAndSaveExisting(address);
            return true;
        }

        return false;
    }

    public bool CallIfNotFlagCondition(Z80StatusFlags flag, ushort address)
    {
        if (!_flags.IsFlagSet(flag))
        {
            SetAndSaveExisting(address);
            return true;
        }

        return false;
    }

    public bool ReturnIfFlagHasStatus(Z80StatusFlags flag, bool status)
    {
        if (_flags.IsFlagSet(flag) == status)
        {
            SetFromStack();
            return true;
            //_currentCycleCount += 11;
        }

        //_currentCycleCount += 5;
        return false;
    }

    public bool ReturnIfFlag(Z80StatusFlags flag)
    {
        return ReturnIfFlagHasStatus(flag, true);
    }

    public bool ReturnIfNotFlag(Z80StatusFlags flag)
    {
        return ReturnIfFlagHasStatus(flag, false);
    }

    private byte GetNextByteByProgramCounter()
    {
        // Note: We don't increment the cycle count here since this operation is included in overall cycle count for each instruction
        var data = _memory[_pc.Word];
        _pc.Word++;
        return data;
    }
}