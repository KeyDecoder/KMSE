using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class Z80AccumulatorFixture
{
    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _register = new Z80Accumulator(_memory, _flags);
    }

    private IMasterSystemMemory _memory;
    private IZ80FlagsManager _flags;
    private Z80Accumulator _register;

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void WhenSetFromInterruptRegisterAndIff2SetThenValueAndFlagsAreSet(bool iff2Status)
    {
        var interruptRegister = Substitute.For<IZ80InterruptPageAddressRegister>();
        const byte value = 0x12;
        interruptRegister.Value.Returns(value);
        _register.SetFromInterruptRegister(interruptRegister, iff2Status);
        _register.Value.Should().Be(value);

        _flags.Received(1).SetIfNegative(value);
        _flags.Received(1).SetIfZero(value);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, iff2Status);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void WhenSetFromMemoryRefreshRegisterAndIff2SetThenValueAndFlagsAreSet(bool iff2Status)
    {
        var interruptRegister = Substitute.For<IZ80MemoryRefreshRegister>();
        const byte value = 0x12;
        interruptRegister.Value.Returns(value);
        _register.SetFromMemoryRefreshRegister(interruptRegister, iff2Status);
        _register.Value.Should().Be(value);

        _flags.Received(1).SetIfNegative(value);
        _flags.Received(1).SetIfZero(value);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, iff2Status);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    public void WhenRotateLeftDigitFromHlRegister()
    {
        const byte expectedAValue = 0x73;
        var hlRegister = Substitute.For<IZ80HlRegister>();
        hlRegister.Value.Returns((ushort)0x1234);
        _memory[0x1234].Returns((byte)0x31);
        _register.Set(0x7A);

        _register.RotateLeftDigit(hlRegister);

        _memory.Received()[0x1234] = 0x1A;
        _register.Value.Should().Be(expectedAValue);

        _flags.Received(1).SetIfNegative(expectedAValue);
        _flags.Received(1).SetIfZero(expectedAValue);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetParityFromValue(expectedAValue);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    public void WhenRotateRightDigitFromHlRegister()
    {
        const byte expectedAValue = 0x80;
        var hlRegister = Substitute.For<IZ80HlRegister>();
        hlRegister.Value.Returns((ushort)0x1234);
        _memory[0x1234].Returns((byte)0x20);
        _register.Set(0x84);

        _register.RotateRightDigit(hlRegister);

        _memory.Received()[0x1234] = 0x42;
        _register.Value.Should().Be(expectedAValue);

        _flags.Received(1).SetIfNegative(expectedAValue);
        _flags.Received(1).SetIfZero(expectedAValue);
        _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.SetParityFromValue(expectedAValue);
        _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    [TestCase(0, 0, false, false, false, false)]
    [TestCase(1, 1, false, false, false, false)]
    [TestCase(0xA0, 0x00, false, false, false, true)]
    [TestCase(0xAA, 0x10, false, false, true, true)]
    [TestCase(0x11, 0x17, true, false, false, false)]
    [TestCase(0x01, 0x61, false, true, false, true)]
    [TestCase(0xFF, 0x65, false, false, true, true)]
    public void WhenDoingDecAdjustAccumulatorAfterAdd(byte currentValue, byte expectedValue, bool halfCarrySet,
        bool carrySet, bool expectedHalfCarryStatus, bool expectedCarryStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(carrySet);
        _flags.IsFlagSet(Z80StatusFlags.HalfCarryH).Returns(halfCarrySet);
        _flags.IsFlagSet(Z80StatusFlags.AddSubtractN).Returns(false);

        _register.Set(currentValue);
        _register.DecimalAdjustAccumulator();

        _register.Value.Should().Be(expectedValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryStatus);
        if (expectedCarryStatus)
        {
            _flags.Received(1).SetFlag(Z80StatusFlags.CarryC);
        }
        else
        {
            _flags.Received(1).ClearFlag(Z80StatusFlags.CarryC);
        }

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetParityFromValue(expectedValue);
    }

    [Test]
    public void WhenInvertAccumulatorRegisterThenValueIsInverted()
    {
        _register.Set(0x12);
        _register.InvertAccumulatorRegister();
        _register.Value.Should().Be(0xED);

        _flags.Received(1).SetFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    [TestCase(0, 0, false, false)]
    [TestCase(1, 0xFF, true, true)]
    [TestCase(0x12, 0xEE, true, true)]
    [TestCase(0xFF, 0x01, true, true)]
    [TestCase(0x10, 0xF0, false, true)]
    public void WhenNegateAccumulatorRegisterThenValueIsInverted(byte currentValue, byte expectedValue,
        bool expectedHalfCarryStatus, bool expectedCarryStatus)
    {
        _register.Set(currentValue);
        _register.NegateAccumulatorRegister();
        _register.Value.Should().Be(expectedValue);

        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryStatus);
        _flags.Received(1).SetIfDecrementOverflow(currentValue);
        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryStatus);
    }

    [Test]
    [TestCase((byte)0, (byte)0, (byte)0, false, false)]
    [TestCase((byte)0x01, (byte)0x01, (byte)0x0002, false, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x00, false, true)]
    [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFE, true, true)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x00, true, true)]
    [TestCase((byte)0x80, (byte)0x80, (byte)0x00, false, true)]
    public void WhenAddWithoutCarry(byte value, byte operand, byte newValue, bool expectedHalfCarryFlagStatus,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.Add(operand);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetIfHalfCarry(value, operand, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase((byte)0, (byte)0, (byte)1, false, false, false)]
    [TestCase((byte)0x01, (byte)0x01, (byte)0x0003, false, false, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x01, false, true, false)]
    [TestCase((byte)0x0F, (byte)0x01, (byte)0x11, true, false, false)]
    [TestCase((byte)0x0F, (byte)0xFF, (byte)0x0F, true, true, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x01, true, true, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x01, true, true, false)]
    [TestCase((byte)0x80, (byte)0xAA, (byte)0x2B, false, true, true)]
    public void WhenAddWithCarryAndCarrySet(byte value, byte operand, byte newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(value);
        _register.Add(operand, true);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetIfHalfCarry(value, operand, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((byte)0, (byte)0, (byte)0, false, false, false)]
    [TestCase((byte)0x01, (byte)0x01, (byte)0x0002, false, false, false)]
    [TestCase((byte)0x0F, (byte)0x01, (byte)0x10, true, false, false)]
    [TestCase((byte)0x0F, (byte)0xFF, (byte)0x0E, true, true, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x00, true, true, false)]
    [TestCase((byte)0x80, (byte)0xAA, (byte)0x2A, false, true, true)]
    public void WhenAddWithCarryAndCarryNotSet(byte value, byte operand, byte newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(false);
        _register.Set(value);
        _register.Add(operand, true);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetIfHalfCarry(value, operand, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((byte)0, (byte)0, (byte)0xFF, true, true, false)]
    [TestCase((byte)1, (byte)1, (byte)0xFF, true, true, false)]
    [TestCase((byte)0x02, (byte)0x01, (byte)0x00, false, false, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0xFD, false, false, false)]
    [TestCase((byte)0x0F, (byte)0x01, (byte)0x0D, false, false, false)]
    [TestCase((byte)0x10, (byte)0x01, (byte)0x0E, true, false, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0xFD, false, false, false)]
    [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFF, true, true, false)]
    [TestCase((byte)0x80, (byte)0x7F, (byte)0x00, true, false, true)]
    public void WhenSubtractWithCarryAndCarrySet(byte value, byte operand, byte newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(value);
        _register.Subtract(operand, true);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetIfHalfCarry(value, operand, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((byte)0, (byte)0, (byte)0, false, false, false)]
    [TestCase((byte)0, (byte)1, (byte)0xFF, true, true, false)]
    [TestCase((byte)0x01, (byte)0x01, (byte)0x00, false, false, false)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0xFE, false, false, false)]
    [TestCase((byte)0x0F, (byte)0x01, (byte)0x0E, false, false, false)]
    [TestCase((byte)0x10, (byte)0x01, (byte)0x0F, true, false, false)]
    [TestCase((byte)0x80, (byte)0x7F, (byte)0x01, true, false, true)]
    public void WhenSubtractWithCarryAndCarryNotSet(byte value, byte operand, byte newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(false);
        _register.Set(value);
        _register.Subtract(operand);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetIfHalfCarry(value, operand, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    // These are tested extensively else where, we just need to validate it is reading and writing the rotated/shifted values from/to memory

    [Test]
    public void WhenRotatingLeftCircularThenRotatedLeftWithCircularHandling()
    {
        _register.Set(0x87);
        _register.RotateLeftCircular();
        _register.Value.Should().Be(0x0F);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingRightCircularThenRotatedRightWithCircularHandling()
    {
        _register.Set(0x07);
        _register.RotateRightCircular();
        _register.Value.Should().Be(0x83);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingLeftThenRotatedLeft()
    {
        _register.Set(0x87);
        _register.RotateLeft();
        _register.Value.Should().Be(0x0E);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingRightThenRotatedRight()
    {
        _register.Set(0x07);
        _register.RotateRight();
        _register.Value.Should().Be(0x03);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftLeftArithmeticThenShiftedLeft()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(0x87);
        _register.ShiftLeftArithmetic();
        _register.Value.Should().Be(0x0E);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftRightArithmeticThenShiftedRight()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(0x07);
        _register.ShiftRightArithmetic();
        _register.Value.Should().Be(0x03);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftLeftLogicalThenShiftedLeft()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(0x87);
        _register.ShiftLeftLogical();
        _register.Value.Should().Be(0x0F);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftRightLogicalThenShiftedRight()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(0x87);
        _register.ShiftRightLogical();
        _register.Value.Should().Be(0x43);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenComparingValue()
    {
        const byte expectedValue = 0xDE;
        _register.Set(0x12);
        _register.Compare(0x34);

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetIfHalfCarry(0x12, 0x34, Arg.Any<int>());
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, false);
    }

    [Test]
    public void WhenAndValue()
    {
        const byte expectedValue = 0x10;
        _register.And(0x12, 0x34);
        _register.Value.Should().Be(expectedValue);

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).ClearFlag(Z80StatusFlags.CarryC);
    }

    [Test]
    public void WhenOrValue()
    {
        const byte expectedValue = 0x36;
        _register.Or(0x12, 0x34);
        _register.Value.Should().Be(expectedValue);

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).ClearFlag(Z80StatusFlags.CarryC);
    }

    [Test]
    public void WhenXorValue()
    {
        const byte expectedValue = 0x26;
        _register.Xor(0x12, 0x34);
        _register.Value.Should().Be(expectedValue);

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).ClearFlag(Z80StatusFlags.CarryC);
    }
}