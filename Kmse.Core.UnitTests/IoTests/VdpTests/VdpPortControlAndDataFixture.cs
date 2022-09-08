using FluentAssertions;
using Kmse.Core.IO.Vdp;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.IoTests.VdpTests;

[TestFixture]
public class VdpPortControlAndDataFixture
{
    [SetUp]
    public void Setup()
    {
        _vdpPort = new VdpPort();
        _vdpPort.Reset();
    }

    private VdpPort _vdpPort;

    [Test]
    [TestCase((byte)0x00, (byte)0x00, (ushort)0x0000, (byte)0x00, (ushort)0x0001)]
    [TestCase((byte)0xFF, (byte)0xFF, (ushort)0xFFFF, (byte)0x03, (ushort)0x3FFF)]
    [TestCase((byte)0xAA, (byte)0xDD, (ushort)0xDDAA, (byte)0x03, (ushort)0x1DAA)]
    [TestCase((byte)0x00, (byte)0xC0, (ushort)0xC000, (byte)0x03, (ushort)0x0000)]
    [TestCase((byte)0x00, (byte)0x80, (ushort)0x8000, (byte)0x02, (ushort)0x0000)]
    [TestCase((byte)0x00, (byte)0x40, (ushort)0x4000, (byte)0x01, (ushort)0x0000)]
    [TestCase((byte)0xFF, (byte)0x7F, (ushort)0x7FFF, (byte)0x01, (ushort)0x3FFF)]
    public void WhenWritingVdpCommandWord(byte data1, byte data2, ushort commandWord, byte codeRegister,
        ushort addressRegister)
    {
        // Command word is written in two bytes
        _vdpPort.WritePort(0xBF, data1);
        _vdpPort.WritePort(0xBF, data2);
        var status = _vdpPort.GetStatus();
        status.CommandWord.Should().Be(commandWord);
        status.CodeRegister.Should().Be(codeRegister);
        status.AddressRegister.Should().Be(addressRegister);
    }

    [Test]
    public void WhenWritingMultipleVpdCommandWords()
    {
        _vdpPort.WritePort(0xBF, 0xAA);
        _vdpPort.WritePort(0xBF, 0xDD);
        _vdpPort.GetStatus().CommandWord.Should().Be(0xDDAA);

        _vdpPort.WritePort(0xBF, 0x01);
        _vdpPort.WritePort(0xBF, 0x42);

        var status = _vdpPort.GetStatus();
        status.CommandWord.Should().Be(0x4201);
        status.CodeRegister.Should().Be(0x01);
        status.AddressRegister.Should().Be(0x0201);
    }

    [Test]
    public void WhenCodeRegisterHasSetWriteModeAction()
    {
        // Default is video ram, so writing and checking just that won't do much
        // So we set to color RAM mode first and then set to video RAM mode second
        // This validates it is actually changing the mode
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);

        _vdpPort.WritePort(0xBF, 0x00);
        _vdpPort.WritePort(0xBF, 0xC0);
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.ColourRam);

        _vdpPort.WritePort(0xBF, 0x00);
        _vdpPort.WritePort(0xBF, 0x40);
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);
    }

    [Test]
    public void WhenWritingToVideoRam()
    {
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0);

        _vdpPort.WritePort(0xBE, 0x01);
        _vdpPort.WritePort(0xBE, 0x02);
        _vdpPort.WritePort(0xBE, 0x03);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x03);
        var videoRam = _vdpPort.DumpVideoRam();
        videoRam[0].Should().Be(0x01);
        videoRam[1].Should().Be(0x02);
        videoRam[2].Should().Be(0x03);
    }

    [Test]
    public void WhenWritingToVideoRamPassLargestAddressThenWraps()
    {
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0);

        _vdpPort.WritePort(0xBF, 0xFE);
        _vdpPort.WritePort(0xBF, 0x7F);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x3FFE);

        _vdpPort.WritePort(0xBE, 0x01);
        _vdpPort.WritePort(0xBE, 0x02);
        _vdpPort.WritePort(0xBE, 0x03);
        // Wraps from 3FFF to 0
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x01);
        var videoRam = _vdpPort.DumpVideoRam();
        videoRam[0x3FFE].Should().Be(0x01);
        videoRam[0x3FFF].Should().Be(0x02);
        videoRam[0].Should().Be(0x03);
    }

    [Test]
    public void WhenWritingToColourRam()
    {
        _vdpPort.WritePort(0xBF, 0x00);
        _vdpPort.WritePort(0xBF, 0xC0);
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.ColourRam);

        _vdpPort.WritePort(0xBE, 0x01);
        _vdpPort.WritePort(0xBE, 0x02);
        _vdpPort.WritePort(0xBE, 0x03);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x03);
        var colourRam = _vdpPort.DumpColourRam();
        colourRam[0].Should().Be(0x01);
        colourRam[1].Should().Be(0x02);
        colourRam[2].Should().Be(0x03);
    }

    [Test]
    public void WhenWritingToColourRamPassLargestAddressThenWraps()
    {
        _vdpPort.WritePort(0xBF, 0x1F);
        _vdpPort.WritePort(0xBF, 0xC0);
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.ColourRam);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x001F);

        _vdpPort.WritePort(0xBE, 0x01);
        _vdpPort.WritePort(0xBE, 0x02);
        _vdpPort.WritePort(0xBE, 0x03);

        // Note that address itself does not wrap but data storage address is wrapped
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x0022);
        var colourRam = _vdpPort.DumpColourRam();
        colourRam[0x1F].Should().Be(0x01);
        colourRam[0x00].Should().Be(0x02);
        colourRam[0x01].Should().Be(0x03);
    }

    [Test]
    public void WhenReadingAndWritingToVideoRam()
    {
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0);

        _vdpPort.WritePort(0xBE, 0x01);
        _vdpPort.WritePort(0xBE, 0x02);
        _vdpPort.WritePort(0xBE, 0x03);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x03);
        var videoRam = _vdpPort.DumpVideoRam();
        videoRam[0].Should().Be(0x01);
        videoRam[1].Should().Be(0x02);
        videoRam[2].Should().Be(0x03);

        // Set the address since any read increments the address
        _vdpPort.WritePort(0xBF, 0x01);
        _vdpPort.WritePort(0xBF, 0x00);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x02);

        // Set address using code register mode 0 so it reads the next byte out instead of the buffered from last write
        var data = _vdpPort.ReadPort(0xBE);
        data.Should().Be(0x02);
        _vdpPort.GetStatus().AddressRegister.Should().Be(0x03);

        // Buffer holds next value in video RAM
        _vdpPort.GetStatus().ReadBuffer.Should().Be(0x03);
    }

    [Test]
    public void WhenWritingVdpRegisters()
    {
        // Start in write colour ram mode to validate this sets the write mode back to video RAM
        _vdpPort.WritePort(0xBF, 0x1F);
        _vdpPort.WritePort(0xBF, 0xC0);
        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.ColourRam);

        // Write vdp register, code register mode 2
        _vdpPort.WritePort(0xBF, 0x3C);
        // Write to Register 2
        _vdpPort.WritePort(0xBF, 0x82);

        _vdpPort.GetStatus().WriteMode.Should().Be(DataPortWriteMode.VideoRam);
        _vdpPort.GetStatus().VdpRegisters[2].Should().Be(0x3C);
    }
}