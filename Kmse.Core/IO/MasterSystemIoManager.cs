using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;
using Kmse.Core.Z80.Interrupts;

namespace Kmse.Core.IO;

/// <summary>
///     Manage I/O ports and route requests to appropriate class to handle
/// </summary>
/// <remarks>
///     The Z80 has a 16 - bit address bus that can be used to access 64K 8 - bit
///     ports.In the SMS, only the lower 8 bits of the address bus are used and
///     the upper 8 bits are ignored.
///     In the Master System and Game Gear, the VDP is commonly accessed at the
///     following Z80 I / O ports:
///     $7E = V counter(read) / SN76489 data(write)
///     $7F = H counter(read) / SN76489 data(write, mirror)
///     $BE = Data port(r / w)
///     $BF = Control port(r / w)
///     The address decoding for the I/ O ports is done with A7, A6, and A0 of
///     the Z80 address bus, so the VDP locations are mirrored:
///     $40 - 7F = Even locations are V counter / PSG, odd locations are H counter / PSG
///     $80 - BF = Even locations are data port, odd locations are control port.
/// </remarks>
public class MasterSystemIoManager : IMasterSystemIoManager
{
    private IControllerPort _controllerPort;
    private IDebugConsolePort _debugConsolePort;
    private readonly IIoPortLogger _logger;
    private ISoundPort _soundPort;
    private IVdpPort _vdpPort;

    private byte _memoryControlRegister;
    private byte _ioControlRegister;

    public MasterSystemIoManager(IIoPortLogger logger, IVdpPort vdpPort, IControllerPort controllerPort, ISoundPort soundPort,
        IDebugConsolePort debugConsolePort)
    {
        _logger = logger;
        _vdpPort = vdpPort;
        _controllerPort = controllerPort;
        _soundPort = soundPort;
        _debugConsolePort = debugConsolePort;
    }

    public void Reset()
    {
        _memoryControlRegister = 0x00;
        _ioControlRegister = 0x00;
    }

    /* SMS MkII Port Mapping
    $00-$3F : Writes to even addresses go to memory control register.
        Writes to odd addresses go to I/O control register.
        Reads return $FF.
    $40-$7F : Writes to any address go to the SN76489 PSG.
        Reads from even addresses return the V counter.
        Reads from odd address return the H counter.
    $80-$BF : Writes to even addresses go to the VDP data port.
        Writes to odd addresses go to the VDP control port.
        Reads from even addresses return the VDP data port contents.
        Reads from odd address return the VDP status flags.
    $C0-$FF : Writes have no effect.
        Reads from even addresses return the I/O port A/B register.
        Reads from odd address return the I/O port B/misc.register.
    */

    public byte ReadPort(ushort port)
    {
        // Ignore the upper 8 bits since that is not used
        var address = (ushort)(port & 0xFF);
        byte dataRead;

        switch (address)
        {
            case < 0x40:
                // $00 -$3F : Writes to even addresses go to memory control register.
                // Writes to odd addresses go to I / O control register.
                // Reads return $FF.
                dataRead = 0xFF;
                break;
            case <= 0xBF:
                // $40 -$7F : Writes to any address go to the SN76489 PSG.
                // Reads from even addresses return the V counter.
                // Reads from odd address return the H counter.

                // $80 -$BF: Writes to even addresses go to the VDP data port.
                // Writes to odd addresses go to the VDP control port.
                // Reads from even addresses return the VDP data port contents.
                // Reads from odd address return the VDP status flags.
                dataRead = _vdpPort.ReadPort((byte)(address & 0xFF));
                break;
            case <= 0xFF:
                // $C0 -$FF: Writes have no effect.
                // Reads from even addresses return the I / O port A/ B register.
                // Reads from odd address return the I / O port B/ misc.register.
                dataRead = _controllerPort.ReadPort(address);
                break;
            default:
                _logger.Error($"{port:X4}", $"Reading of port {address:X4} is not supported");
                throw new InvalidOperationException($"Unsupported read of port at address {address:X4}");
        }

        _logger.PortRead(port, dataRead);
        return dataRead;
    }

    public void WritePort(ushort port, byte value)
    {
        // Ignore the upper 8 bits since that is not used
        var address = (ushort)(port & 0xFF);
        _logger.PortWrite(port, value);

        if (address < 0x40)
        {
            //$00 -$3F : Writes to even addresses go to memory control register.
            //    Writes to odd addresses go to I / O control register.
            //    Reads return $FF.
            if (address % 2 == 0)
            {
                _memoryControlRegister = value;
            }
            else
            {
                _ioControlRegister = value;
                _controllerPort.SetIoPortControl(value);
            }

            return;
        }

        if (address < 0x80)
        {
            // $40 -$7F : Writes to any address go to the SN76489 PSG.
            _soundPort.WritePort((byte)(address & 0xFF), value);
            return;
        }

        if (address <= 0xBF)
        {
            _vdpPort.WritePort((byte)(address & 0xFF), value);
            return;
        }

        if (address is 0xFC or 0xFD)
        {
            _debugConsolePort.WritePort(address, value);
            return;
        }

        // Unknown port mapping, just log it and move on
        _logger.Error($"{port:X4}", "Unhandled write to I/O");
        throw new InvalidOperationException($"Unsupported write to port at address {address:X4}");  
    }
}