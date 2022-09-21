using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Running;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

public abstract class CpuTestFixtureBase
{
    protected Z80Cpu _cpu;
    protected ICpuLogger _cpuLogger;
    protected IZ80InterruptManagement _interruptManagement;
    protected IMasterSystemIoManager _io;
    protected IMasterSystemMemory _memory;

    protected void PrepareForTest()
    {
        _cpu.Reset();
        _memory.ClearReceivedCalls();
        _memory.ClearSubstitute();
        _io.ClearReceivedCalls();
    }

    [SetUp]
    public void Setup()
    {
        _cpuLogger = Substitute.For<ICpuLogger>();
        _memory = Substitute.For<IMasterSystemMemory>();
        _io = Substitute.For<IMasterSystemIoManager>();
        var instructionLogger = new Z80InstructionLogger(_cpuLogger);

        var flags = new Z80FlagsManager();
        var accumulator = new Z80Accumulator(flags, _memory);
        var af = new Z80AfRegister(_memory, flags, accumulator);
        var bc = new Z80BcRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var de = new Z80DeRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var hl = new Z80HlRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var ix = new Z80IndexRegisterXy(_memory, af.Flags);
        var iy = new Z80IndexRegisterXy(_memory, af.Flags);
        var rRegister = new Z80MemoryRefreshRegister(_memory, af.Flags);
        var iRegister = new Z80InterruptPageAddressRegister(_memory, af.Flags);

        var stack = new Z80StackManager(_memory, af.Flags, _cpuLogger);
        var pc = new Z80ProgramCounter(_memory, af.Flags, stack, instructionLogger);

        var ioManagement = new Z80CpuInputOutput(_io, af.Flags);
        var memoryManagement = new Z80CpuMemoryManagement(_memory, af.Flags);
        _interruptManagement = new Z80InterruptManagement(pc, _cpuLogger);
        var cycleCounter = new Z80CpuCycleCounter();
        var runningStateManager = new Z80CpuRunningStateManager(_cpuLogger);

        var registers = new Z80CpuRegisters
        {
            Pc = pc,
            Stack = stack,
            Af = af,
            Bc = bc,
            De = de,
            Hl = hl,
            IX = ix,
            IY = iy,
            R = rRegister,
            I = iRegister
        };
        var cpuManagement = new Z80CpuManagement
        {
            IoManagement = ioManagement,
            InterruptManagement = _interruptManagement,
            MemoryManagement = memoryManagement,
            CycleCounter = cycleCounter,
            RunningStateManager = runningStateManager,
        };
        var cpuInstructions = new Z80CpuInstructions(_memory, _io, _cpuLogger, registers, cpuManagement);
        _cpu = new Z80Cpu(_memory, _io, _cpuLogger, instructionLogger, cpuInstructions, registers, cpuManagement);
    }
}