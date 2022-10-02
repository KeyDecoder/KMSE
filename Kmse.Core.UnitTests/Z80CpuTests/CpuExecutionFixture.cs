using FluentAssertions;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

[TestFixture]
public class CpuExecutionFixture : CpuTestFixtureBase
{
    [Test]
    public void WhenResettingCpu()
    {
        InterruptManagement.SetMaskableInterrupt();
        InterruptManagement.SetNonMaskableInterrupt();

        Cpu.Reset();
        InterruptManagement.MaskableInterrupt.Should().BeFalse();
        InterruptManagement.NonMaskableInterrupt.Should().BeFalse();

        var status = Cpu.GetStatus();

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
        Memory[0x00].Returns((byte)0x76);
        Cpu.ExecuteNextCycle().Should().Be(4);

        var status = Cpu.GetStatus();
        status.Halted.Should().Be(true);
        status.Pc.Should().Be(0x01);
    }

    [Test]
    public void WhenExecutingDoubleByteInstruction()
    {
        PrepareForTest();

        // Test using setting MI mode to 2 since double byte instruction with clear change
        Memory[0x00].Returns((byte)0xED);
        Memory[0x01].Returns((byte)0x5E);
        Cpu.ExecuteNextCycle().Should().Be(8);

        var status = Cpu.GetStatus();
        status.InterruptMode.Should().Be(2);
        status.Pc.Should().Be(0x02);
    }

    [Test]
    public void WhenExecutingDdCbSpecialInstruction()
    {
        Registers.IX = Substitute.For<IZ80IndexRegisterX>();
        var cpuInstructions = new Z80CpuInstructions(Memory, Io, CpuLogger, Registers, CpuManagement);
        Cpu = new Z80Cpu(Memory, Io, CpuLogger, InstructionLogger, cpuInstructions, Registers, CpuManagement);

        PrepareForTest();

        // RES 0,(IX+d)
        Memory[0x00].Returns((byte)0xDD);
        Memory[0x01].Returns((byte)0xCB);
        Memory[0x02].Returns((byte)0x02);
        Memory[0x03].Returns((byte)0x86);
        Cpu.ExecuteNextCycle().Should().Be(23);

        var status = Cpu.GetStatus();
        status.Pc.Should().Be(0x04);

        Registers.IX.Received(1).ResetBitByRegisterLocation(0, 0x02);
    }

    [Test]
    public void WhenExecutingFdCbSpecialInstruction()
    {
        Registers.IY = Substitute.For<IZ80IndexRegisterY>();
        var cpuInstructions = new Z80CpuInstructions(Memory, Io, CpuLogger, Registers, CpuManagement);
        Cpu = new Z80Cpu(Memory, Io, CpuLogger, InstructionLogger, cpuInstructions, Registers, CpuManagement);

        PrepareForTest();

        // RES 0,(IY+d)
        Memory[0x00].Returns((byte)0xFD);
        Memory[0x01].Returns((byte)0xCB);
        Memory[0x02].Returns((byte)0x02);
        Memory[0x03].Returns((byte)0x86);
        Cpu.ExecuteNextCycle().Should().Be(23);

        var status = Cpu.GetStatus();
        status.Pc.Should().Be(0x04);

        Registers.IY.Received(1).ResetBitByRegisterLocation(0, 0x02);
    }

    [Test]
    public void WhenExecutingAndInterruptIsSet()
    {
        // This validates that when executing, it checks for interrupts and processes them

        PrepareForTest();

        // Enable interrupts
        Memory[0x00].Returns((byte)0xFB);
        // Execute EI instruction first so we have maskable set but also moved the PC
        // This allows us to better test handling since Pc is non zero and interrupts are on
        Cpu.ExecuteNextCycle().Should().Be(4);

        InterruptManagement.ClearMaskableInterrupt();
        InterruptManagement.SetNonMaskableInterrupt();

        var cycleCount = Cpu.ExecuteNextCycle();

        cycleCount.Should().Be(11);

        var status = Cpu.GetStatus();
        status.Halted.Should().Be(false);
        status.InterruptFlipFlop1.Should().BeFalse();
        status.InterruptFlipFlop2.Should().BeTrue();
        status.Pc.Should().Be(0x66);

        // Pc put onto stack
        status.StackPointer.Should().Be(0xDFEE);
        // Wrote current PC (0x00) onto stack
        Memory.Received(1)[0xDFEF] = 0x00;
        Memory.Received(1)[0xDFEE] = 0x01;
    }
}