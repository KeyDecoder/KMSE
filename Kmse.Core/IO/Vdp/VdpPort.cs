using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Vdp.Control;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.IO.Vdp.Rendering;
using Kmse.Core.IO.Vdp.Rendering.DebugRendering;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Interrupts;

namespace Kmse.Core.IO.Vdp;

/// <summary>
///     Emulate the video display processor
///     This emulates the 315-5246 which is used in the SMS 2 and later versions of the SMS.
/// </summary>
/// <remarks>
///     Documentation - https://www.smspower.org/uploads/Development/msvdp-20021112.txt
/// </remarks>
public class VdpPort : IVdpPort
{
    private readonly IVdpHorizontalCounter _horizontalCounter;
    private readonly IIoPortLogger _ioPortLogger;
    private readonly IVdpFlags _flags;
    private readonly IVdpControlPortManager _controlPortManager;
    private readonly IVdpDebugRenderer _debugRenderer;
    private readonly IVdpDisplayModeRenderer _renderer;
    private readonly IZ80InterruptManagement _interruptManagement;
    private readonly IVdpRam _ram;

    private readonly IVdpRegisters _registers;
    private readonly IVdpVerticalCounter _verticalCounter;

    public VdpPort(IVdpRegisters registers, IZ80InterruptManagement interruptManagement, IVdpRam ram,
        IVdpVerticalCounter verticalCounter, IVdpHorizontalCounter horizontalCounter, IVdpDisplayModeRenderer renderer, IIoPortLogger ioPortLogger,
        IVdpFlags flags, IVdpControlPortManager controlPortManager, IVdpDebugRenderer debugRenderer)
    {
        _interruptManagement = interruptManagement;
        _ram = ram;
        _verticalCounter = verticalCounter;
        _horizontalCounter = horizontalCounter;
        _ioPortLogger = ioPortLogger;
        _flags = flags;
        _controlPortManager = controlPortManager;
        _debugRenderer = debugRenderer;
        _renderer = renderer;
        _registers = registers;
    }

    public void Reset()
    {
        _controlPortManager.Reset();
        _registers.Reset();
        _ram.Reset();
        _verticalCounter.Reset();
        _horizontalCounter.Reset();
        _flags.Reset();
        _renderer.Reset();
    }

    public byte ReadPort(byte port)
    {
        return port switch
        {
            < 0x40 => throw new InvalidOperationException(
                $"Invalid operation reading from port at address {port} which is not a valid VDP port"),
            < 0x80 => HandleHvCounterReads(port),
            < 0xC0 => HandleVdpRead(port),
            _ => throw new InvalidOperationException(
                $"Invalid operation reading from port at address {port} which is not a valid VDP port")
        };
    }

    public void WritePort(byte port, byte value)
    {
        // $80-$BF : 
        // Writes to even addresses go to the VDP data port.
        // Writes to odd addresses go to the VDP control port.

        if (port < 0x80)
        {
            throw new InvalidOperationException(
                $"Invalid operation writing to port at address {port} which is not a valid VDP port");
        }

        if (port % 2 == 0)
        {
            // Command word writing is reset when data port is written
            _controlPortManager.ResetControlByte();
            _ram.WriteData(value);
        }
        else
        {
            _controlPortManager.WriteToVdpControlPort(value);
        }
    }

    public void SetIoPortControl(byte value)
    {
        /*
          Port $3F : I/O port control
          D3 : Port B TH pin direction (1=input, 0=output)
          D1 : Port A TH pin direction (1=input, 0=output)
         */

        if (Bitwise.IsSet(value, 1) || Bitwise.IsSet(value, 3))
        {
            // If TH of either port is set, then update the latched H counter 
            _horizontalCounter.UpdateLatchedCounter();
        }
    }

    public void SetDisplayType(VdpDisplayType displayType)
    {
        _verticalCounter.SetDisplayType(displayType);
    }

    public VdpPortStatus GetStatus()
    {
        return new VdpPortStatus
        {
            HCounter = _horizontalCounter.Counter,
            VCounter = _verticalCounter.Counter,
            CommandWord = _controlPortManager.CommandWord,
            StatusFlags = _flags.Flags,
            CodeRegister = _controlPortManager.CodeRegister,
            AddressRegister = _ram.AddressRegister,
            VdpRegisters = _registers.DumpRegisters(),
            ReadBuffer = _ram.GetReadBufferValue(),
            WriteMode = _ram.WriteMode
        };
    }

    public byte[] DumpVideoRam()
    {
        return _ram.DumpVideoRam();
    }

    public byte[] DumpColourRam()
    {
        return _ram.DumpColourRam();
    }

    /// <summary>
    ///     Execute update of VDP based on the number of cycles that have been executed by the CPU
    /// </summary>
    /// <param name="cycles">Number of cycles executed since last update</param>
    public void Execute(int cycles)
    {
        // TODO: This needs more work to keep the CPU execution and VDP execution in sync to ensure 
        // always renders a single frame as expected
        // Until the more VDP emulation is done, this is sufficient for now
        _horizontalCounter.Increment(cycles);

        if (_horizontalCounter.EndOfScanline())
        {
            _horizontalCounter.ResetLine();

            // At the end of each line, if we are inside the active frame, render the entire line
            // This is easier since we can render all the pixels since the H counter is not incremented with a consistent value due to cycle count changes
            if (_verticalCounter.IsInsideActiveFrame())
            {
                // TODO: Change renderer based on video mode
                _renderer.RenderLine();
            }

            // Since we use the counter in rendering, we don't increment the V Counter until after we have rendered the current line
            _verticalCounter.Increment();

            if (_verticalCounter.EndOfActiveFrame())
            {
                _debugRenderer.RenderAllTilesAndSpritesInMemory();
                _debugRenderer.RenderAllSpritesInAddressTable();

                _renderer.UpdateDisplay();
                _flags.SetFlag(VdpStatusFlags.FrameInterruptPending);
            }

            if (_verticalCounter.EndOfFrame())
            {
                // Clear and reset
                _renderer.ResetBuffer();
                _verticalCounter.ResetFrame();
            }

            _verticalCounter.UpdateLineCounter();
        }

        if (_registers.IsFrameInterruptEnabled() && _flags.IsFlagSet(VdpStatusFlags.FrameInterruptPending))
        {
            // Frame interrupt, so trigger maskable interrupt
            _interruptManagement.SetMaskableInterrupt();
        }

        if (_registers.IsLineInterruptEnabled() && _verticalCounter.IsLineInterruptPending)
        {
            // Line interrupt, so trigger maskable interrupt
            _interruptManagement.SetMaskableInterrupt();
        }
    }

    private byte HandleHvCounterReads(byte port)
    {
        // $40 -$7F : 
        // Reads from even addresses return the V counter.
        // Reads from odd address return the latched H counter.
        return port % 2 == 0 ? _verticalCounter.Counter : _horizontalCounter.LatchedCounterAsByte();
    }

    private byte HandleVdpRead(byte port)
    {
        // $80-$BF :            
        // Reads from even addresses return the VDP data port contents.
        // Reads from odd address return the VDP status flags.

        // Command word writing is reset when control port or data port read
        _controlPortManager.ResetControlByte();
        return port % 2 == 0 ? _ram.ReadData() : ReadVdpStatus();
    }

    private byte ReadVdpStatus()
    {
        var status = (byte)_flags.Flags;

        // Clear all the flags when the status is read
        _flags.ClearAllFlags();
        _verticalCounter.ClearLineInterruptPending();

        return status;
    }
}