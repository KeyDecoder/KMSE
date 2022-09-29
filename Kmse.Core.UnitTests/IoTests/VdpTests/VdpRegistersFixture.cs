using FluentAssertions;
using Kmse.Core.IO.Vdp;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Registers;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.IoTests.VdpTests;

[TestFixture]
public class VdpRegistersFixture
{
    [SetUp]
    public void Setup()
    {
        _registers = new VdpRegisters();
    }

    private VdpRegisters _registers;

    [Test]
    public void WhenSettingRegistersWithInvalidIndex()
    {
        var indexTooBig = () => _registers.SetRegister(12, 1);
        var indexNegative = () => _registers.SetRegister(-1, 1);

        indexTooBig.Should().Throw<ArgumentOutOfRangeException>();
        indexNegative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void WhenSettingRegisters()
    {
        for (var i = 0; i < 11; i++)
        {
            _registers.SetRegister(i, (byte)(i + 1));
        }

        var rawRegisters = _registers.DumpRegisters();
        for (var i = 0; i < rawRegisters.Length; i++)
        {
            rawRegisters[i].Should().Be((byte)(i + 1));
        }
    }

    [Test]
    public void WhenRegistersAreSet()
    {
        for (var i = 0; i < 11; i++)
        {
            _registers.SetRegister(i, 0xFF);
        }

        _registers.IsVerticalScrollingEnabledForColumns24To31().Should().BeFalse();
        _registers.IsHorizontalScrollingEnabledForRows0To1().Should().BeFalse();
        _registers.MaskColumn0WithOverscanColor().Should().BeTrue();
        _registers.IsLineInterruptEnabled().Should().BeTrue();
        _registers.ShiftSpritesLeftBy8Pixels().Should().BeTrue();
        _registers.GetVideoMode().Should().Be(VdpVideoMode.Mode4);
        _registers.IsNoSyncAndMonochrome().Should().BeTrue();

        _registers.IsDisplayVisible().Should().BeTrue();
        _registers.IsFrameInterruptEnabled().Should().BeTrue();
        _registers.IsSprites16By16().Should().BeTrue();
        _registers.IsSprites8By16().Should().BeTrue();
        _registers.IsSpritePixelsDoubledInSize().Should().BeTrue();

        _registers.GetNameTableBaseAddressOffset().Should().Be(0x3800);
        _registers.GetSpriteAttributeTableBaseAddressOffset().Should().Be(0x3F00);
        _registers.GetSpritePatternGeneratorBaseAddressOffset().Should().Be(0x2000);
        _registers.GetOverscanBackdropColour().Should().Be(0x0F);
    }

    [Test]
    public void WhenSettingDefault()
    {
        _registers.SetRegister(0x00, 0x36);
        _registers.SetRegister(0x01, 0x80);
        _registers.SetRegister(0x02, 0xFF);
        _registers.SetRegister(0x03, 0xFF);
        _registers.SetRegister(0x04, 0xFF);
        _registers.SetRegister(0x05, 0xFF);
        _registers.SetRegister(0x06, 0xFB);
        _registers.SetRegister(0x07, 0x00);
        _registers.SetRegister(0x08, 0x00);
        _registers.SetRegister(0x09, 0x00);
        _registers.SetRegister(0x0A, 0xFF);

        _registers.IsVerticalScrollingEnabledForColumns24To31().Should().BeTrue();
        _registers.IsHorizontalScrollingEnabledForRows0To1().Should().BeTrue();
        _registers.MaskColumn0WithOverscanColor().Should().BeTrue();
        _registers.IsLineInterruptEnabled().Should().BeTrue();
        _registers.ShiftSpritesLeftBy8Pixels().Should().BeFalse();
        _registers.GetVideoMode().Should().Be(VdpVideoMode.Mode4);
        _registers.IsNoSyncAndMonochrome().Should().BeFalse();

        _registers.IsDisplayVisible().Should().BeFalse();
        _registers.IsFrameInterruptEnabled().Should().BeFalse();
        _registers.IsSprites16By16().Should().BeFalse();
        _registers.IsSprites8By16().Should().BeFalse();
        _registers.IsSpritePixelsDoubledInSize().Should().BeFalse();

        _registers.GetNameTableBaseAddressOffset().Should().Be(0x3800);
        _registers.GetSpriteAttributeTableBaseAddressOffset().Should().Be(0x3F00);
        _registers.GetSpritePatternGeneratorBaseAddressOffset().Should().Be(0x000);
        _registers.GetOverscanBackdropColour().Should().Be(0x00);
    }

    [Test]
    public void WhenSettingRegister8ThenBackgroundXScrollValueIsUpdated()
    {
        _registers.SetRegister(8, 0x04);
        _registers.GetBackgroundXScroll().Should().Be(0x04);
    }

    [Test]
    public void WhenSettingRegister9ThenBackgroundXScrollValueIsUpdated()
    {
        _registers.SetRegister(9, 0x03);
        _registers.GetBackgroundYScroll().Should().Be(0x03);
    }

    [Test]
    public void WhenSettingRegister10ThenLineCounterValueIsUpdated()
    {
        _registers.SetRegister(10, 0x05);
        _registers.GetLineCounterValue().Should().Be(0x05);
    }

    [Test]
    public void WhenRegistersAreClear()
    {
        for (var i = 0; i < 11; i++)
        {
            _registers.SetRegister(i, 0xFF);
        }

        for (var i = 0; i < 11; i++)
        {
            _registers.SetRegister(i, 0x00);
        }

        _registers.IsVerticalScrollingEnabledForColumns24To31().Should().BeTrue();
        _registers.IsHorizontalScrollingEnabledForRows0To1().Should().BeTrue();
        _registers.MaskColumn0WithOverscanColor().Should().BeFalse();
        _registers.IsLineInterruptEnabled().Should().BeFalse();
        _registers.ShiftSpritesLeftBy8Pixels().Should().BeFalse();
        _registers.GetVideoMode().Should().Be(VdpVideoMode.Graphic1);
        _registers.IsNoSyncAndMonochrome().Should().BeFalse();

        _registers.IsDisplayVisible().Should().BeFalse();
        _registers.IsFrameInterruptEnabled().Should().BeFalse();
        _registers.IsSprites16By16().Should().BeFalse();
        _registers.IsSprites8By16().Should().BeFalse();
        _registers.IsSpritePixelsDoubledInSize().Should().BeFalse();

        _registers.GetNameTableBaseAddressOffset().Should().Be(0x0000);
        _registers.GetSpriteAttributeTableBaseAddressOffset().Should().Be(0x00);
        _registers.GetSpritePatternGeneratorBaseAddressOffset().Should().Be(0x00);
        _registers.GetOverscanBackdropColour().Should().Be(0x00);
        _registers.GetBackgroundXScroll().Should().Be(0x00);
        _registers.GetBackgroundYScroll().Should().Be(0x00);
        _registers.GetLineCounterValue().Should().Be(0x00);
    }

    [Test]
    [TestCase(0x04, 0x00, 0x0E, VdpVideoMode.Mode4, (ushort)0x3800)]
    [TestCase(0x06, 0x00, 0x0E, VdpVideoMode.Mode4, (ushort)0x3800)]
    [TestCase(0x04, 0x08, 0x0E, VdpVideoMode.Mode4, (ushort)0x3800)]
    [TestCase(0x06, 0x18, 0x0E, VdpVideoMode.Mode4, (ushort)0x3800)]
    [TestCase(0x06, 0x10, 0x00, VdpVideoMode.Mode4With224Lines, (ushort)0x0700)]
    [TestCase(0x06, 0x10, 0x02, VdpVideoMode.Mode4With224Lines, (ushort)0x0700)]
    [TestCase(0x06, 0x10, 0x04, VdpVideoMode.Mode4With224Lines, (ushort)0x1700)]
    [TestCase(0x06, 0x10, 0x08, VdpVideoMode.Mode4With224Lines, (ushort)0x2700)]
    [TestCase(0x06, 0x10, 0x0E, VdpVideoMode.Mode4With224Lines, (ushort)0x3700)]
    [TestCase(0x06, 0x08, 0x00, VdpVideoMode.Mode4With240Lines, (ushort)0x0700)]
    [TestCase(0x06, 0x08, 0x02, VdpVideoMode.Mode4With240Lines, (ushort)0x0700)]
    [TestCase(0x06, 0x08, 0x04, VdpVideoMode.Mode4With240Lines, (ushort)0x1700)]
    [TestCase(0x06, 0x08, 0x08, VdpVideoMode.Mode4With240Lines, (ushort)0x2700)]
    [TestCase(0x06, 0x08, 0x0E, VdpVideoMode.Mode4With240Lines, (ushort)0x3700)]
    public void WhenSettingVideoModeToMode4(byte register0, byte register1, byte register2, VdpVideoMode mode,
        ushort nameTableBaseAddressOffset)
    {
        _registers.SetRegister(0, register0);
        _registers.SetRegister(1, register1);
        _registers.SetRegister(2, register2);
        _registers.GetVideoMode().Should().Be(mode);
        _registers.GetNameTableBaseAddressOffset().Should().Be(nameTableBaseAddressOffset);
    }
}