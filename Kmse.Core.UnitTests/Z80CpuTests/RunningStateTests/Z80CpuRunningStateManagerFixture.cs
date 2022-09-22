using FluentAssertions;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Running;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RunningStateTests;

public class Z80CpuRunningStateManagerFixture
{
    private Z80CpuRunningStateManager _cpuRunningState;

    [SetUp]
    public void Setup()
    {
        _cpuRunningState = new Z80CpuRunningStateManager(Substitute.For<ICpuLogger>());
    }

    [Test]
    public void WhenHaltIsCalledThenRunningStateIsHalted()
    {
        _cpuRunningState.Reset();
        _cpuRunningState.Halt();
        _cpuRunningState.Halted.Should().BeTrue();
    }

    [Test]
    public void WhenResetThenNotHalted()
    {
        _cpuRunningState.Halt();
        _cpuRunningState.Reset();
        _cpuRunningState.Halted.Should().BeFalse();
    }

    [Test]
    public void WhenResumeIfHaltedThenNotHalted()
    {
        _cpuRunningState.Halt();
        _cpuRunningState.ResumeIfHalted();
        _cpuRunningState.Halted.Should().BeFalse();
    }
}