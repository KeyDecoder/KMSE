using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;

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
    private readonly IControllerPort _controllerPort;
    private readonly IDebugConsolePort _debugConsolePort;
    private readonly IIoPortLogger _logger;
    private readonly ISoundPort _soundPort;
    private readonly IVdpPort _vdpPort;

    public MasterSystemIoManager(IIoPortLogger logger, IVdpPort vdpPort, IControllerPort controllerPort,
        ISoundPort soundPort, IDebugConsolePort debugConsolePort)
    {
        _logger = logger;
        _vdpPort = vdpPort;
        _controllerPort = controllerPort;
        _soundPort = soundPort;
        _debugConsolePort = debugConsolePort;
    }

    public bool NonMaskableInterrupt { get; private set; }
    public bool MaskableInterrupt { get; private set; }

    public void SetMaskableInterrupt()
    {
        MaskableInterrupt = true;
        _logger.SetMaskableInterruptStatus(MaskableInterrupt);
    }

    public void ClearMaskableInterrupt()
    {
        MaskableInterrupt = false;
        _logger.SetMaskableInterruptStatus(MaskableInterrupt);
    }

    public void SetNonMaskableInterrupt()
    {
        NonMaskableInterrupt = true;
        _logger.SetNonMaskableInterruptStatus(NonMaskableInterrupt);
    }

    public void ClearNonMaskableInterrupt()
    {
        NonMaskableInterrupt = false;
        _logger.SetNonMaskableInterruptStatus(NonMaskableInterrupt);
    }

    public byte ReadPort(ushort port)
    {
        // Ignore the upper 8 bits since that is not used
        var address = (ushort)(port & 0xFF);

        if (address < 0x40)
        {
            _logger.ReadPort(port, 0xFF);
            return 0xFF;
        }

        if (address <= 0xBF)
        {
            var data = _vdpPort.ReadPort(address);
            _logger.ReadPort(port, data);
            return data;
        }

        switch (address)
        {
            // Controller port 1
            case 0xDC:
            // mirror of 0xDC
            case 0xC0:
            // Controller port 2
            case 0xDD:
            // mirror of 0xDD
            case 0xC1:
            {
                var data = _controllerPort.ReadPort(address);
                _logger.ReadPort(port, data);
                return data;
            }
            default:
            {
                _logger.ReadPort(port, 0);
                return 0;
            }
        }
    }

    public void WritePort(ushort port, byte value)
    {
        // Ignore the upper 8 bits since that is not used
        var address = (ushort)(port & 0xFF);

        if (address < 0x40) return;

        if (address < 0x80)
        {
            // Writing to sound chip
            _soundPort.WritePort(address, value);
            _logger.WritePort(port, value);
            return;
        }

        if (address <= 0xBF)
        {
            _vdpPort.WritePort(address, value);
            _logger.WritePort(port, value);
            return;
        }

        if (address is 0xFC or 0xFD)
        {
            _debugConsolePort.WritePort(address, value);
            _logger.WritePort(port, value);
            return;
        }

        // Unknown port mapping, just log it
        _logger.WritePort(port, value);
    }
}