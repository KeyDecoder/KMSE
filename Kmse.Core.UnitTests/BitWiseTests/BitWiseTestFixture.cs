using FluentAssertions;
using Kmse.Core.Utilities;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.BitWiseTests;

[TestFixture]
public class BitWiseTestFixture
{
    [Test]
    public void WhenCheckingBitWithInvalidBit()
    {
        var action = () => Bitwise.IsSet((byte)0xFF, 8);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenCheckingBitWithNegativeBit()
    {
        var action = () => Bitwise.IsSet((byte)0xFF, -1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenCheckingIntegerBitWithInvalidBit()
    {
        var action = () => Bitwise.IsSet(0xFF, 16);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenCheckingIntegerBitWithNegativeBit()
    {
        var action = () => Bitwise.IsSet(0xFF, -1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSettingBitWithInvalidBit()
    {
        byte test = 0;
        var action = () => Bitwise.Set(ref test, 8);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSettingBitWithNegativeBit()
    {
        byte test = 0;
        var action = () => Bitwise.Set(ref test, -1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSetIfBitWithInvalidBit()
    {
        byte test = 0;
        var action = () => Bitwise.SetIf(ref test, 8, () => false);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSetIfBitWithNegativeBit()
    {
        byte test = 0;
        var action = () => Bitwise.SetIf(ref test, -1, () => false);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenClearingBitWithInvalidBit()
    {
        byte test = 0;
        var action = () => Bitwise.Clear(ref test, 8);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenClearingBitWithNegativeBit()
    {
        byte test = 0;
        var action = () => Bitwise.Clear(ref test, -1);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSetOrClearIfBitWithInvalidBit()
    {
        byte test = 0;
        var action = () => Bitwise.SetOrClearIf(ref test, 8, () => false);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSetOrClearIfBitWithNegativeBit()
    {
        byte test = 0;
        var action = () => Bitwise.SetOrClearIf(ref test, -1, () => false);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static object[] _isSetTestCases =
    {
        new object[] { (byte)(1 << 0), 0, true },
        new object[] { (byte)(1 << 1), 1, true },
        new object[] { (byte)(1 << 2), 2, true },
        new object[] { (byte)(1 << 3), 3, true },
        new object[] { (byte)(1 << 4), 4, true },
        new object[] { (byte)(1 << 5), 5, true },
        new object[] { (byte)(1 << 6), 6, true },
        new object[] { (byte)(1 << 7), 7, true },

        new object[] { (byte)0x00, 0, false },
        new object[] { (byte)0x00, 1, false },
        new object[] { (byte)0x00, 2, false },
        new object[] { (byte)0x00, 3, false },
        new object[] { (byte)0x00, 4, false },
        new object[] { (byte)0x00, 5, false },
        new object[] { (byte)0x00, 6, false },
        new object[] { (byte)0x00, 7, false },

        new object[] { (byte)(1 << 0), 1, false },
        new object[] { (byte)(1 << 1), 2, false },
        new object[] { (byte)(1 << 2), 3, false },
        new object[] { (byte)(1 << 3), 4, false },
        new object[] { (byte)(1 << 4), 5, false },
        new object[] { (byte)(1 << 5), 6, false },
        new object[] { (byte)(1 << 6), 7, false },
        new object[] { (byte)(1 << 7), 0, false }
    };

    [Test]
    [TestCaseSource(nameof(_isSetTestCases))]
    public void WhenCheckingIfBitSet(byte source, int bit, bool isSet)
    {
        Bitwise.IsSet(source, bit).Should().Be(isSet);
    }

    private static object[] _isSetIntTestCases =
    {
        new object[] { 1 << 0, 0, true },
        new object[] { 1 << 1, 1, true },
        new object[] { 1 << 2, 2, true },
        new object[] { 1 << 3, 3, true },
        new object[] { 1 << 4, 4, true },
        new object[] { 1 << 5, 5, true },
        new object[] { 1 << 6, 6, true },
        new object[] { 1 << 7, 7, true },
        new object[] { 1 << 8, 8, true },
        new object[] { 1 << 9, 9, true },
        new object[] { 1 << 10, 10, true },
        new object[] { 1 << 11, 11, true },
        new object[] { 1 << 12, 12, true },
        new object[] { 1 << 13, 13, true },
        new object[] { 1 << 14, 14, true },
        new object[] { 1 << 15, 15, true },

        new object[] { 0x00, 0, false },
        new object[] { 0x00, 1, false },
        new object[] { 0x00, 2, false },
        new object[] { 0x00, 3, false },
        new object[] { 0x00, 4, false },
        new object[] { 0x00, 5, false },
        new object[] { 0x00, 6, false },
        new object[] { 0x00, 7, false },
        new object[] { 0x00, 8, false },
        new object[] { 0x00, 9, false },
        new object[] { 0x00, 10, false },
        new object[] { 0x00, 11, false },
        new object[] { 0x00, 12, false },
        new object[] { 0x00, 13, false },
        new object[] { 0x00, 14, false },
        new object[] { 0x00, 15, false },

        new object[] { 1 << 0, 1, false },
        new object[] { 1 << 1, 2, false },
        new object[] { 1 << 2, 3, false },
        new object[] { 1 << 3, 4, false },
        new object[] { 1 << 4, 5, false },
        new object[] { 1 << 5, 6, false },
        new object[] { 1 << 6, 7, false },
        new object[] { 1 << 7, 8, false },
        new object[] { 1 << 8, 9, false },
        new object[] { 1 << 9, 10, false },
        new object[] { 1 << 10, 11, false },
        new object[] { 1 << 11, 12, false },
        new object[] { 1 << 12, 13, false },
        new object[] { 1 << 13, 14, false },
        new object[] { 1 << 14, 15, false },
        new object[] { 1 << 15, 0, false }
    };

    [Test]
    [TestCaseSource(nameof(_isSetIntTestCases))]
    public void WhenCheckingIntIfBitSet(int source, int bit, bool isSet)
    {
        Bitwise.IsSet(source, bit).Should().Be(isSet);
    }

    private static object[] _setBitTestCases =
    {
        new object[] { (byte)0x00, 0, (byte)0x01 },
        new object[] { (byte)0x00, 1, (byte)0x02 },
        new object[] { (byte)0x00, 2, (byte)0x04 },
        new object[] { (byte)0x00, 3, (byte)0x08 },
        new object[] { (byte)0x00, 4, (byte)0x10 },
        new object[] { (byte)0x00, 5, (byte)0x20 },
        new object[] { (byte)0x00, 6, (byte)0x40 },
        new object[] { (byte)0x00, 7, (byte)0x80 },

        new object[] { unchecked((byte)~0x01), 0, (byte)0xFF },
        new object[] { unchecked((byte)~0x02), 1, (byte)0xFF },
        new object[] { unchecked((byte)~0x04), 2, (byte)0xFF },
        new object[] { unchecked((byte)~0x08), 3, (byte)0xFF },
        new object[] { unchecked((byte)~0x10), 4, (byte)0xFF },
        new object[] { unchecked((byte)~0x20), 5, (byte)0xFF },
        new object[] { unchecked((byte)~0x40), 6, (byte)0xFF },
        new object[] { unchecked((byte)~0x80), 7, (byte)0xFF }
    };

    [Test]
    [TestCaseSource(nameof(_setBitTestCases))]
    public void WhenSettingBit(byte source, int bit, byte expectedValue)
    {
        Bitwise.Set(ref source, bit);
        source.Should().Be(expectedValue);
    }

    private static object[] _clearBitTestCases =
    {
        new object[] { (byte)0x01, 0, (byte)0x00 },
        new object[] { (byte)0x02, 1, (byte)0x00 },
        new object[] { (byte)0x04, 2, (byte)0x00 },
        new object[] { (byte)0x08, 3, (byte)0x00 },
        new object[] { (byte)0x10, 4, (byte)0x00 },
        new object[] { (byte)0x20, 5, (byte)0x00 },
        new object[] { (byte)0x40, 6, (byte)0x00 },
        new object[] { (byte)0x80, 7, (byte)0x00 },

        new object[] { (byte)0xFF, 0, unchecked((byte)~0x01) },
        new object[] { (byte)0xFF, 1, unchecked((byte)~0x02) },
        new object[] { (byte)0xFF, 2, unchecked((byte)~0x04) },
        new object[] { (byte)0xFF, 3, unchecked((byte)~0x08) },
        new object[] { (byte)0xFF, 4, unchecked((byte)~0x10) },
        new object[] { (byte)0xFF, 5, unchecked((byte)~0x20) },
        new object[] { (byte)0xFF, 6, unchecked((byte)~0x40) },
        new object[] { (byte)0xFF, 7, unchecked((byte)~0x80) }
    };

    [Test]
    [TestCaseSource(nameof(_clearBitTestCases))]
    public void WhenClearingBit(byte source, int bit, byte expectedValue)
    {
        Bitwise.Clear(ref source, bit);
        source.Should().Be(expectedValue);
    }

    private static object[] _setIfBitTestCases =
    {
        new object[] { (byte)0x00, 0, (byte)0x01, () => true },
        new object[] { (byte)0x00, 1, (byte)0x02, () => true },
        new object[] { (byte)0x00, 2, (byte)0x04, () => true },
        new object[] { (byte)0x00, 3, (byte)0x08, () => true },
        new object[] { (byte)0x00, 4, (byte)0x10, () => true },
        new object[] { (byte)0x00, 5, (byte)0x20, () => true },
        new object[] { (byte)0x00, 6, (byte)0x40, () => true },
        new object[] { (byte)0x00, 7, (byte)0x80, () => true },

        new object[] { unchecked((byte)~0x01), 0, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x02), 1, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x04), 2, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x08), 3, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x10), 4, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x20), 5, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x40), 6, (byte)0xFF, () => true },
        new object[] { unchecked((byte)~0x80), 7, (byte)0xFF, () => true },

        new object[] { (byte)0x00, 0, (byte)0x00, () => false },
        new object[] { (byte)0x00, 1, (byte)0x00, () => false },
        new object[] { (byte)0x00, 2, (byte)0x00, () => false },
        new object[] { (byte)0x00, 3, (byte)0x00, () => false },
        new object[] { (byte)0x00, 4, (byte)0x00, () => false },
        new object[] { (byte)0x00, 5, (byte)0x00, () => false },
        new object[] { (byte)0x00, 6, (byte)0x00, () => false },
        new object[] { (byte)0x00, 7, (byte)0x00, () => false },

        new object[] { unchecked((byte)~0x01), 0, unchecked((byte)~0x01), () => false },
        new object[] { unchecked((byte)~0x02), 1, unchecked((byte)~0x02), () => false },
        new object[] { unchecked((byte)~0x04), 2, unchecked((byte)~0x04), () => false },
        new object[] { unchecked((byte)~0x08), 3, unchecked((byte)~0x08), () => false },
        new object[] { unchecked((byte)~0x10), 4, unchecked((byte)~0x10), () => false },
        new object[] { unchecked((byte)~0x20), 5, unchecked((byte)~0x20), () => false },
        new object[] { unchecked((byte)~0x40), 6, unchecked((byte)~0x40), () => false },
        new object[] { unchecked((byte)~0x80), 7, unchecked((byte)~0x80), () => false }
    };

    [Test]
    [TestCaseSource(nameof(_setIfBitTestCases))]
    public void WhenSettingBitIf(byte source, int bit, byte expectedValue, Func<bool> func)
    {
        Bitwise.SetIf(ref source, bit, func);
        source.Should().Be(expectedValue);
    }

    private static object[] _setClearIfBitTestCases =
    {
        new object[] { (byte)0x00, 0, (byte)0x01, () => true },
        new object[] { (byte)0x01, 0, (byte)0x00, () => false },

        new object[] { (byte)0x00, 7, (byte)0x80, () => true },
        new object[] { (byte)0x80, 7, (byte)0x00, () => false },

        new object[] { (byte)0x7F, 7, (byte)0xFF, () => true },
        new object[] { (byte)0xFF, 7, (byte)0x7F, () => false }
    };

    [Test]
    [TestCaseSource(nameof(_setClearIfBitTestCases))]
    public void WhenSettingOrClearingBitIf(byte source, int bit, byte expectedValue, Func<bool> func)
    {
        var output = source;
        Bitwise.SetOrClearIf(ref output, bit, func);
        output.Should().Be(expectedValue);
    }

    private static object[] _toUnsignedShortTestCases =
    {
        new object[] { (byte)0, (byte)0, (ushort)0 },
        new object[] { (byte)1, (byte)1, (ushort)0x0101 },
        new object[] { (byte)1, (byte)0xFF, (ushort)0x01FF },
        new object[] { (byte)0xFF, (byte)0x01, (ushort)0xFF01 },
        new object[] { (byte)0xAB, (byte)0xCD, (ushort)0xABCD },
        new object[] { (byte)0xFF, (byte)0xFF, (ushort)0xFFFF }
    };

    [Test]
    [TestCaseSource(nameof(_toUnsignedShortTestCases))]
    public void WhenCheckingIntIfBitSet(byte high, byte low, ushort value)
    {
        Bitwise.ToUnsigned16BitValue(high, low).Should().Be(value);
    }
}