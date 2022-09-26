using FluentAssertions;
using Kmse.Core.IO.Vdp;
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
        _vdpPort = new VdpPort(_vdpRegisters, _interruptManagement);
        _vdpPort.Reset();
    }

    private IZ80InterruptManagement _interruptManagement;
    private IVdpRegisters _vdpRegisters;
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
        for (var i = 0; i < 342; i++)
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
            for (var i = 0; i < 342; i++)
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
            for (var i = 0; i < 342; i++)
            {
                _vdpPort.Execute(1);
            }
        }

        _interruptManagement.Received(1).SetMaskableInterrupt();
    }
}