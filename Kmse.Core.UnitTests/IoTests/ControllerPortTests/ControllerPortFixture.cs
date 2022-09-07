using FluentAssertions;
using Kmse.Core.IO.Controllers;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.IoTests.ControllerPortTests
{

    [TestFixture]
    public class ControllerPortFixture
    {
        [SetUp]
        public void Setup()
        {
            _controllerPort = new ControllerPort();
            _controllerPort.Reset();
        }

        private ControllerPort _controllerPort;

        [Test]
        [TestCase(0x00, 0x30)]
        [TestCase(1 << 7, 0xB0)]
        [TestCase(1 << 5, 0x70)]
        [TestCase(0xFF, 0xF0)]
        public void WhenSettingIoPortControl(byte ioPortControl, byte expectedPortValue)
        {
            // Bits 7 and 5 of port control are copied into I/O port 0xDD bits 7 and 6 respectively
            // Note that bit 5 of DD is always set and bit 4 is set since reset button not pressed
            _controllerPort.SetIoPortControl(ioPortControl);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedPortValue);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0x00);
        }

        [Test]
        public void WhenTriggeringResetButton()
        {
            _controllerPort.ChangeResetButtonState(true);

            // When controller pressed bit 4 is not set, so should be zero (and bit 5 always set)
            _controllerPort.ReadPort(0xDD).Should().Be(0x20);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0);
        }

        [Test]
        public void WhenReleasingResetButton()
        {
            _controllerPort.ChangeResetButtonState(true);
            _controllerPort.ReadPort(0xDD).Should().Be(0x20);

            _controllerPort.ChangeResetButtonState(false);

            // When controller not pressed bit 4 is set (and bit 5 always set)
            _controllerPort.ReadPort(0xDD).Should().Be(0x30);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 1 << 5)]
        [TestCase(ControllerInputStatus.LeftButton, 1 << 4)]
        [TestCase(ControllerInputStatus.Right, 1 << 3)]
        [TestCase(ControllerInputStatus.Left, 1 << 2)]
        [TestCase(ControllerInputStatus.Down, 1 << 1)]
        [TestCase(ControllerInputStatus.Up, 1 << 0)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0x0F)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0x36)]
        public void TriggeringInputAToBePressed(ControllerInputStatus status, byte expectedOutput)
        {
            _controllerPort.ChangeInputAControlState(status, true);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedOutput);

            // I/O port B unchanged
            _controllerPort.ReadPort(0xDD).Should().Be(0x30);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0x1F)]
        [TestCase(ControllerInputStatus.LeftButton, 0x2F)]
        [TestCase(ControllerInputStatus.Right, 0x37)]
        [TestCase(ControllerInputStatus.Left, 0x3B)]
        [TestCase(ControllerInputStatus.Down, 0x3D)]
        [TestCase(ControllerInputStatus.Up, 0x3E)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0x30)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0x09)]
        public void TriggeringInputAToBeUnpressed(ControllerInputStatus status, byte expectedOutput)
        {
            // Press all the buttons and then unpress the buttons from the parameters
            _controllerPort.ChangeInputAControlState((ControllerInputStatus)0x3F, true);
            _controllerPort.ReadPort(0xDC).Should().Be(0x3F);
            _controllerPort.ChangeInputAControlState(status, false);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedOutput);

            // I/O port B unchanged
            _controllerPort.ReadPort(0xDD).Should().Be(0x30);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0x00, 0x38)]
        [TestCase(ControllerInputStatus.LeftButton, 0x00, 0x34)]
        [TestCase(ControllerInputStatus.Right, 0x00, 0x32)]
        [TestCase(ControllerInputStatus.Left, 0x00, 0x31)]
        [TestCase(ControllerInputStatus.Down, 0x80, 0x30)]
        [TestCase(ControllerInputStatus.Up, 0x40, 0x30)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0xC0, 0x33)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0x80, 0x3D)]
        public void TriggeringInputB(ControllerInputStatus status, byte expectedDcOutput, byte expectedDdOutput)
        {
            _controllerPort.SetIoPortControl(0x00);
            _controllerPort.ChangeInputBControlState(status, true);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedDcOutput);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedDdOutput);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0xC0, 0x37)]
        [TestCase(ControllerInputStatus.LeftButton, 0xC0, 0x3B)]
        [TestCase(ControllerInputStatus.Right, 0xC0, 0x3D)]
        [TestCase(ControllerInputStatus.Left, 0xC0, 0x3E)]
        [TestCase(ControllerInputStatus.Down, 0x40, 0x3F)]
        [TestCase(ControllerInputStatus.Up, 0x80, 0x3F)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0x00, 0x3C)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0x40, 0x32)]
        public void TriggeringInputBToBeUnpressed(ControllerInputStatus status, byte expectedDcOutput, byte expectedDdOutput)
        {
            _controllerPort.SetIoPortControl(0x00);
            _controllerPort.ChangeInputBControlState((ControllerInputStatus)0x3F, true);
            _controllerPort.ChangeInputBControlState(status, false);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedDcOutput);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedDdOutput);
        }
    }
}
