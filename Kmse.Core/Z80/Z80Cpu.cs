using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Running;

namespace Kmse.Core.Z80;

public class Z80Cpu : IZ80Cpu
{
    private const int NopCycleCount = 4;
    private readonly IZ80AfRegister _af;
    private readonly IZ80BcRegister _bc;
    private readonly ICpuLogger _cpuLogger;
    private readonly IZ80CpuCycleCounter _cycleCounter;
    private readonly IZ80DeRegister _de;
    private readonly IZ80HlRegister _hl;
    private readonly IZ80InstructionLogger _instructionLogger;
    private readonly IZ80CpuInstructions _instructions;
    private readonly IZ80InterruptManagement _interruptManagement;
    private readonly IZ80InterruptPageAddressRegister _iRegister;
    private readonly IZ80IndexRegisterXy _ix;
    private readonly IZ80IndexRegisterXy _iy;
    private readonly IZ80ProgramCounter _pc;
    private readonly IZ80MemoryRefreshRegister _rRegister;
    private readonly IZ80CpuRunningStateManager _runningStateManager;
    private readonly IZ80StackManager _stack;

    public Z80Cpu(IMasterSystemMemory memory, IMasterSystemIoManager io, ICpuLogger cpuLogger,
        IZ80InstructionLogger instructionLogger, IZ80CpuInstructions instructions, Z80CpuRegisters registers, Z80CpuManagement cpuManagement)
    {
        _cpuLogger = cpuLogger;
        _instructionLogger = instructionLogger;

        _cpuLogger.Debug("Initializing CPU");

        _af = registers.Af;
        _bc = registers.Bc;
        _de = registers.De;
        _hl = registers.Hl;
        _ix = registers.IX;
        _iy = registers.IY;
        _rRegister = registers.R;
        _iRegister = registers.I;

        _stack = registers.Stack;
        _pc = registers.Pc;

        _interruptManagement = cpuManagement.InterruptManagement;
        _cycleCounter = cpuManagement.CycleCounter;
        _runningStateManager = cpuManagement.RunningStateManager;
        _instructions = instructions;
    }

    public CpuStatus GetStatus()
    {
        return new CpuStatus
        {
            CurrentCycleCount = _cycleCounter.CurrentCycleCount,
            Halted = _runningStateManager.Halted,

            Af = _af.AsUnsigned16BitValue(),
            Bc = _bc.AsUnsigned16BitValue(),
            De = _de.AsUnsigned16BitValue(),
            Hl = _hl.AsUnsigned16BitValue(),
            AfShadow = _af.ShadowAsUnsigned16BitValue(),
            BcShadow = _bc.ShadowAsUnsigned16BitValue(),
            DeShadow = _de.ShadowAsUnsigned16BitValue(),
            HlShadow = _hl.ShadowAsUnsigned16BitValue(),
            Ix = _ix.AsUnsigned16BitValue(),
            Iy = _iy.AsUnsigned16BitValue(),
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
        _runningStateManager.Reset();

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
            _runningStateManager.ResumeIfHalted();
            return _cycleCounter.CurrentCycleCount;
        }

        if (_runningStateManager.Halted)
        {
            // NOP until interrupt
            return NopCycleCount;
        }

        var opCode = _pc.GetNextInstruction();
        var instruction = _instructions.GetInstruction(opCode);
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
}