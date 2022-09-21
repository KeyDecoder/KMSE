using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80;

public partial class Z80Cpu : IZ80Cpu
{
    private readonly ICpuLogger _cpuLogger;
    private readonly IZ80InstructionLogger _instructionLogger;
    private readonly IMasterSystemIoManager _io;
    private readonly IMasterSystemMemory _memory;
    private readonly IZ80ProgramCounter _pc;
    private readonly IZ80StackManager _stack;
    private readonly IZ80FlagsManager _flags;
    private readonly IZ80Accumulator _accumulator;
    private readonly IZ80AfRegister _af;
    private readonly IZ80BcRegister _bc;
    private readonly IZ80DeRegister _de;
    private readonly IZ80HlRegister _hl;
    private readonly IZ808BitGeneralPurposeRegister _b;
    private readonly IZ808BitGeneralPurposeRegister _c;
    private readonly IZ808BitGeneralPurposeRegister _d;
    private readonly IZ808BitGeneralPurposeRegister _e;
    private readonly IZ808BitGeneralPurposeRegister _h;
    private readonly IZ808BitGeneralPurposeRegister _l;
    private readonly IZ80IndexRegisterXy _ix;
    private readonly IZ80IndexRegisterXy _iy;
    private readonly IZ80MemoryRefreshRegister _rRegister;
    private readonly IZ80InterruptPageAddressRegister _iRegister;
    private readonly IZ80CpuInputOutput _ioManagement;
    private readonly IZ80CpuMemoryManagement _memoryManagement;
    private readonly IZ80InterruptManagement _interruptManagement;
    private readonly IZ80CpuCycleCounter _cycleCounter;

    private bool _halted;
    private const int NopCycleCount = 4;

    public Z80Cpu(IMasterSystemMemory memory, IMasterSystemIoManager io, ICpuLogger cpuLogger, IZ80InstructionLogger instructionLogger, Z80CpuRegisters registers, Z80CpuManagement cpuManagement)
    {
        _cpuLogger = cpuLogger;
        _instructionLogger = instructionLogger;

        _cpuLogger.Debug("Initializing CPU");
        _memory = memory;
        _io = io;

        _af = registers.Af;
        _bc = registers.Bc;
        _de = registers.De;
        _hl = registers.Hl;
        _ix = registers.IX;
        _iy = registers.IY;
        _rRegister = registers.R;
        _iRegister = registers.I;

        _accumulator = _af.Accumulator;
        _flags = _af.Flags;
        _b = _bc.B;
        _c = _bc.C;
        _d = _de.D;
        _e = _de.E;
        _h = _hl.H;
        _l = _hl.L;

        _stack = registers.Stack;
        _pc = registers.Pc;

        _ioManagement = cpuManagement.IoManagement;
        _memoryManagement = cpuManagement.MemoryManagement;
        _interruptManagement = cpuManagement.InterruptManagement;
        _cycleCounter = cpuManagement.CycleCounter;

        PopulateInstructions();
    }

    public CpuStatus GetStatus()
    {
        return new CpuStatus
        {
            CurrentCycleCount = _cycleCounter.CurrentCycleCount,
            Halted = _halted,

            Af = _af.AsRegister(),
            Bc = _bc.AsRegister(),
            De = _de.AsRegister(),
            Hl = _hl.AsRegister(),
            AfShadow = _af.ShadowAsRegister(),
            BcShadow = _bc.ShadowAsRegister(),
            DeShadow = _de.ShadowAsRegister(),
            HlShadow = _hl.ShadowAsRegister(),
            Ix = _ix.AsRegister(),
            Iy = _iy.AsRegister(),
            Pc = _pc.Value,
            StackPointer = _stack.Value,
            IRegister = _iRegister.Value,
            RRegister = _rRegister.Value,
            InterruptFlipFlop1 = _interruptManagement.InterruptEnableFlipFlopStatus,

            InterruptFlipFlop2 = _interruptManagement.InterruptEnableFlipFlopTempStorageStatus,
            InterruptMode = _interruptManagement.InterruptMode,

            NonMaskableInterruptStatus = _interruptManagement.NonMaskableInterrupt,
            MaskableInterruptStatus = _interruptManagement.MaskableInterrupt
        };
    }

    public void Reset()
    {
        _cpuLogger.Debug("Resetting CPU");

        _cycleCounter.Reset();

        _pc.Reset();
        _stack.Reset();
        _interruptManagement.Reset();

        _halted = false;

        _af.Reset();
        _bc.Reset();
        _de.Reset();
        _hl.Reset();
        _ix.Reset();
        _iy.Reset();
        _iRegister.Reset();
        _rRegister.Reset();

        _instructionLogger.StartNewInstruction(0x00);
    }

