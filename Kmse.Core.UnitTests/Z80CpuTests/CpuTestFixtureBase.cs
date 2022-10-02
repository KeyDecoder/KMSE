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
    protected Z80Cpu Cpu;
    protected ICpuLogger CpuLogger;
    protected IZ80InterruptManagement InterruptManagement;
    protected IMasterSystemIoManager Io;
    protected IMasterSystemMemory Memory;
    protected IZ80InstructionLogger InstructionLogger;
    protected Z80CpuRegisters Registers;
    protected Z80CpuManagement CpuManagement;

    protected void PrepareForTest()
    {
        Cpu.Reset();
        Memory.ClearReceivedCalls();
        Memory.ClearSubstitute();
        Io.ClearReceivedCalls();
    }

    [SetUp]
    public void Setup()
    {
        CpuLogger = Substitute.For<ICpuLogger>();
        Memory = Substitute.For<IMasterSystemMemory>();
        Io = Substitute.For<IMasterSystemIoManager>();
        InstructionLogger = new Z80InstructionLogger(CpuLogger);

        var flags = new Z80FlagsManager();
        var accumulator = new Z80Accumulator(Memory, flags);
        var af = new Z80AfRegister(Memory, flags, accumulator);
        var bc = new Z80BcRegister(Memory, af.Flags, () => new Z808BitGeneralPurposeRegister(Memory, flags));
        var de = new Z80DeRegister(Memory, af.Flags, () => new Z808BitGeneralPurposeRegister(Memory, flags));
        var hl = new Z80HlRegister(Memory, af.Flags, () => new Z808BitGeneralPurposeRegister(Memory, flags));
        var ix = new Z80IndexRegisterX(Memory, af.Flags);
        var iy = new Z80IndexRegisterY(Memory, af.Flags);
        var rRegister = new Z80MemoryRefreshRegister(Memory, af.Flags);
        var iRegister = new Z80InterruptPageAddressRegister(Memory, af.Flags);

        var stack = new Z80StackManager(Memory, af.Flags, CpuLogger);
        var pc = new Z80ProgramCounter(Memory, af.Flags, stack, InstructionLogger);

        var ioManagement = new Z80CpuInputOutputManager(Io, af.Flags);
        var memoryManagement = new Z80CpuMemoryManagement(Memory, af.Flags);
        InterruptManagement = new Z80InterruptManagement(pc, CpuLogger);
        var cycleCounter = new Z80CpuCycleCounter();
        var runningStateManager = new Z80CpuRunningStateManager(CpuLogger);

        Registers = new Z80CpuRegisters
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
        CpuManagement = new Z80CpuManagement
        {
            IoManagement = ioManagement,
            InterruptManagement = InterruptManagement,
            MemoryManagement = memoryManagement,
            CycleCounter = cycleCounter,
            RunningStateManager = runningStateManager,
        };
        var cpuInstructions = new Z80CpuInstructions(Memory, Io, CpuLogger, Registers, CpuManagement);
        Cpu = new Z80Cpu(Memory, Io, CpuLogger, InstructionLogger, cpuInstructions, Registers, CpuManagement);
    }
}