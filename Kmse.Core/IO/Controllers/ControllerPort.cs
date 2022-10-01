using Kmse.Core.Utilities;

namespace Kmse.Core.IO.Controllers;

/// <summary>
/// Implement controller inputs and support for reset button
/// </summary>
/// <remarks>
/// For anyone wondering where the pause button support is, the Z80's NMI pin is connected to the PAUSE button.
/// So we handle this inside the <see cref="IMasterSystemConsole"/> implementation and it will trigger the NMI directly via <see cref="IMasterSystemIoManager"/>
/// </remarks>
public class ControllerPort : IControllerPort
{
    private byte _ioPortControl;

    private byte _ioPortAb;
    private byte _ioPortBMisc;

    private readonly object _lock = new();

    public void Reset()
    {
        // In the case of a controller, a pressed button returns 0, otherwise 1.
        // Default to no buttons pressed

        _ioPortAb = 0xFF;
        // Reset and bit 5 always set as well
        _ioPortBMisc = 0xFF;
        _ioPortControl = 0x00;
    }

    public void SetIoPortControl(byte value)
    {
        _ioPortControl = value;

        // Since we don't have a light gun we just return the current state of the TH output/input setting for region detection
        // https://www.smspower.org/Development/RegionDetection
        // Set top bits 7 and 6 depending on current IO port control state
        Bitwise.SetOrClearIf(ref _ioPortBMisc, 7, () => Bitwise.IsSet(_ioPortControl, 7));
        Bitwise.SetOrClearIf(ref _ioPortBMisc, 6, () => Bitwise.IsSet(_ioPortControl, 5));
    }

    public byte ReadPort(ushort port)
    {
        /*
        $C0 -$FF: Writes have no effect.
            Reads from even addresses return the I / O port A/ B register.
            Reads from odd address return the I / O port B/ misc.register.
        */

        // When bit 2 is set, all ports at $C0 through $FF return $FF on a SMS 2, Game Gear, and Genesis
        if (Bitwise.IsSet(_ioPortControl, 2))
        {
            return 0xFF;
        }

        return port % 2 == 0 ? _ioPortAb : _ioPortBMisc;
    }

    public void ChangeResetButtonState(bool pressed)
    {
        // I/O Port $DD
        //D4 : RESET button(1 = not pressed, 0 = pressed)
        Bitwise.SetOrClearIf(ref _ioPortBMisc, 4, () => !pressed);
    }

    public void ChangeInputAControlState(ControllerInputStatus status, bool pressed)
    {
        // Port $DC: I / O port A and B
        // D5 : Port A TR pin input
        // D4 : Port A TL pin input
        // D3 : Port A RIGHT pin input
        // D2 : Port A LEFT pin input
        // D1 : Port A DOWN pin input
        // D0 : Port A UP pin input

        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.RightButton, 5, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.LeftButton, 4, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Right, 3, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Left, 2, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Down, 1, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Up, 0, pressed);
    }

    public void ChangeInputBControlState(ControllerInputStatus status, bool pressed)
    {
        // Port $DC: I / O port A and B
        // D7: Port B DOWN pin input
        // D6 : Port B UP pin input

        // I/O Port $DD
        // D3: Port B TR pin input
        // D2 : Port B TL pin input
        // D1 : Port B RIGHT pin input
        // D0 : Port B LEFT pin input

        SetIoPortIfFlag(ref _ioPortBMisc, status, ControllerInputStatus.RightButton, 3, pressed);
        SetIoPortIfFlag(ref _ioPortBMisc, status, ControllerInputStatus.LeftButton, 2, pressed);
        SetIoPortIfFlag(ref _ioPortBMisc, status, ControllerInputStatus.Right, 1, pressed);
        SetIoPortIfFlag(ref _ioPortBMisc, status, ControllerInputStatus.Left, 0, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Down, 7, pressed);
        SetIoPortIfFlag(ref _ioPortAb, status, ControllerInputStatus.Up, 6, pressed);
    }

    private static void SetIoPortIfFlag(ref byte ioPortValue, ControllerInputStatus status, ControllerInputStatus flag, int bit, bool pressed)
    {
        if (status.HasFlag(flag))
        {
            Bitwise.SetOrClearIf(ref ioPortValue, bit, () => !pressed);
        }
    }
}