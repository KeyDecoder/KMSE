using FluentAssertions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.InterruptManagementTests;

public class Z80InterruptManagementFixture
{
    private Z80InterruptManagement _interruptManagement;
    private IZ80ProgramCounter _programCounter;
    private IZ80MemoryRefreshRegister _refreshRegister;

    [SetUp]
    public void Setup()
    {
        _programCounter = Substitute.For<IZ80ProgramCounter>();
        _refreshRegister = Substitute.For<IZ80MemoryRefreshRegister>();
        _interruptManagement = new Z80InterruptManagement(_programCounter, Substitute.For<ICpuLogger>(), _refreshRegister);
    }

    [Test]
    public void WhenSettingInterruptModeThenModeIsSet()
    {
        _interruptManagement.SetInterruptMode(2);
        _interruptManagement.InterruptMode.Should().Be(2);
    }

    [Test]
    public void WhenSettingInvalidInterruptModeThenExceptionIsThrown()
    {
        var action = () => _interruptManagement.SetInterruptMode(3);
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void WhenEnablingMaskableInterruptsThenFlipFlopsAreSetToTrue()
    {
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();
    }

    [Test]
    public void WhenDisablingMaskableInterruptsThenFlipFlopsAreSetToFalse()
    {
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.DisableMaskableInterrupts();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();
    }

    [Test]
    public void WhenSettingNonMaskableInterruptThenNonMaskableInterruptIsSet()
    {
        _interruptManagement.SetNonMaskableInterrupt();
        _interruptManagement.NonMaskableInterrupt.Should().BeTrue();
        _interruptManagement.InterruptWaiting().Should().BeTrue();
    }

    [Test]
    public void WhenProcessingInterruptsWithNonMaskableInterruptThenJumpProgramCounterToMemoryAddress66H()
    {
        _interruptManagement.SetNonMaskableInterrupt();
        var cycleCount = _interruptManagement.ProcessInterrupts();

        cycleCount.Should().Be(11);
        _refreshRegister.Received(1).Increment(1);
        _programCounter.Received(1).SetAndSaveExisting(0x66);
        _interruptManagement.NonMaskableInterrupt.Should().BeFalse();
    }

    [Test]
    public void WhenProcessingInterruptsWithNonMaskableInterruptThenFlipFlopStatusIsCopiedToTempStorage()
    {
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetNonMaskableInterrupt();
        _interruptManagement.ProcessInterrupts();

        _interruptManagement.NonMaskableInterrupt.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();
    }

    [Test]
    public void WhenSettingMaskableInterruptWithInterruptsEnabledThenMaskableInterruptIsSet()
    {
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();
        _interruptManagement.MaskableInterrupt.Should().BeTrue();
        _interruptManagement.InterruptWaiting().Should().BeTrue();
    }

    [Test]
    public void WhenSettingMaskableInterruptWithInterruptsDisabledThenMaskableInterruptIsNotSet()
    {
        _interruptManagement.DisableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();
        _interruptManagement.MaskableInterrupt.Should().BeFalse();
        _interruptManagement.InterruptWaiting().Should().BeFalse();
    }

    [Test]
    public void WhenProcessingMaskableInterruptThenInterruptNotHandledOnNextInstruction()
    {
        _interruptManagement.SetInterruptMode(1);
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();

        var cycleCount = _interruptManagement.ProcessInterrupts();

        cycleCount.Should().Be(0);
        _refreshRegister.DidNotReceive().Increment(1);

        _interruptManagement.MaskableInterrupt.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();

        _programCounter.DidNotReceiveWithAnyArgs().SetAndSaveExisting(0);
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    public void WhenProcessingMaskableInterruptMode0Or1ThenJumpTo38H(byte mode)
    {
        _interruptManagement.SetInterruptMode(mode);
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();

        _interruptManagement.ProcessInterrupts();
        var cycleCount = _interruptManagement.ProcessInterrupts();

        cycleCount.Should().Be(11);
        _refreshRegister.Received(1).Increment(1);

        _interruptManagement.MaskableInterrupt.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();

        _programCounter.Received(1).SetAndSaveExisting(0x38);
    }

    [Test]
    public void WhenProcessingMaskableInterruptMode2ThenNoOperation()
    {
        _interruptManagement.SetInterruptMode(2);
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();

        _interruptManagement.ProcessInterrupts();
        var cycleCount = _interruptManagement.ProcessInterrupts();

        cycleCount.Should().Be(4);
        _refreshRegister.Received(1).Increment(1);

        _interruptManagement.MaskableInterrupt.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();

        // Mode 2 does nothing so we check it did not make any calls to PC
        _programCounter.DidNotReceiveWithAnyArgs().SetAndSaveExisting(0);
        _programCounter.DidNotReceiveWithAnyArgs().Set(0);
    }

    [Test]
    public void WhenProcessingInterruptsWithBothNonMaskableAndMaskableInterruptsThenNonMaskableIsHandledFirst()
    {
        // NonMaskable interrupt is always processed first and Maskable interrupts disabled temporarily until a RET call then on the next processing cycle is the maskable interrupt processed
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetMaskableInterrupt();
        _interruptManagement.SetNonMaskableInterrupt();
        var cycleCount = _interruptManagement.ProcessInterrupts();

        cycleCount.Should().Be(11);
        _programCounter.Received(1).SetAndSaveExisting(0x66);
        _interruptManagement.NonMaskableInterrupt.Should().BeFalse();
        _interruptManagement.MaskableInterrupt.Should().BeTrue();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeTrue();

        cycleCount = _interruptManagement.ProcessInterrupts();
        cycleCount.Should().Be(0);
    }

    [Test]
    public void WhenResetThenInterruptsAndInterruptFlipFlopsAreReset()
    {
        _interruptManagement.SetInterruptMode(1);
        _interruptManagement.EnableMaskableInterrupts();
        _interruptManagement.SetNonMaskableInterrupt();
        _interruptManagement.SetMaskableInterrupt();

        _interruptManagement.Reset();
        _interruptManagement.MaskableInterrupt.Should().BeFalse();
        _interruptManagement.NonMaskableInterrupt.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopStatus.Should().BeFalse();
        _interruptManagement.InterruptEnableFlipFlopTempStorageStatus.Should().BeFalse();
        _interruptManagement.InterruptMode.Should().Be(0);
        _interruptManagement.InterruptWaiting().Should().BeFalse();
    }
}