using FluentAssertions;
using Kmse.Core.Utilities;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.BitWiseTests;

[TestFixture]
public class BitWiseTestFixture
{
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
}