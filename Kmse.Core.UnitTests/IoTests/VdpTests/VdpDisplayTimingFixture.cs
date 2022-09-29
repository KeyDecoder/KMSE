using FluentAssertions;
using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Vdp;
using Kmse.Core.IO.Vdp.Control;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.IO.Vdp.Rendering;
using Kmse.Core.Z80.Interrupts;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.IoTests.VdpTests;

[TestFixture]
public class VdpDisplayTimingFixture
{
    [SetUp]
    public void Setup()
    {
        _interruptManagement = Substitute.For<IZ80InterruptManagement>();
        _vdpRegisters = Substitute.For<IVdpRegisters>();
        _vdpRegisters.GetVideoMode().Returns(VdpVideoMode.Mode4);
        _ram = new VdpRam();
        _verticalCounter = new VdpVerticalCounter(_vdpRegisters);
        _horizontalCounter = new VdpHorizontalCounter();
        _controlPortManager = new VdpControlPortManager(_ram, _vdpRegisters);
        _flags = new VdpFlags();
        _displayUpdater = new TestDisplayUpdater();
        _renderer = new VdpMode4DisplayModeRenderer(_vdpRegisters, _ram, _displayUpdater, _verticalCounter, _flags);
        _vdpPort = new VdpPort(_vdpRegisters, _interruptManagement, _ram, _verticalCounter, _horizontalCounter, _renderer, Substitute.For<IIoPortLogger>(), _flags, _controlPortManager);
        _vdpPort.Reset();
    }

    private IZ80InterruptManagement _interruptManagement;
    private IVdpRegisters _vdpRegisters;
    private IVdpRam _ram;
    private IVdpVerticalCounter _verticalCounter;
    private IVdpHorizontalCounter _horizontalCounter;
    private IVdpControlPortManager _controlPortManager;
    private IVdpFlags _flags;
    private IVdpDisplayModeRenderer _renderer;
    private TestDisplayUpdater _displayUpdater;
    private VdpPort _vdpPort;

    [Test]
    public void WhenExecutingHCounterIsUpdated()
    {
        _vdpPort.Execute(1);
        _vdpPort.GetStatus().HCounter.Should().Be(1);
        _interruptManagement.DidNotReceive().SetMaskableInterrupt();
    }

    [Test]
    public void WhenExecutingAFullLineVCounterIsUpdated()
    {
        for (var i = 0; i < 228; i++)
        {
            _vdpPort.Execute(1);
        }

        _vdpPort.GetStatus().HCounter.Should().Be(0);
        _vdpPort.GetStatus().VCounter.Should().Be(1);
        _interruptManagement.DidNotReceive().SetMaskableInterrupt();
    }

    [Test]
    public void WhenExecutingAFullFrameThenCountersAreReset()
    {
        // Pal has 313 lines and 342 pixels per line
        _vdpPort.SetDisplayType(VdpDisplayType.Pal);
        for (var y = 0; y < 312; y++)
        {
            for (var i = 0; i < 228; i++)
            {
                _vdpPort.Execute(1);
            }
        }

        _vdpPort.GetStatus().HCounter.Should().Be(0);
        _vdpPort.GetStatus().VCounter.Should().Be(0);
    }

    [Test]
    public void WhenExecutingAFullActiveFrameAFrameInterruptIsGenerated()
    {
        // Enable frame interrupt
        _vdpRegisters.IsFrameInterruptEnabled().Returns(true);

        // 192 active lines and 342 pixels per line
        _vdpPort.SetDisplayType(VdpDisplayType.Pal);
        for (var y = 0; y < 192; y++)
        {
            for (var i = 0; i < 228; i++)
            {
                _vdpPort.Execute(1);
            }
        }

        _interruptManagement.Received(1).SetMaskableInterrupt();
    }

    private class TestDisplayUpdater : IVdpDisplayUpdater
    {
        public bool GotRenderCall { get; private set; }
        public void UpdateDisplay(Span<byte> frame)
        {
            GotRenderCall = true;
        }
    }
}