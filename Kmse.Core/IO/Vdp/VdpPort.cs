using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
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
    private readonly IZ80InterruptManagement _interruptManagement;
    private readonly IVdpRam _ram;

    private readonly IVdpRegisters _registers;
    private readonly IVdpVerticalCounter _verticalCounter;
    private byte _codeRegister;
    private bool _commandWordSecondByte;
    private ushort _vdpCommandWord;
    private VdpStatusFlags _vdpStatus;

    public VdpPort(IVdpRegisters registers, IZ80InterruptManagement interruptManagement, IVdpRam ram,
        IVdpVerticalCounter verticalCounter, IVdpHorizontalCounter horizontalCounter)
    {
        _interruptManagement = interruptManagement;
        _ram = ram;
        _verticalCounter = verticalCounter;
        _horizontalCounter = horizontalCounter;
        _registers = registers;
    }

    public void Reset()
    {
        _vdpCommandWord = 0x00;
        _commandWordSecondByte = false;
        _codeRegister = 0x00;
        _vdpStatus = 0x00;
        _registers.Reset();
        _ram.Reset();
        _verticalCounter.Reset();
        _horizontalCounter.Reset();
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
            _commandWordSecondByte = false;
            _ram.WriteData(value);
        }
        else
        {
            WriteToVdpControlPort(value);
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

    /// <summary>
    ///     Execute update of VDP based on the number of cycles that have been executed by the CPU
    /// </summary>
    /// <param name="cycles">Number of cycles executed since last update</param>
    public void Execute(int cycles)
    {
        _horizontalCounter.Increment();

        if (_horizontalCounter.EndOfScanline())
        {
            _horizontalCounter.ResetLine();
            _verticalCounter.Increment();

            if (_verticalCounter.EndOfActiveFrame())
            {
                SetFlag(VdpStatusFlags.FrameInterruptPending);
            }

            if (_verticalCounter.EndOfFrame())
            {
                _verticalCounter.ResetFrame();
            }

            _verticalCounter.UpdateLineCounter();
        }

        if (_registers.IsFrameInterruptEnabled() && IsFlagSet(VdpStatusFlags.FrameInterruptPending))
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
            CommandWord = _vdpCommandWord,
            StatusFlags = _vdpStatus,
            CodeRegister = _codeRegister,
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
        _commandWordSecondByte = false;
        return port % 2 == 0 ? _ram.ReadData() : ReadVdpStatus();
    }

    private byte ReadVdpStatus()
    {
        var status = (byte)_vdpStatus;

        // Clear all the flags when the status is read
        ClearFlag(VdpStatusFlags.FrameInterruptPending);
        ClearFlag(VdpStatusFlags.SpriteCollision);
        ClearFlag(VdpStatusFlags.SpriteOverflow);

        _verticalCounter.ClearLineInterruptPending();

        return status;
    }

    private void WriteToVdpControlPort(byte value)
    {
        // Command word structure
        // This is two bytes but written into two I/O writes
        // So we keep track of if we have written the first or second byte yet
        //MSB LSB
        //CD1 CD0 A13 A12 A11 A10 A09 A08    Second byte written
        //A07 A06 A05 A04 A03 A02 A01 A00    First byte written

        if (!_commandWordSecondByte)
        {
            // Keep upper 8 bits and set lower
            // We maintain the upper 8 bits while writing rather than clearing (not sure why?)
            _vdpCommandWord &= 0xFF00;
            _vdpCommandWord |= value;

            //When the first byte is written, the lower 8 bits of the address register are updated
            // Clear lower 8 bits and set from first byte of command word but preserve the upper 6 bits until the next write
            _ram.UpdateAddressRegisterLowerByte(value);
        }
        else
        {
            // Clear top 8 bits and set lower to add to first write
            _vdpCommandWord &= 0x00FF;
            _vdpCommandWord |= (ushort)(value << 8);

            //When the second byte is written, the upper 6 bits of the address
            //register and the code register are updated

            // Add the bottom 6 bits of the second command word byte to the address register
            var upperAddressValue = (byte)(value & 0x3F);
            _ram.UpdateAddressRegisterUpperByte(upperAddressValue);

            // Set code register to value in top 2 bits of second command word byte
            _codeRegister = (byte)((value & 0xC0) >> 6);

            ProcessCodeRegisterChange();
        }

        _commandWordSecondByte = !_commandWordSecondByte;
    }

    private void ProcessCodeRegisterChange()
    {
        switch (_codeRegister)
        {
            case 0:
            {
                _ram.ReadFromVideoRamIntoBuffer();
                _ram.SetWriteModeToVideoRam();
            }
                break;
            case 1:
            {
                // Writes to the data port go to VRAM.
                _ram.SetWriteModeToVideoRam();
            }
                break;
            case 2:
            {
                // VDP register write
                // Writes to the data port go to VRAM.
                _ram.SetWriteModeToVideoRam();

                // MSB LSB
                // 1   0 ?   ? R03 R02 R01 R00 Second byte written
                // D07 D06 D05 D04 D03 D02 D01 D00    First byte written
                // Rxx: VDP register number
                // Dxx : VDP register data
                var registerNumber = (byte)((_vdpCommandWord & 0x0F00) >> 8);
                var registerData = (byte)(_vdpCommandWord & 0x00FF);
                ProcessVdpRegisterWrite(registerNumber, registerData);
            }
                break;
            case 3:
            {
                // Writes to the data port go to CRAM.
                _ram.SetWriteModeToColourRam();
            }
                break;
            default: throw new InvalidOperationException($"VDP code register value '{_codeRegister}' is not valid");
        }
    }

    private void ProcessVdpRegisterWrite(byte registerNumber, byte registerData)
    {
        if (registerNumber > 11)
        {
            // There are only 11 registers, values 11 through 15 have no effect when written to.
            return;
        }

        _registers.SetRegister(registerNumber, registerData);
    }

    private void SetFlag(VdpStatusFlags flags)
    {
        _vdpStatus |= flags;
    }

    private void ClearFlag(VdpStatusFlags flags)
    {
        _vdpStatus &= ~flags;
    }

    private void SetClearFlagConditional(VdpStatusFlags flags, bool condition)
    {
        if (condition)
        {
            SetFlag(flags);
        }
        else
        {
            ClearFlag(flags);
        }
    }

    private bool IsFlagSet(VdpStatusFlags flags)
    {
        var currentSetFlags = _vdpStatus & flags;
        return currentSetFlags == flags;
    }
}