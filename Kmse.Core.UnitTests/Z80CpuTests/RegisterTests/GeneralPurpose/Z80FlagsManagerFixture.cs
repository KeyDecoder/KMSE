using FluentAssertions;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers.General;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class Z80FlagsManagerFixture
{
    [SetUp]
    public void Setup()
    {
        _register = new Z80FlagsManager();
    }

    private Z80FlagsManager _register;

    [Test]
    public void WhenSettingValueThenValueIsUpdated()
    {
        _register.Set(0x05);
        _register.Value.Should().Be(0x05);
    }

    [Test]
    public void WhenResettingRegisterThenValueIsZero()
    {
        _register.Set(0x05);
        _register.SwapWithShadow();
        _register.Set(0x01);
        _register.Reset();
        _register.Value.Should().Be(0x00);
        _register.ShadowValue.Should().Be(0x00);
    }

    [Test]
    public void WhenSwappingValueWithShadow()
    {
        _register.Set(0x05);
        _register.SwapWithShadow();
        _register.Value.Should().Be(0x00);
        _register.ShadowValue.Should().Be(0x05);
    }

    [Test]
    public void WhenSwappingValueBackFromShadow()
    {
        _register.Set(0x12);
        _register.SwapWithShadow();
        _register.Set(0x33);
        _register.SwapWithShadow();

        _register.Value.Should().Be(0x12);
        _register.ShadowValue.Should().Be(0x33);
    }

    [Test]
    [TestCase(1 << 0, Z80StatusFlags.CarryC)]
    [TestCase(1 << 1, Z80StatusFlags.AddSubtractN)]
    [TestCase(1 << 2, Z80StatusFlags.ParityOverflowPV)]
    [TestCase(1 << 3, Z80StatusFlags.NotUsedX3)]
    [TestCase(1 << 4, Z80StatusFlags.HalfCarryH)]
    [TestCase(1 << 5, Z80StatusFlags.NotUsedX5)]
    [TestCase(1 << 6, Z80StatusFlags.ZeroZ)]
    [TestCase(1 << 7, Z80StatusFlags.SignS)]
    [TestCase(0xAA,
        Z80StatusFlags.AddSubtractN | Z80StatusFlags.NotUsedX3 | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.SignS)]
    [TestCase(0xFF,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS)]
    public void WhenSettingFlagThenValueIsUpdatedWithExpectedValue(byte expectedValue, Z80StatusFlags flags)
    {
        _register.SetFlag(flags);
        _register.Value.Should().Be(expectedValue);
    }

    [Test]
    // We test by setting all flags and then validating the correct bit was cleared
    // To do this we take the bit, bit shift it up to the correct spot
    // and then do a NOT (~) to set all bits except the one we want to ensure cleared
    [TestCase(unchecked((byte)~(1 << 0)), Z80StatusFlags.CarryC)]
    [TestCase(unchecked((byte)~(1 << 1)), Z80StatusFlags.AddSubtractN)]
    [TestCase(unchecked((byte)~(1 << 2)), Z80StatusFlags.ParityOverflowPV)]
    [TestCase(unchecked((byte)~(1 << 3)), Z80StatusFlags.NotUsedX3)]
    [TestCase(unchecked((byte)~(1 << 4)), Z80StatusFlags.HalfCarryH)]
    [TestCase(unchecked((byte)~(1 << 5)), Z80StatusFlags.NotUsedX5)]
    [TestCase(unchecked((byte)~(1 << 6)), Z80StatusFlags.ZeroZ)]
    [TestCase(unchecked((byte)~(1 << 7)), Z80StatusFlags.SignS)]
    [TestCase(unchecked((byte)~0xAA),
        Z80StatusFlags.AddSubtractN | Z80StatusFlags.NotUsedX3 | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.SignS)]
    [TestCase(0x00,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS)]
    public void WhenClearingFlagThenValueIsUpdatedWithExpectedValue(byte expectedValue, Z80StatusFlags flags)
    {
        _register.Set(0xFF);
        _register.ClearFlag(flags);
        _register.Value.Should().Be(expectedValue);
    }

    [Test]
    [TestCase(unchecked((byte)~(1 << 0)), Z80StatusFlags.CarryC)]
    [TestCase(unchecked((byte)~(1 << 1)), Z80StatusFlags.AddSubtractN)]
    [TestCase(unchecked((byte)~(1 << 2)), Z80StatusFlags.ParityOverflowPV)]
    [TestCase(unchecked((byte)~(1 << 3)), Z80StatusFlags.NotUsedX3)]
    [TestCase(unchecked((byte)~(1 << 4)), Z80StatusFlags.HalfCarryH)]
    [TestCase(unchecked((byte)~(1 << 5)), Z80StatusFlags.NotUsedX5)]
    [TestCase(unchecked((byte)~(1 << 6)), Z80StatusFlags.ZeroZ)]
    [TestCase(unchecked((byte)~(1 << 7)), Z80StatusFlags.SignS)]
    [TestCase(unchecked((byte)~0xAA),
        Z80StatusFlags.AddSubtractN | Z80StatusFlags.NotUsedX3 | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.SignS)]
    [TestCase(0x00,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS)]
    public void WhenInvertingSetFlagThenValueIsUpdatedWithExpectedValue(byte expectedValue, Z80StatusFlags flags)
    {
        // In this case, if we set all the flags and then invert a specific flag it's the same as clearing the flag
        _register.Set(0xFF);
        _register.InvertFlag(flags);
        _register.Value.Should().Be(expectedValue);
    }

    [Test]
    [TestCase(1 << 0, Z80StatusFlags.CarryC)]
    [TestCase(1 << 1, Z80StatusFlags.AddSubtractN)]
    [TestCase(1 << 2, Z80StatusFlags.ParityOverflowPV)]
    [TestCase(1 << 3, Z80StatusFlags.NotUsedX3)]
    [TestCase(1 << 4, Z80StatusFlags.HalfCarryH)]
    [TestCase(1 << 5, Z80StatusFlags.NotUsedX5)]
    [TestCase(1 << 6, Z80StatusFlags.ZeroZ)]
    [TestCase(1 << 7, Z80StatusFlags.SignS)]
    [TestCase(0xAA,
        Z80StatusFlags.AddSubtractN | Z80StatusFlags.NotUsedX3 | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.SignS)]
    [TestCase(0xFF,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS)]
    public void WhenInvertingClearedFlagThenValueIsUpdatedWithExpectedValue(byte expectedValue, Z80StatusFlags flags)
    {
        // In this case, if we clear all the flags and then invert a specific flag it's the same as setting the flag
        _register.Set(0x00);
        _register.InvertFlag(flags);
        _register.Value.Should().Be(expectedValue);
    }

    [Test]
    public void WhenSettingOrClearingFlagOnConditionalTrueThenFlagIsSet()
    {
        _register.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, true);
        _register.Value.Should().Be(1 << 2);
    }

    [Test]
    public void WhenSettingOrClearingFlagOnConditionalFalseThenFlagIsCleared()
    {
        _register.Set(0xFF);
        _register.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, false);
        _register.Value.Should().Be(unchecked((byte)~(1 << 2)));
    }

    [Test]
    [TestCase(0, Z80StatusFlags.CarryC | Z80StatusFlags.NotUsedX5, false)]
    [TestCase(0x01, Z80StatusFlags.CarryC, true)]
    // False since only 1 flag is set not both
    [TestCase(0x01, Z80StatusFlags.CarryC | Z80StatusFlags.HalfCarryH, false)]
    [TestCase(0x81, Z80StatusFlags.CarryC | Z80StatusFlags.SignS, true)]
    [TestCase(0xFE,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS, false)]
    [TestCase(0xFF,
        Z80StatusFlags.CarryC | Z80StatusFlags.AddSubtractN | Z80StatusFlags.ParityOverflowPV |
        Z80StatusFlags.NotUsedX3 | Z80StatusFlags.HalfCarryH | Z80StatusFlags.NotUsedX5 | Z80StatusFlags.ZeroZ |
        Z80StatusFlags.SignS, true)]
    public void WhenCheckingIfSetFlagThenReturnExpectedResult(byte value, Z80StatusFlags flags, bool expectedResult)
    {
        _register.Set(value);
        _register.IsFlagSet(flags).Should().Be(expectedResult);
    }

    [Test]
    [TestCase(0x00, true)]
    [TestCase(0x01, false)]
    [TestCase(0x03, true)]
    [TestCase(0xAB, false)]
    [TestCase(0xFE, false)]
    [TestCase(0xFF, true)]
    public void WhenSettingParityFromValueThenFlagIsSetIfBitsEven(byte value, bool parityFlagSet)
    {
        _register.SetParityFromValue(value);
        _register.IsFlagSet(Z80StatusFlags.ParityOverflowPV).Should().Be(parityFlagSet);
    }

    [Test]
    [TestCase(0x00, true)]
    [TestCase(0x01, false)]
    [TestCase(0xFF, false)]
    public void WhenSettingZeroFlagFromByteValueThenFlagIsSetIfValueZero(byte value, bool flagSet)
    {
        _register.SetIfZero(value);
        _register.IsFlagSet(Z80StatusFlags.ZeroZ).Should().Be(flagSet);
    }

    [Test]
    [TestCase((ushort)0x0000, true)]
    [TestCase((ushort)0x0001, false)]
    [TestCase((ushort)0x00FF, false)]
    [TestCase((ushort)0xFFFF, false)]
    public void WhenSettingZeroFlagFromUnsignedShortValueThenFlagIsSetIfValueZero(ushort value, bool flagSet)
    {
        _register.SetIfZero(value);
        _register.IsFlagSet(Z80StatusFlags.ZeroZ).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x0000, true)]
    [TestCase(0x0001, false)]
    [TestCase(0x0000FF, false)]
    [TestCase(0xFFFFFF, false)]
    public void WhenSettingZeroFlagFromIntValueThenFlagIsSetIfValueZero(int value, bool flagSet)
    {
        _register.SetIfZero(value);
        _register.IsFlagSet(Z80StatusFlags.ZeroZ).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x00, false)]
    [TestCase(0x01, false)]
    [TestCase(0x80, true)]
    [TestCase(0x81, true)]
    [TestCase(0xFF, true)]
    public void WhenSettingSignFlagFromByteValueThenFlagIsSetIfValueNegative(byte value, bool flagSet)
    {
        _register.SetIfNegative(value);
        _register.IsFlagSet(Z80StatusFlags.SignS).Should().Be(flagSet);
    }

    [Test]
    [TestCase((ushort)0x0000, false)]
    [TestCase((ushort)0x0001, false)]
    [TestCase((ushort)0x0080, false)]
    [TestCase((ushort)0x00FF, false)]
    [TestCase((ushort)0x8000, true)]
    [TestCase((ushort)0x8001, true)]
    [TestCase((ushort)0xFFFF, true)]
    public void WhenSettingSignFlagFromUnsignedShortValueThenFlagIsSetIfValueNegative(ushort value, bool flagSet)
    {
        _register.SetIfNegative(value);
        _register.IsFlagSet(Z80StatusFlags.SignS).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x0000, false)]
    [TestCase(0x0001, false)]
    [TestCase(0x0080, true)]
    [TestCase(0x0081, true)]
    [TestCase(0x00FF, true)]
    [TestCase(0x8000, false)]
    [TestCase(0x8080, true)]
    [TestCase(0xFFFF, true)]
    public void WhenSettingSignFlagFromTwosComplementIntValueThenFlagIsSetIfValueNegative(int value, bool flagSet)
    {
        _register.SetIfTwosComplementNegative(value);
        _register.IsFlagSet(Z80StatusFlags.SignS).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x00, false)]
    [TestCase(0x01, false)]
    [TestCase(0x7E, false)]
    [TestCase(0x7F, true)]
    [TestCase(0x80, false)]
    [TestCase(0xFF, false)]
    public void WhenSettingOverflowFlagFromIncrementedValueThenFlagIsSetIfValueIs7F(byte value, bool flagSet)
    {
        _register.SetIfIncrementOverflow(value);
        _register.IsFlagSet(Z80StatusFlags.ParityOverflowPV).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x00, false)]
    [TestCase(0x01, false)]
    [TestCase(0x7F, false)]
    [TestCase(0x80, true)]
    [TestCase(0x81, false)]
    [TestCase(0xFF, false)]
    public void WhenSettingOverflowFlagFromDecrementedValueThenFlagIsSetIfValueIs80(byte value, bool flagSet)
    {
        _register.SetIfDecrementOverflow(value);
        _register.IsFlagSet(Z80StatusFlags.ParityOverflowPV).Should().Be(flagSet);
    }

    [Test]
    [TestCase(0x00, 0x00, 0x00, false)]
    [TestCase(0x07, 0x07, 0x0E, false)]
    [TestCase(0x07, 0x08, 0x0F, false)]
    [TestCase(0x0F, 0x00, 0x0F, false)]
    [TestCase(0x08, 0x08, 0x10, true)]
    [TestCase(0x0F, 0x01, 0x10, true)]
    [TestCase(0xFF, 0xFF, 0x1FE, true)]
    public void WhenSettingHalfCarryFlagFromChangedValueThenFlagIsSetIfHalfCarry(byte currentValue, byte operand,
        int changedValue, bool flagSet)
    {
        _register.SetIfHalfCarry(currentValue, operand, changedValue);
        _register.IsFlagSet(Z80StatusFlags.HalfCarryH).Should().Be(flagSet);
    }
}