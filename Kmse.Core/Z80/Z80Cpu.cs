using System.Text;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers;
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

    private Z80Register _af, _bc, _de, _hl;
    private Z80Register _afShadow, _bcShadow, _deShadow, _hlShadow;
    private Z80Register _ix, _iy;
    private byte _iRegister, _rRegister;

    private IZ80ProgramCounter _pc;
    private IZ80StackManager _stack;

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
        _af = new Z80Register();
        _bc = new Z80Register();
        _de = new Z80Register();
        _hl = new Z80Register();
        _afShadow = new Z80Register();
        _bcShadow = new Z80Register();
        _deShadow = new Z80Register();
        _hlShadow = new Z80Register();
        _ix = new Z80Register();
        _iy = new Z80Register();
        _iRegister = 0;
        _rRegister = 0;
        PopulateInstructions();
    }

    public void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io)
    {
        _cpuLogger.Debug("Initializing CPU");
        _memory = memory;
        _io = io;

        // TODO: Need to create these indirectly to allow mock interfaces to be injected for testing
        _pc = new Z80ProgramCounter(memory, _instructionLogger);
        _stack = new Z80StackManager(memory, _cpuLogger);
    }

    public CpuStatus GetStatus()
    {
        return new CpuStatus
        {
            CurrentCycleCount = _currentCycleCount,
            Halted = _halted,

            Af = _af,
            Bc = _bc,
            De = _de,
            Hl = _hl,
            AfShadow = _afShadow,
            BcShadow = _bcShadow,
            DeShadow = _deShadow,
            HlShadow = _hlShadow,
            Ix = _ix,
            Iy = _iy,
            Pc = _pc.GetValue(),
            StackPointer = _stack.GetValue(),
            IRegister = _iRegister,
            RRegister = _rRegister,
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

        _iRegister = 0;
        _rRegister = 0;

        _af.Word = 0x00;
        _bc.Word = 0x00;
        _de.Word = 0x00;
        _hl.Word = 0x00;

        _afShadow.Word = 0x00;
        _bcShadow.Word = 0x00;
        _deShadow.Word = 0x00;
        _hlShadow.Word = 0x00;

        _io.ClearNonMaskableInterrupt();
        _io.ClearMaskableInterrupt();

        _instructionLogger.StartNewInstruction(0x00);
    }

    public int ExecuteNextCycle()
    {
        _currentCycleCount = 0;
        _instructionLogger.StartNewInstruction(_pc.GetValue());

        if (_io.NonMaskableInterrupt)
        {
            _cpuLogger.LogInstruction(_pc.GetValue(), "NMI", "Non Maskable Interrupt", "Non Maskable Interrupt", string.Empty);

            // Copy state of IFF1 into IFF2 to keep a copy and reset IFF1 so processing can continue without a masked interrupt occuring
            // This gets copied back with a RETN occurs
            _interruptFlipFlop2 = _interruptFlipFlop1;
            _interruptFlipFlop1 = false;

            // We have to clear this here to avoid this triggering in every cycle
            // but not sure this is accurate since if another NMI is triggered this could end up in an endless loop instead
            _io.ClearNonMaskableInterrupt();

            // Handle NMI by jumping to 0x66
            SaveAndUpdateProgramCounter(0x66);

            // If halted then NMI starts it up again
            _halted = false;

            _currentCycleCount += 11;
            return _currentCycleCount;
        }

        if (_interruptFlipFlop1 && _io.MaskableInterrupt)
        {
            _cpuLogger.LogInstruction(_pc.GetValue(), "MI", "Maskable Interrupt", "Maskable Interrupt", $"Mode {_interruptMode}");

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
                SaveAndUpdateProgramCounter(0x0038);
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
            0xCB => ProcessCBOpCode(CbInstructionModes.Normal),
            0xDD => ProcessDDOpCode(),
            0xFD => ProcessFDOpCode(),
            0xED => ProcessEDOpCode(),
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

    private Instruction ProcessGenericNonPrefixedOpCode(byte opCode)
    {
        if (!_genericInstructions.TryGetValue(opCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled instruction - {opCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessCBOpCode(CbInstructionModes mode)
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

    private Instruction ProcessDDOpCode()
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var secondOpCode = _pc.GetNextInstruction();
        if (secondOpCode == 0xCB)
        {
            // Not a normal instruction, but a special instruction
            // ie. DD CB data opcode
            return ProcessCBOpCode(CbInstructionModes.DD);
        }

        if (!_ddInstructions.TryGetValue(secondOpCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled 0xDD instruction - {secondOpCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessFDOpCode()
    {
        // Two byte op code, so get next part of instruction and use that to lookup instruction
        var secondOpCode = _pc.GetNextInstruction();
        if (secondOpCode == 0xCB)
        {
            // Not a normal instruction, but a special instruction
            // ie. FD CB data opcode
            return ProcessCBOpCode(CbInstructionModes.FD);
        }
        if (!_fdInstructions.TryGetValue(secondOpCode, out var instruction))
        {
            _cpuLogger.Error($"Unhandled 0xFD instruction - {secondOpCode:X2}");
        }

        return instruction;
    }

    private Instruction ProcessEDOpCode()
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