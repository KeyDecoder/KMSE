using FluentAssertions;
using Kmse.Core.Z80.Model;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

[TestFixture]
public class CpuInstructionsFixture : CpuTestFixtureBase
{
    private static object[] _jrTestCases =
    {
        // This is 500 + 2 + offset (the 2 is for the instruction)
        new object[] { (byte)0, (ushort)502 },
        new object[] { (byte)1, (ushort)503 },
        new object[] { (byte)2, (ushort)504 },
        new object[] { (byte)3, (ushort)505 },
        new object[] { (byte)129, (ushort)631 },
        // Since 130 is actually -126 here, we go backwards
        new object[] { (byte)130, (ushort)376 },
        // Since 255 is actually -1 here, we go backwards by 1
        new object[] { (byte)255, (ushort)501 }
    };

    [Test]
    [TestCaseSource(nameof(_jrTestCases))]
    public void WhenExecutingRelativeJump(byte offset, ushort expectedPc)
    {
        PrepareForTest();

        // Execute at least 500 instructions
        for (var i = 0; i < 500; i++)
        {
            _memory[(ushort)i].Returns((byte)0x00);
            _cpu.ExecuteNextCycle().Should().Be(4);
        }

        _memory[500].Returns((byte)0x18);
        _memory[501].Returns(offset);
        _cpu.ExecuteNextCycle().Should().Be(12);

        var status = _cpu.GetStatus();
        status.Pc.Should().Be(expectedPc);
    }

    [Test]
    public void WhenExecutingAddWithCarryWithNoCarry()
    {
        for (var i = 0; i < 256; i++)
        {
            PrepareForTest();

            var originalValue = (byte)i;
            var valueToAdd = i + 1;

            //ld a, 127
            _memory[0].Returns((byte)0x3E);
            _memory[1].Returns((byte)i);
            //adc a, a
            _memory[2].Returns((byte)0x8F);

            // Execute ld
            _cpu.ExecuteNextCycle().Should().Be(7);
            _cpu.GetStatus().Af.High.Should().Be((byte)i);

            // Execute adc
            _cpu.ExecuteNextCycle().Should().Be(4);

            var status = _cpu.GetStatus();
            status.Pc.Should().Be(0x03);

            var expectedValue = (byte)(i + i);
            var result = originalValue + valueToAdd;
            var foundValue = _cpu.GetStatus().Af.High;
            foundValue.Should().Be(expectedValue);

            var signFlagSet = expectedValue >> 7 == 0x01;
            var zeroFlagSet = expectedValue == 0;
            var hFlagSet = ((originalValue ^ result ^ originalValue) & 0x10) != 0;
            var pvFlagSet = ((originalValue ^ originalValue ^ 0x80) & (originalValue ^ result) & 0x80) != 0;
            var carryFlagSet = result > 0xFF;

            IsFlagSet(signFlagSet, status.Af.Low, Z80StatusFlags.SignS);
            IsFlagSet(zeroFlagSet, status.Af.Low, Z80StatusFlags.ZeroZ);
            IsFlagSet(hFlagSet, status.Af.Low, Z80StatusFlags.HalfCarryH);
            IsFlagSet(pvFlagSet, status.Af.Low, Z80StatusFlags.ParityOverflowPV);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.AddSubtractN);
            IsFlagSet(carryFlagSet, status.Af.Low, Z80StatusFlags.CarryC);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.NotUsedX3);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.NotUsedX5);
        }
    }

    [Test]
    public void WhenExecutingAddWithCarryWithCarry()
    {
        for (var i = 0; i < 256; i++)
        {
            PrepareForTest();

            var originalValue = (byte)i;
            var valueToAdd = i + 1;

            // scf to set carry flag
            _memory[0].Returns((byte)0x37);

            //ld a, i
            _memory[1].Returns((byte)0x3E);
            _memory[2].Returns((byte)i);

            //adc a, a
            _memory[3].Returns((byte)0x8F);

            // execute scf
            _cpu.ExecuteNextCycle().Should().Be(4);

            // Execute ld
            _cpu.ExecuteNextCycle().Should().Be(7);
            _cpu.GetStatus().Af.High.Should().Be((byte)i);

            // Execute adc
            _cpu.ExecuteNextCycle().Should().Be(4);

            var status = _cpu.GetStatus();
            status.Pc.Should().Be(0x04);

            var expectedValue = (byte)(originalValue + valueToAdd);
            var result = originalValue + valueToAdd;
            var foundValue = _cpu.GetStatus().Af.High;
            foundValue.Should().Be(expectedValue);

            var signFlagSet = expectedValue >> 7 == 0x01;
            var zeroFlagSet = expectedValue == 0;
            var hFlagSet = ((originalValue ^ result ^ originalValue) & 0x10) != 0;
            var pvFlagSet = ((originalValue ^ originalValue ^ 0x80) & (originalValue ^ result) & 0x80) != 0;
            var carryFlagSet = result > 0xFF;

            IsFlagSet(signFlagSet, status.Af.Low, Z80StatusFlags.SignS);
            IsFlagSet(zeroFlagSet, status.Af.Low, Z80StatusFlags.ZeroZ);
            IsFlagSet(hFlagSet, status.Af.Low, Z80StatusFlags.HalfCarryH);
            IsFlagSet(pvFlagSet, status.Af.Low, Z80StatusFlags.ParityOverflowPV);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.AddSubtractN);
            IsFlagSet(carryFlagSet, status.Af.Low, Z80StatusFlags.CarryC);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.NotUsedX3);
            IsFlagSet(false, status.Af.Low, Z80StatusFlags.NotUsedX5);
        }
    }

    private void IsFlagSet(bool check, byte status, Z80StatusFlags flags)
    {
        var currentSetFlags = (Z80StatusFlags)status & flags;
        var result = currentSetFlags == flags;
        result.Should().Be(check);
    }
}