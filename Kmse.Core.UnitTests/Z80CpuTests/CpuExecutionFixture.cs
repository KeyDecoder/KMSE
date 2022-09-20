using FluentAssertions;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

[TestFixture]
public class CpuExecutionFixture
{
    [SetUp]
    public void Setup()
    {
        _cpuLogger = Substitute.For<ICpuLogger>();
        _memory = Substitute.For<IMasterSystemMemory>();
        _io = Substitute.For<IMasterSystemIoManager>();
        _cpu = new Z80Cpu(_cpuLogger, new Z80InstructionLogger(_cpuLogger));
        _cpu.Initialize(_memory, _io);
        _interruptManagement = _cpu.GetInterruptManagementInterface();
    }

    private void PrepareForTest()
    {
        _cpu.Reset();
        _memory.ClearReceivedCalls();
        _io.ClearReceivedCalls();
        _interruptManagement.ClearMaskableInterrupt();
        _interruptManagement.ClearNonMaskableInterrupt();
    }

    private Z80Cpu _cpu;
    private ICpuLogger _cpuLogger;
    private IMasterSystemMemory _memory;
    private IMasterSystemIoManager _io;
    private IZ80InterruptManagement _interruptManagement;

    [Test]
    public void WhenResettingCpu()
    {
        _interruptManagement.SetMaskableInterrupt();
        _interruptManagement.SetNonMaskableInterrupt();

        _cpu.Reset();
        _interruptManagement.MaskableInterrupt.Should().BeFalse();
        _interruptManagement.NonMaskableInterrupt.Should().BeFalse();

        var status = _cpu.GetStatus();

        status.CurrentCycleCount.Should().Be(0);
        status.Pc.Should().Be(0);
        status.Halted.Should().BeFalse();
        status.InterruptFlipFlop1.Should().BeFalse();
        status.InterruptFlipFlop2.Should().BeFalse();
        status.InterruptMode.Should().Be(0);

        status.IRegister.Should().Be(0);
        status.RRegister.Should().Be(0);
        status.StackPointer.Should().Be(0xDFF0);
        status.Af.Word.Should().Be(0);
        status.Bc.Word.Should().Be(0);
        status.De.Word.Should().Be(0);
        status.Hl.Word.Should().Be(0);
        status.AfShadow.Word.Should().Be(0);
        status.BcShadow.Word.Should().Be(0);
        status.DeShadow.Word.Should().Be(0);
        status.HlShadow.Word.Should().Be(0);
    }

    [Test]
    public void WhenExecutingSimpleInstruction()
    {
        PrepareForTest();

        // Test using HALT since basic instruction with clear change
        _memory[0x00].Returns((byte)0x76);
        _cpu.ExecuteNextCycle().Should().Be(4);

        var status = _cpu.GetStatus();
        status.Halted.Should().Be(true);
        status.Pc.Should().Be(0x01);
    }

    [Test]
    public void WhenExecutingDoubleByteInstruction()
    {
        PrepareForTest();

        // Test using setting MI mode to 2 since double byte instruction with clear change
        _memory[0x00].Returns((byte)0xED);
        _memory[0x01].Returns((byte)0x5E);
        _cpu.ExecuteNextCycle().Should().Be(8);

        var status = _cpu.GetStatus();
        status.InterruptMode.Should().Be(2);
        status.Pc.Should().Be(0x02);
    }

    [Test]
    public void WhenExecutingDdCbSpecialInstruction()
    {
        // TODO: Add once we implemented at least 1 DD CB XX XX instruction
    }

    [Test]
    public void WhenExecutingFdCbSpecialInstruction()
    {
        // TODO: Add once we implemented at least 1 FD CB XX XX instruction
    }

    [Test]
    public void WhenExecutingAndNmiSet()
    {
        PrepareForTest();

        // Enable interrupts
        _memory[0x00].Returns((byte)0xFB);
        // Execute EI instruction first so we have maskable set but also moved the PC
        // This allows us to better test handling since Pc is non zero and interrupts are on
        _cpu.ExecuteNextCycle().Should().Be(4);

        _interruptManagement.ClearMaskableInterrupt();
        _interruptManagement.SetNonMaskableInterrupt();

        var cycleCount = _cpu.ExecuteNextCycle();
        cycleCount.Should().Be(11);
        var status = _cpu.GetStatus();
        status.Halted.Should().Be(false);
        status.InterruptFlipFlop1.Should().BeFalse();
        status.InterruptFlipFlop2.Should().BeTrue();
        status.Pc.Should().Be(0x66);

        // Pc put onto stack
        status.StackPointer.Should().Be(0xDFEE);
        // Wrote current PC (0x00) onto stack
        _memory.Received(1)[0xDFEF] = 0x00;
        _memory.Received(1)[0xDFEE] = 0x01;
    }

    private static int[] _maskedInterruptModes = { 0, 1, 2 };

    [Test]
    [TestCaseSource(nameof(_maskedInterruptModes))]
    public void WhenExecutingAndMaskedInterruptSetUsingMode0(int mode)
    {
        PrepareForTest();

        // Enable interrupts
        _memory[0x00].Returns((byte)0xFB);
        _memory[0x01].Returns((byte)0xED);

        byte opCode = mode switch
        {
            0 => 0x46,
            1 => 0x56,
            2 => 0x5E,
            _ => throw new ArgumentException("Invalid mode")
        };
        _memory[0x02].Returns(opCode);

        // Set EI and then set mode
        _cpu.ExecuteNextCycle().Should().Be(4);
        _cpu.ExecuteNextCycle().Should().Be(8);

        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();
        _interruptManagement.SetMaskableInterrupt(); ;

        var cycleCount = _cpu.ExecuteNextCycle();
        var status = _cpu.GetStatus();

        status.Halted.Should().Be(false);
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();

        // Jumps to 0x38 when in mode 0 or 1
        if (mode is 0 or 1)
        {
            cycleCount.Should().Be(11);

            // Interrupt is not cleared in these modes until a RETI or DI is called
            _interruptManagement.MaskableInterrupt.Should().BeTrue();

            status.Pc.Should().Be(0x38);

            // Pc put onto stack
            status.StackPointer.Should().Be(0xDFEE);
            // Wrote current PC (0x00) onto stack
            _memory.Received(1)[0xDFEF] = 0x00;
            _memory.Received(1)[0xDFEE] = 0x03;
        }
        else
        {
            // NOP
            cycleCount.Should().Be(4);

            // Interrupt is cleared in this modes since this mode is not supported
            _interruptManagement.MaskableInterrupt.Should().BeFalse();
        }
    }

    [Test]
    public void WhenEnablingAndDisablingMaskableInterrupts()
    {
        PrepareForTest();

        // Enable interrupts
        _memory[0x00].Returns((byte)0xFB);
        _memory[0x01].Returns((byte)0xF3);

        // Execute EI first
        _cpu.ExecuteNextCycle().Should().Be(4);
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();

        // Execute disable interrupt
        _cpu.ExecuteNextCycle().Should().Be(4);
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();
    }
}