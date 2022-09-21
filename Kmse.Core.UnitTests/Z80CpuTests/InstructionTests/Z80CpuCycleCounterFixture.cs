using FluentAssertions;
using Kmse.Core.Z80.Instructions;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.InstructionTests;

public class Z80CpuCycleCounterFixture
{
    private Z80CpuCycleCounter _cycleCounter;

    [SetUp]
    public void Setup()
    {
        _cycleCounter = new Z80CpuCycleCounter();
    }

    [Test]
    public void WhenIncrementingValue()
    {
        _cycleCounter.Reset();
        _cycleCounter.Increment(1);
        _cycleCounter.Increment(2);
        _cycleCounter.Increment(3);
        _cycleCounter.CurrentCycleCount.Should().Be(6);
    }

    [Test]
    public void WhenIncrementingValueByNegativeNumber()
    {
        _cycleCounter.Reset();
        var action = () => _cycleCounter.Increment(-1);
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void WhenResetThenValueIsZero()
    {
        _cycleCounter.Increment(123);
        _cycleCounter.Reset();
        _cycleCounter.CurrentCycleCount.Should().Be(0);
    }
}