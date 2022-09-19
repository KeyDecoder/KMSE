using Kmse.Core.IO;
using Kmse.Core.Memory;
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

    private int _currentCycleCount;
    private IMasterSystemIoManager _io;
    private IMasterSystemMemory _memory;
    private bool _halted;

    private const int NopCycleCount = 4;

    private IZ80ProgramCounter _pc;
    private IZ80StackManager _stack;
    private IZ80FlagsManager _flags;
    private IZ80Accumulator _accumulator;
    private IZ80AfRegister _af;
    private IZ80BcRegister _bc;
    private IZ80DeRegister _de;
    private IZ80HlRegister _hl;
    private IZ808BitGeneralPurposeRegister _b;
    private IZ808BitGeneralPurposeRegister _c;
    private IZ808BitGeneralPurposeRegister _d;
    private IZ808BitGeneralPurposeRegister _e;
    private IZ808BitGeneralPurposeRegister _h;
    private IZ808BitGeneralPurposeRegister _l;
    private IZ80IndexRegisterXy _ix;
    private IZ80IndexRegisterXy _iy;
    private IZ80MemoryRefreshRegister _rRegister;
    private IZ80InterruptPageAddressRegister _iRegister;
    private IZ80CpuInputOutput _ioManagement;
    private IZ80CpuMemoryManagement _memoryManagement;

    /// <summary>
    /// Disables interrupts from being accepted if set to False
    /// </summary>
    private bool _interruptFlipFlop1;
    /// <summary>
    /// Temporary storage location for FF1 above
    /// </summary>
    private bool _interruptFlipFlop2;
    private byte _interruptMode;

    public Z80Cpu(ICpuLogger cpuLogger, IZ80InstructionLogger instructionLogger)
    {
        _cpuLogger = cpuLogger;
        _instructionLogger = instructionLogger;
        PopulateInstructions();
    }

    public void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io)
    {
        _cpuLogger.Debug("Initializing CPU");
        _memory = memory;
        _io = io;

        // TODO: Need to create these indirectly to allow mock interfaces to be injected for testing
        _af = new Z80AfRegister(memory);
        _bc = new Z80BcRegister(memory, _af.Flags);
        _de = new Z80DeRegister(memory, _af.Flags);
        _hl = new Z80HlRegister(memory, _af.Flags);
        _ix = new Z80IndexRegisterXy(memory, _af.Flags);
        _iy = new Z80IndexRegisterXy(memory, _af.Flags);
        _rRegister = new Z80MemoryRefreshRegister(memory, _af.Flags);
        _iRegister = new Z80InterruptPageAddressRegister(memory, _af.Flags);

        _accumulator = _af.Accumulator;
        _flags = _af.Flags;
        _b = _bc.B;
        _c = _bc.C;
        _d = _de.D;
        _e = _de.E;
        _h = _hl.H;
        _l = _hl.L;

        _stack = new Z80StackManager(memory, _cpuLogger, _af.Flags);
        _pc = new Z80ProgramCounter(memory, _instructionLogger, _flags, _stack);

        _ioManagement = new Z80CpuInputOutput(_io, _flags);
        _memoryManagement = new Z80CpuMemoryManagement(_memory, _flags);
    }

    public CpuStatus GetStatus()
    {
        return new CpuStatus
        {
            CurrentCycleCount = _currentCycleCount,
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
            InterruptFlipFlop1 = _interruptFlipFlop1,

            InterruptFlipFlop2 = _interruptFlipFlop2,
            InterruptMode = _interruptMode,

            NonMaskableInterruptStatus = _io.NonMaskableInterrupt,
            MaskableInterruptStatus = _io.MaskableInterrupt
        };
    }

    public void Reset()
    {
        _cpuLogger.Debug("Resetting CPU");
        _currentCycleCount = 0;

        _pc.Reset();
        _stack.Reset();

        _halted = false;
        _interruptFlipFlop1 = false;
        _interruptFlipFlop2 = false;
        _interruptMode = 0;

        _af.Reset();
        _bc.Reset();
        _de.Reset();
        _hl.Reset();
        _ix.Reset();
        _iy.Reset();
        _iRegister.Reset();
        _rRegister.Reset();

        _io.ClearNonMaskableInterrupt();
        _io.ClearMaskableInterrupt();

        _instructionLogger.StartNewInstruction(0x00);
    }

    public int ExecuteNextCycle()
    {
        _currentCycleCount = 0;
        _instructionLogger.StartNewInstruction(_pc.Value);

        if (_io.NonMaskableInterrupt)
        {
            _cpuLogger.LogInstruction(_pc.Value, "NMI", "Non Maskable Interrupt", "Non Maskable Interrupt", string.Empty);

            // Copy state of IFF1 into IFF2 to keep a copy and reset IFF1 so processing can continue without a masked interrupt occuring
            // This gets copied back with a RETN occurs
            _interruptFlipFlop2 = _interruptFlipFlop1;
            _interruptFlipFlop1 = false;

            // We have to clear this here to avoid this triggering in every cycle
            // but not sure this is accurate since if another NMI is triggered this could end up in an endless loop instead
            _io.ClearNonMaskableInterrupt();

            // Handle NMI by jumping to 0x66
            _pc.SetAndSaveExisting(0x66);

            // If halted then NMI starts it up again
            ResumeIfHalted();

            _currentCycleCount += 11;
            return _currentCycleCount;
        }

        if (_interruptFlipFlop1 && _io.MaskableInterrupt)
        {
            _cpuLogger.LogInstruction(_pc.Value, "MI", "Maskable Interrupt", "Maskable Interrupt", $"Mode {_interruptMode}");

            _interruptFlipFlop1 = false;
            _interruptFlipFlop2 = false;

            if (_interruptMode is 0 or 1)
            {
                // The SMS hardware generates two types of interrupts: IRQs and NMIs.
                // An IRQ is a maskable interrupt which may be generated by:
                // * the VSync impulse which occurs when a frame has been rasterised, or:
                // * a scanline counter falling below zero(see the VDP Register 10 description
                // for details)

                // For the SMS 2, Game Gear, and Genesis, the value $FF is always read from
                // the data bus, which corresponds to the instruction 'RST 38H'.
                // Basically mode 0 is the same as mode 1

                // Mode 1, jump to address 0x0038h
                _pc.SetAndSaveExisting(0x0038);
                _currentCycleCount += 11;

                return _currentCycleCount;
            }

            // Mode 2 is not used in SMS since ports don't set a byte on data bus
            //https://www.smspower.org/uploads/Development/richard.txt
            // Maybe it is used but with random values returned?
            //https://www.smspower.org/uploads/Development/smstech-20021112.txt
            _cpuLogger.Error("Maskable Interrupt while in mode 2 which is not supported");
            _currentCycleCount += NopCycleCount;
            _io.ClearMaskableInterrupt();
            return _currentCycleCount;
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

            _currentCycleCount += NopCycleCount;
            return _currentCycleCount;
        }

        instruction.Execute();
        // Note that -1 (DynamicCycleHandling) indicates the clock cycles change dynamically so handled inside the instruction handler
        if (instruction.ClockCycles > 0)
        {
            _currentCycleCount += instruction.ClockCycles;
        }

        _instructionLogger
            .SetOpCode(instruction.GetOpCode(), instruction.Name, instruction.Description)
            .Log();

        return _currentCycleCount;
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