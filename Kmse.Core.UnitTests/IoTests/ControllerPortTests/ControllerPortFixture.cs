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
        [TestCase(0x00, 0x3F)]
        [TestCase(1 << 7, 0xBF)]
        [TestCase(1 << 5, 0x7F)]
        [TestCase(0xFF, 0xFF)]
        public void WhenSettingIoPortControl(byte ioPortControl, byte expectedPortValue)
        {
            // Bits 7 and 5 of port control are copied into I/O port 0xDD bits 7 and 6 respectively
            // Note that bit 5 of DD is always set and bit 4 is set since reset button not pressed
            _controllerPort.SetIoPortControl(ioPortControl);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedPortValue);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0xFF);
        }

        [Test]
        public void WhenTriggeringResetButton()
        {
            _controllerPort.ChangeResetButtonState(true);

            // When controller pressed bit 4 is not set, so should be zero (and remaining bits always set since nothing else pressed)
            _controllerPort.ReadPort(0xDD).Should().Be(0xEF);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0xFF);
        }

        [Test]
        public void WhenReleasingResetButton()
        {
            _controllerPort.ChangeResetButtonState(true);
            _controllerPort.ReadPort(0xDD).Should().Be(0xEF);

            _controllerPort.ChangeResetButtonState(false);

            // When controller not pressed bit 4 is set along with all other bits since nothing pressed
            _controllerPort.ReadPort(0xDD).Should().Be(0xFF);

            // I/O port A unchanged
            _controllerPort.ReadPort(0xDC).Should().Be(0xFF);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0xDF)]
        [TestCase(ControllerInputStatus.LeftButton, 0xEF)]
        [TestCase(ControllerInputStatus.Right, 0xF7)]
        [TestCase(ControllerInputStatus.Left, 0xFB)]
        [TestCase(ControllerInputStatus.Down, 0xFD)]
        [TestCase(ControllerInputStatus.Up, 0xFE)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0xF0)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0xC9)]
        public void TriggeringInputAToBePressed(ControllerInputStatus status, byte expectedOutput)
        {
            _controllerPort.ChangeInputAControlState(status, true);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedOutput);

            // I/O port B unchanged
            _controllerPort.ReadPort(0xDD).Should().Be(0xFF);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0xE0)]
        [TestCase(ControllerInputStatus.LeftButton, 0xD0)]
        [TestCase(ControllerInputStatus.Right, 0xC8)]
        [TestCase(ControllerInputStatus.Left, 0xC4)]
        [TestCase(ControllerInputStatus.Down, 0xC2)]
        [TestCase(ControllerInputStatus.Up, 0xC1)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0xCF)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0xF6)]
        public void TriggeringInputAToBeUnpressed(ControllerInputStatus status, byte expectedOutput)
        {
            // Press all the buttons and then unpress the buttons from the parameters
            _controllerPort.ChangeInputAControlState((ControllerInputStatus)0x3F, true);
            _controllerPort.ReadPort(0xDC).Should().Be(0xC0);
            _controllerPort.ChangeInputAControlState(status, false);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedOutput);

            // I/O port B unchanged
            _controllerPort.ReadPort(0xDD).Should().Be(0xFF);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0xFF, 0xF7)]
        [TestCase(ControllerInputStatus.LeftButton, 0xFF, 0xFB)]
        [TestCase(ControllerInputStatus.Right, 0xFF, 0xFD)]
        [TestCase(ControllerInputStatus.Left, 0xFF, 0xFE)]
        [TestCase(ControllerInputStatus.Down, 0x7F, 0xFF)]
        [TestCase(ControllerInputStatus.Up, 0xBF, 0xFF)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0x3F, 0xFC)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0x7F, 0xF2)]
        public void TriggeringInputB(ControllerInputStatus status, byte expectedDcOutput, byte expectedDdOutput)
        {
            _controllerPort.SetIoPortControl(0xF0);
            _controllerPort.ChangeInputBControlState(status, true);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedDcOutput);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedDdOutput);
        }

        [Test]
        [TestCase(ControllerInputStatus.RightButton, 0x3F, 0xF8)]
        [TestCase(ControllerInputStatus.LeftButton, 0x3F, 0xF4)]
        [TestCase(ControllerInputStatus.Right, 0x3F, 0xF2)]
        [TestCase(ControllerInputStatus.Left, 0x3F, 0xF1)]
        [TestCase(ControllerInputStatus.Down, 0xBF, 0xF0)]
        [TestCase(ControllerInputStatus.Up, 0x7F, 0xF0)]
        [TestCase(ControllerInputStatus.Up | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.Right, 0xFF, 0xF3)]
        [TestCase(ControllerInputStatus.LeftButton | ControllerInputStatus.Down | ControllerInputStatus.Left | ControllerInputStatus.RightButton, 0xBF, 0xFD)]
        public void TriggeringInputBToBeUnpressed(ControllerInputStatus status, byte expectedDcOutput, byte expectedDdOutput)
        {
            _controllerPort.SetIoPortControl(0xF0);
            _controllerPort.ChangeInputBControlState((ControllerInputStatus)0x3F, true);
            _controllerPort.ChangeInputBControlState(status, false);
            _controllerPort.ReadPort(0xDC).Should().Be(expectedDcOutput);
            _controllerPort.ReadPort(0xDD).Should().Be(expectedDdOutput);
        }
    }
}
