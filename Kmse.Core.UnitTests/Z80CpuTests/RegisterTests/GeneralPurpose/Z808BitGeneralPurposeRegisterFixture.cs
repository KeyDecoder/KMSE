using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class Z808BitGeneralPurposeRegisterFixture
{
    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _register = new Z808BitGeneralPurposeRegister(_memory, _flags);
    }

    private IMasterSystemMemory _memory;
    private IZ80FlagsManager _flags;
    private Z808BitGeneralPurposeRegister _register;

    [Test]
    [TestCase(0x01, 0x02, false)]
    [TestCase(0x0F, 0x10, true)]
    [TestCase(0x10, 0x11, false)]
    [TestCase(0xFE, 0xFF, false)]
    [TestCase(0xFF, 0x00, true)]
    public void WhenIncrementThenValueIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        _register.Set(value);
        _register.Increment();
        _register.Value.Should().Be(expectedValue);

        CheckFlags(true, halfCarryStatus, value, expectedValue);
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
        _register.Set(value);
        _register.Decrement();
        _register.Value.Should().Be(expectedValue);

        CheckFlags(false, halfCarryStatus, value, expectedValue);
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
    public void WhenClearingBitThenBitCleared()
    {
        for (var i = 0; i < 8; i++)
        {
            _register.Set(0xFF);
            _register.ClearBit(i);
            // Set the bit manually and then invert to get the cleared bit
            _register.Value.Should().Be((byte)~(1 << i));
        }
    }

    [Test]
    [TestCase(-1)]
    [TestCase(8)]
    public void WhenClearingBitOutsideRangeThenExceptionThrown(int bit)
    {
        var action = () => _register.ClearBit(bit);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSettingBitThenBitSet()
    {
        for (var i = 0; i < 8; i++)
        {
            _register.Set(0x00);
            _register.SetBit(i);
            // Set the bit manually and then invert to get the cleared bit
            _register.Value.Should().Be((byte)(1 << i));
        }
    }

    [Test]
    [TestCase(-1)]
    [TestCase(8)]
    public void WhenSettingBitOutsideRangeThenExceptionThrown(int bit)
    {
        var action = () => _register.SetBit(bit);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(0xAA, (byte)(((0xAA << 1) & 0xFF) + 1), true)]
    [TestCase(0x55, (byte)((0x55 << 1) & 0xFF), false)]
    public void WhenRotatingLeftCircularThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.RotateLeftCircular();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false, false)]
    [TestCase(0x55, (byte)((0x55 << 1) & 0xFF), false, false)]
    [TestCase(0x55, (byte)(((0x55 << 1) & 0xFF) + 1), true, false)]
    [TestCase(0xAA, (byte)((0xAA << 1) & 0xFF), false, true)]
    [TestCase(0xAA, (byte)(((0xAA << 1) & 0xFF) + 1), true, true)]
    [TestCase(0xFF, 0xFE, false, true)]
    [TestCase(0xFF, 0xFF, true, true)]
    public void WhenRotatingLeftThenMatchesExpectedValue(byte value, byte expectedValue, bool currentCarryFlagStatus,
        bool expectedCarryFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(currentCarryFlagStatus);
        _register.Set(value);
        _register.RotateLeft();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(0x01, 0x80, true)]
    [TestCase(0xAA, (byte)((0xAA >> 1) & 0xFF), false)]
    [TestCase(0x55, (byte)(((0x55 >> 1) & 0xFF) + 0x80), true)]
    [TestCase(0xFF, 0xFF, true)]
    public void WhenRotatingRightCircularThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.RotateRightCircular();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false, false)]
    [TestCase(0, 0x80, true, false)]
    [TestCase(0x01, 0x00, false, true)]
    [TestCase(0x01, 0x80, true, true)]
    [TestCase(0x55, (byte)((0x55 >> 1) & 0xFF), false, true)]
    [TestCase(0x55, (byte)(((0x55 >> 1) & 0xFF) + 0x80), true, true)]
    [TestCase(0xAA, (byte)((0xAA >> 1) & 0xFF), false, false)]
    [TestCase(0xAA, (byte)(((0xAA >> 1) & 0xFF) + 0x80), true, false)]
    [TestCase(0xFF, 0x7F, false, true)]
    [TestCase(0xFF, 0xFF, true, true)]
    public void WhenRotatingRightThenMatchesExpectedValue(byte value, byte expectedValue, bool currentCarryFlagStatus,
        bool expectedCarryFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(currentCarryFlagStatus);
        _register.Set(value);
        _register.RotateRight();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(0x55, (byte)((0x55 << 1) & 0xFF), false)]
    [TestCase(0xAA, (byte)((0xAA << 1) & 0xFF), true)]
    [TestCase(0xFF, 0xFE, true)]
    public void WhenShiftingLeftArithmeticThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.ShiftLeftArithmetic();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(0x55, (byte)((0x55 >> 1) & 0xFF), true)]
    [TestCase(0xAA, (byte)(((0xAA >> 1) & 0xFF) + 0x80), false)]
    [TestCase(0xFF, 0xFF, true)]
    public void WhenShiftingRightArithmeticThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.ShiftRightArithmetic();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0x01, false)]
    [TestCase(0x01, 0x03, false)]
    [TestCase(0x55, (byte)(((0x55 << 1) & 0xFF) + 1), false)]
    [TestCase(0xAA, (byte)(((0xAA << 1) & 0xFF) + 1), true)]
    [TestCase(0xFF, 0xFF, true)]
    public void WhenShiftingLeftLogicalThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.ShiftLeftLogical();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase(0, 0, false)]
    [TestCase(0x01, 0x00, true)]
    [TestCase(0x80, 0x40, false)]
    [TestCase(0x55, (byte)((0x55 >> 1) & 0xFF), true)]
    [TestCase(0xAA, (byte)((0xAA >> 1) & 0xFF), false)]
    [TestCase(0xFF, 0x7F, true)]
    public void WhenShiftingRightLogicalThenMatchesExpectedValue(byte value, byte expectedValue,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.ShiftRightLogical();

        _register.Value.Should().Be(expectedValue);
        CheckFlagsForBitShifting(expectedValue, expectedCarryFlagStatus);
    }

    private void CheckFlagsForBitShifting(byte expectedValue, bool expectedCarryFlagStatus)
    {
        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);
    }
}