    public int ExecuteNextCycle()
    {
        _cycleCounter.Reset();
        _instructionLogger.StartNewInstruction(_pc.Value);

        if (_interruptManagement.InterruptWaiting())
        {
            var cycles = _interruptManagement.ProcessInterrupts();
            _cycleCounter.Increment(cycles);
            // If halted and an interrupt occurs, then resume
            ResumeIfHalted();
            return _cycleCounter.CurrentCycleCount;
        }

        if (_halted)
        {
            // NOP until interrupt
            return NopCycleCount;
        }

        var opCode = _pc.GetNextInstruction();
        var instruction = opCode switch
        {
            0xCB => ProcessCbOpCode(CbInstructionModes.Normal),
            0xDD => ProcessDdOpCode(),
            0xFD => ProcessFdOpCode(),
            0xED => ProcessEdOpCode(),
            _ => ProcessGenericNonPrefixedOpCode(opCode)
        };

        if (instruction == null)
        {
            // Unhandled instruction, just do a NOP
            _instructionLogger
                .SetOpCode(opCode.ToString("X2"), "Unimplemented Instruction", "Unimplemented Instruction")
                .Log();

            _cycleCounter.Increment(NopCycleCount);
            return _cycleCounter.CurrentCycleCount;
        }

        instruction.Execute();
        // Note that -1 (DynamicCycleHandling) indicates the clock cycles change dynamically so handled inside the instruction handler
        if (instruction.ClockCycles > 0)
        {
            _cycleCounter.Increment(instruction.ClockCycles);
        }

        _instructionLogger
            .SetOpCode(instruction.GetOpCode(), instruction.Name, instruction.Description)
            .Log();

        return _cycleCounter.CurrentCycleCount;
    }

    private void Halt()
    {
        _cpuLogger.Debug("Halting CPU");
        _halted = true;
    }

    private void ResumeIfHalted()
    {
        if (_halted)
        {
            _cpuLogger.Debug("Resuming CPU");
        }
        _halted = false;
    }

    private Instruction ProcessGenericNonPrefixedOpCode(byte opCode)
    {
        if (!_genericInstructions.TryGetValue(opCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled instruction - {opCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessCbOpCode(CbInstructionModes mode)
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var opCode = _pc.GetNextInstruction();

        // Normal CB instruction, just do a lookup for the instruction
        if (mode == CbInstructionModes.Normal)
        {
            if (!_cbInstructions.TryGetValue(opCode, out var instruction))
            {
                _cpuLogger.Error($"Unhandled 0xCB instruction - {opCode:X2}");
            }

            return instruction;
        }
        
        // Special instruction which is actually 4 bytes and has started with a different prefix op code
        // FD/DD CB XX OpCode

        // We need to read another byte which is the actual op code we use to lookup since third byte is data
        var fourthOpCode = _pc.GetNextInstruction();
        SpecialCbInstruction specialCbInstruction;
        bool foundInstruction;
        switch (mode)
        {
            case CbInstructionModes.DD: foundInstruction = _specialDdcbInstructions.TryGetValue(fourthOpCode, out specialCbInstruction); break;
            case CbInstructionModes.FD: foundInstruction = _specialFdcbInstructions.TryGetValue(fourthOpCode, out specialCbInstruction); break;
            default:
            {
                _cpuLogger.Error($"Unhandled CB instruction mode 0x{mode} - Second Op Code {opCode:X2}, Third Op Code {fourthOpCode:X2}");
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        if (!foundInstruction)
        {
            _cpuLogger.Error($"Unhandled 0x{mode} 0xCB instruction - {opCode:X2}");
            return null;
        }

        // Add data byte which is byte read after the 0xCB
        specialCbInstruction.SetDataByte(opCode);
        return specialCbInstruction;
    }

    private Instruction ProcessDdOpCode()
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var secondOpCode = _pc.GetNextInstruction();
        if (secondOpCode == 0xCB)
        {
            // Not a normal instruction, but a special instruction
            // ie. DD CB data opcode
            return ProcessCbOpCode(CbInstructionModes.DD);
        }

        if (!_ddInstructions.TryGetValue(secondOpCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled 0xDD instruction - {secondOpCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessFdOpCode()
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var secondOpCode = _pc.GetNextInstruction();
        if (secondOpCode == 0xCB)
        {
            // Not a normal instruction, but a special instruction
            // ie. FD CB data opcode
            return ProcessCbOpCode(CbInstructionModes.FD);
        }
        if (!_fdInstructions.TryGetValue(secondOpCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled 0xFD instruction - {secondOpCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessEdOpCode()
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var secondOpCode = _pc.GetNextInstruction();
        if (!_edInstructions.TryGetValue(secondOpCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled 0xED instruction - {secondOpCode:X2}");
        }

        return instruction;
    }
}