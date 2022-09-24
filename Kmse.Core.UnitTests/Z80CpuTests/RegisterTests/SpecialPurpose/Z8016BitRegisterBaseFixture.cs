using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

[TestFixture]
public class Z8016BitRegisterBaseFixture
{
    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _register = new TestZ8016BitRegisterBase(_memory, _flags);

        _memory[0x00].Returns((byte)0x01);
        _memory[0x01].Returns((byte)0x02);
        _memory[0x02].Returns((byte)0xFF);
        _memory[0x03].Returns((byte)0x04);
    }

    private IMasterSystemMemory _memory;
    private IZ80FlagsManager _flags;
    private TestZ8016BitRegisterBase _register;

    [Test]
    public void WhenResetBitByRegisterLocationThenBitInDataIsResetAtMemoryLocation()
    {
        _register.Set(0x01);
        _register.ResetBitByRegisterLocation(3, 1);
        _memory.Received()[0x02] = 0xF7;
    }

    [Test]
    public void WhenSetBitByRegisterLocationThenBitInDataIsSetAtMemoryLocation()
    {
        _register.Set(0x02);
        _register.SetBitByRegisterLocation(3, 1);
        _memory.Received()[0x03] = 0x0C;
    }

    [Test]
    public void WhenTestBitByRegisterLocationAndBitSetThenFlagsAreSetAsExpected()
    {
        _register.Set(0x02);
        _register.TestBitByRegisterLocation(2, 1);
        _memory.DidNotReceive()[0x03] = 0x04;

        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ZeroZ, false);
        _flags.Received(1).SetFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);

        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.SignS, false);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, false);
    }

    [Test]
    public void WhenTestBitByRegisterLocationAndBitNotSetThenFlagsAreSetAsExpected()
    {
        _register.Set(0x02);
        _register.TestBitByRegisterLocation(3, 1);
        _memory.DidNotReceive()[0x03] = 0x04;

        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ZeroZ, true);
        _flags.Received(1).SetFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);

        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.SignS, false);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, true);
    }

    [Test]
    public void WhenTestBitByRegisterLocationAndBit7SetThenSignFlagSet()
    {
        _register.Set(0x02);
        _register.TestBitByRegisterLocation(7, 0);
        _memory.DidNotReceive()[0x02] = 0xFF;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.SignS, true);
    }

    [Test]
    [TestCase((ushort)0, (ushort)0, (ushort)0, false, false)]
    [TestCase((ushort)0x0001, (ushort)0x01, (ushort)0x0002, false, false)]
    [TestCase((ushort)0x00FF, (ushort)0x01, (ushort)0x0100, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x01, (ushort)0x1000, true, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x00FF, (ushort)0x10FE, true, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0000, true, true)]
    [TestCase((ushort)0x8000, (ushort)0x8000, (ushort)0x0000, false, true)]
    public void WhenAddWithoutCarry(ushort value, ushort operand, ushort newValue, bool expectedHalfCarryFlagStatus,
        bool expectedCarryFlagStatus)
    {
        _register.Set(value);
        _register.Add(operand);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryFlagStatus);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);
    }

    [Test]
    [TestCase((ushort)0, (ushort)0, (ushort)1, false, false, false)]
    [TestCase((ushort)0x0001, (ushort)0x01, (ushort)0x0003, false, false, false)]
    [TestCase((ushort)0x00FF, (ushort)0x01, (ushort)0x0101, false, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x01, (ushort)0x1001, true, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x00FF, (ushort)0x10FF, true, false, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0001, true, true, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0001, true, true, false)]
    [TestCase((ushort)0x8000, (ushort)0x9AAA, (ushort)0x1AAB, false, true, true)]
    public void WhenAddWithCarryAndCarrySet(ushort value, ushort operand, ushort newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(value);
        _register.Add(operand, true);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryFlagStatus);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((ushort)0, (ushort)0, (ushort)0, false, false, false)]
    [TestCase((ushort)0x0001, (ushort)0x01, (ushort)0x0002, false, false, false)]
    [TestCase((ushort)0x00FF, (ushort)0x01, (ushort)0x0100, false, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x01, (ushort)0x1000, true, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x00FF, (ushort)0x10FE, true, false, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0000, true, true, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0x0000, true, true, false)]
    [TestCase((ushort)0x8000, (ushort)0x9AAA, (ushort)0x1AAA, false, true, true)]
    public void WhenAddWithCarryAndCarryNotSet(ushort value, ushort operand, ushort newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(false);
        _register.Set(value);
        _register.Add(operand, true);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryFlagStatus);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((ushort)0, (ushort)0, (ushort)0xFFFF, true, true, false)]
    [TestCase((ushort)1, (ushort)1, (ushort)0xFFFF, true, true, false)]
    [TestCase((ushort)0x0002, (ushort)0x01, (ushort)0x0000, false, false, false)]
    [TestCase((ushort)0x00FF, (ushort)0x01, (ushort)0x00FD, false, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x01, (ushort)0x0FFD, false, false, false)]
    [TestCase((ushort)0x1000, (ushort)0x01, (ushort)0x0FFE, true, false, false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, (ushort)0xFFFD, false, false, false)]
    [TestCase((ushort)0xFFFF, (ushort)0xFFFF, (ushort)0xFFFF, true, true, false)]
    [TestCase((ushort)0x8000, (ushort)0x7F00, (ushort)0x00FF, true, false, true)]
    public void WhenSubtractWithCarryAndCarrySet(ushort value, ushort operand, ushort newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _register.Set(value);
        _register.Subtract(operand);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryFlagStatus);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    [Test]
    [TestCase((ushort)0, (ushort)0, (ushort)0, false, false, false)]
    [TestCase((ushort)0, (ushort)1, (ushort)0xFFFF, true, true, false)]
    [TestCase((ushort)0x0001, (ushort)0x01, (ushort)0x0000, false, false, false)]
    [TestCase((ushort)0x00FF, (ushort)0x01, (ushort)0x00FE, false, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x01, (ushort)0x0FFE, false, false, false)]
    [TestCase((ushort)0x1000, (ushort)0x01, (ushort)0x0FFF, true, false, false)]
    [TestCase((ushort)0x8000, (ushort)0x7F00, (ushort)0x0100, true, false, true)]
    public void WhenSubtractWithCarryAndCarryNotSet(ushort value, ushort operand, ushort newValue,
        bool expectedHalfCarryFlagStatus, bool expectedCarryFlagStatus, bool expectedParityFlagStatus)
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(false);
        _register.Set(value);
        _register.Subtract(operand);

        _register.Value.Should().Be(newValue);

        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, expectedHalfCarryFlagStatus);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, expectedCarryFlagStatus);

        _flags.Received(1).SetIfNegative(newValue);
        _flags.Received(1).SetIfZero(newValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, expectedParityFlagStatus);
    }

    // These are tested extensively else where, we just need to validate it is reading and writing the rotated/shifted values from/to memory

    [Test]
    public void WhenRotatingLeftCircularThenRotatedLeftWithCircularHandling()
    {
        _memory[0x1235].Returns((byte)0x87);
        _register.Set(0x1234);
        _register.RotateLeftCircular(1);
        _memory.Received()[0x1235] = 0x0F;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingRightCircularThenRotatedRightWithCircularHandling()
    {
        _memory[0x1235].Returns((byte)0x07);
        _register.Set(0x1234);
        _register.RotateRightCircular(1);
        _memory.Received()[0x1235] = 0x83;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingLeftThenRotatedLeft()
    {
        _memory[0x1235].Returns((byte)0x87);
        _register.Set(0x1234);
        _register.RotateLeft(1);
        _memory.Received()[0x1235] = 0x0E;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenRotatingRightThenRotatedRight()
    {
        _memory[0x1235].Returns((byte)0x07);
        _register.Set(0x1234);
        _register.RotateRight(1);
        _memory.Received()[0x1235] = 0x03;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftLeftArithmeticThenMemoryDataShiftedLeft()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _memory[0x1235].Returns((byte)0x87);
        _register.Set(0x1234);
        _register.ShiftLeftArithmetic(1);
        _memory.Received()[0x1235] = 0x0E;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftRightArithmeticThenMemoryDataShiftedRight()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _memory[0x1235].Returns((byte)0x07);
        _register.Set(0x1234);
        _register.ShiftRightArithmetic(1);
        _memory.Received()[0x1235] = 0x03;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftLeftLogicalThenMemoryDataShiftedLeft()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _memory[0x1235].Returns((byte)0x87);
        _register.Set(0x1234);
        _register.ShiftLeftLogical(1);
        _memory.Received()[0x1235] = 0x0F;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }

    [Test]
    public void WhenShiftRightLogicalThenMemoryDataShiftedRight()
    {
        _flags.IsFlagSet(Z80StatusFlags.CarryC).Returns(true);
        _memory[0x1235].Returns((byte)0x87);
        _register.Set(0x1234);
        _register.ShiftRightLogical(1);
        _memory.Received()[0x1235] = 0x43;
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.CarryC, true);
    }
}