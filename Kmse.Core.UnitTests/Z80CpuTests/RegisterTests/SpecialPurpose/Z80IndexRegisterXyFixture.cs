using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

public class Z80IndexRegisterXyFixture
{
    private IZ80FlagsManager _flags;
    private TestZ80IndexRegisterXy _indexRegister;
    private IMasterSystemMemory _memory;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _indexRegister = new TestZ80IndexRegisterXy(_memory, _flags);
    }

    [Test]
    [TestCase(0x01, 0x02, false)]
    [TestCase(0x0F, 0x10, true)]
    [TestCase(0x10, 0x11, false)]
    [TestCase(0xFE, 0xFF, false)]
    [TestCase(0xFF, 0x00, true)]
    public void WhenIncrementHighByteThenHighByteIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        _indexRegister.SetHigh(value);
        _indexRegister.SetLow(0x12);
        _indexRegister.IncrementHigh();
        _indexRegister.High.Should().Be(expectedValue);
        _indexRegister.Low.Should().Be(0x12);

        CheckFlags(true, halfCarryStatus, value, expectedValue);
    }

    [Test]
    [TestCase(0x01, 0x02, false)]
    [TestCase(0x0F, 0x10, true)]
    [TestCase(0x10, 0x11, false)]
    [TestCase(0xFE, 0xFF, false)]
    [TestCase(0xFF, 0x00, true)]
    public void WhenIncrementLowByteThenLowByteIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        _indexRegister.SetLow(value);
        _indexRegister.SetHigh(0x33);
        _indexRegister.IncrementLow();
        _indexRegister.Low.Should().Be(expectedValue);
        _indexRegister.High.Should().Be(0x33);

        CheckFlags(true, halfCarryStatus, value, expectedValue);
    }

    private void CheckFlags(bool increment, bool halfCarryStatus, byte value, byte expectedValue)
    {
        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, halfCarryStatus);
        if (increment)
        {
            _flags.Received(1).SetIfIncrementOverflow(value);
            _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        }
        else
        {
            _flags.Received(1).SetIfDecrementOverflow(value);
            _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        }
    }

    [Test]
    [TestCase(0x02, 0x01, false)]
    [TestCase(0x10, 0x0F, true)]
    [TestCase(0x11, 0x10, false)]
    [TestCase(0xFF, 0xFE, false)]
    [TestCase(0x00, 0xFF, true)]
    public void WhenDecrementHighByteThenHighByteIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        _indexRegister.SetHigh(value);
        _indexRegister.SetLow(0x12);
        _indexRegister.DecrementHigh();
        _indexRegister.High.Should().Be(expectedValue);
        _indexRegister.Low.Should().Be(0x12);

        CheckFlags(false, halfCarryStatus, value, expectedValue);
    }

    [Test]
    [TestCase(0x02, 0x01, false)]
    [TestCase(0x10, 0x0F, true)]
    [TestCase(0x11, 0x10, false)]
    [TestCase(0xFF, 0xFE, false)]
    [TestCase(0x00, 0xFF, true)]
    public void WhenDecrementLowByteThenLowByteIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        _indexRegister.SetLow(value);
        _indexRegister.SetHigh(0x33);
        _indexRegister.DecrementLow();
        _indexRegister.Low.Should().Be(expectedValue);
        _indexRegister.High.Should().Be(0x33);

        CheckFlags(false, halfCarryStatus, value, expectedValue);
    }
}