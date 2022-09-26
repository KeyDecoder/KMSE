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
    private readonly IZ80InterruptManagement _interruptManagement;
    private ushort _addressRegister;
    private byte _codeRegister;
    private bool _commandWordSecondByte;
    private byte[] _cRam;
    private int _hCounter;
    private int _latchedHCounter;

    private byte _readBuffer;
    private byte _vCounter;
    private bool _secondVCount;
    private ushort _vdpCommandWord;

    private VdpStatusFlags _vdpStatus;
    private byte[] _vRam;
    private DataPortWriteMode _writeMode;

    private VdpDisplayType _displayType;

    private byte _lineCounter;
    private bool _isLineInterruptPending;

    private readonly IVdpRegisters _registers;

    public VdpPort(IVdpRegisters registers, IZ80InterruptManagement interruptManagement)
    {
        _interruptManagement = interruptManagement;
        _registers = registers;
    }

    public void Reset()
    {
        _hCounter = 0;
        _latchedHCounter = 0;
        _vCounter = 0;
        _secondVCount = false;
        _vdpCommandWord = 0x00;
        _commandWordSecondByte = false;
        _codeRegister = 0x00;
        _addressRegister = 0x00;
        _vdpStatus = 0x00;
        _readBuffer = 0x00;
        // Assume 16K of video RAM and 32 bytes of color RAM 
        // https://segaretro.org/Sega_Master_System/Technical_specifications
        _vRam = new byte[100000];
        _cRam = new byte[32];
        _writeMode = DataPortWriteMode.VideoRam;
        _registers.Reset();
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
            WriteToVdpDataPort(value);
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
            _latchedHCounter = _hCounter;
        }
    }

    /// <summary>
    ///     Execute update of VDP based on the number of cycles that have been executed by the CPU
    /// </summary>
    /// <param name="cycles">Number of cycles executed since last update</param>
    public void Execute(int cycles)
    {
        IncrementHCounter();

        if (EndOfScanline())
        {
            ResetHCounter();
            IncrementVCounter();

            if (EndOfActiveFrame())
            {
                SetFlag(VdpStatusFlags.FrameInterruptPending);
            }

            if (EndOfFrame())
            {
                ResetVCounter();
            }

            UpdateLineCounter();
        }


        if (_registers.IsFrameInterruptEnabled() && IsFlagSet(VdpStatusFlags.FrameInterruptPending))
        {
            // Frame interrupt, so trigger maskable interrupt
            _interruptManagement.SetMaskableInterrupt();
        }

        if (_registers.IsLineInterruptEnabled() && _isLineInterruptPending)
        {
            // Line interrupt, so trigger maskable interrupt
            _interruptManagement.SetMaskableInterrupt();
        }
    }

    public void SetDisplayType(VdpDisplayType displayType)
    {
        _displayType = displayType;
    }

    public VdpPortStatus GetStatus()
    {
        return new VdpPortStatus
        {
            HCounter = _hCounter,
            VCounter = _vCounter,
            CommandWord = _vdpCommandWord,
            StatusFlags = _vdpStatus,
            CodeRegister = _codeRegister,
            AddressRegister = _addressRegister,
            VdpRegisters = _registers.DumpRegisters(),
            ReadBuffer = _readBuffer,
            WriteMode = _writeMode
        };
    }

    public byte[] DumpVideoRam()
    {
        return _vRam.ToArray();
    }

    public byte[] DumpColourRam()
    {
        return _cRam.ToArray();
    }

    private byte HandleHvCounterReads(byte port)
    {
        // $40 -$7F : 
        // Reads from even addresses return the V counter.
        // Reads from odd address return the latched H counter.
        return port % 2 == 0 ? _vCounter : (byte)((_latchedHCounter >> 1) & 0xFF);
    }

    private byte HandleVdpRead(byte port)
    {
        // $80-$BF :            
        // Reads from even addresses return the VDP data port contents.
        // Reads from odd address return the VDP status flags.

        // Command word writing is reset when control port or data port read
        _commandWordSecondByte = false;
        return port % 2 == 0 ? PerformVdpDataRead() : ReadVdpStatus();
    }

    private byte ReadVdpStatus()
    {
        var status = (byte)_vdpStatus;

        // Clear all the flags when the status is read
        ClearFlag(VdpStatusFlags.FrameInterruptPending);
        ClearFlag(VdpStatusFlags.SpriteCollision);
        ClearFlag(VdpStatusFlags.SpriteOverflow);

        _isLineInterruptPending = false;

        return status;
    }

    private byte PerformVdpDataRead()
    {
        // Reads from VRAM are buffered.Every time the data port is read
        // (regardless of the code register) the contents of a buffer are returned.
        // The VDP will then read a byte from VRAM at the current address, and increment the address
        // register.
        // In this way data for the next data port read is ready with no delay while the VDP reads VRAM.
        var dataToReturn = _readBuffer;
        _readBuffer = ReadFromVideoRam(_addressRegister);
        IncrementAddressRegister();

        return dataToReturn;
    }

    private void WriteToVdpDataPort(byte value)
    {
        // Command word writing is reset when data port is written
        _commandWordSecondByte = false;
        switch (_writeMode)
        {
            case DataPortWriteMode.VideoRam:
                WriteToVideoRam(_addressRegister, value);
                break;
            case DataPortWriteMode.ColourRam:
                WriteToColourRam(_addressRegister, value);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        IncrementAddressRegister();

        // An additional quirk is that writing to the data port will also load the buffer with the value written.
        _readBuffer = value;
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
            _addressRegister &= 0xFF00;
            _addressRegister |= value;
        }
        else
        {
            // Clear top 8 bits and set lower to add to first write
            _vdpCommandWord &= 0x00FF;
            _vdpCommandWord |= (ushort)(value << 8);

            //When the second byte is written, the upper 6 bits of the address
            //register and the code register are updated
            _addressRegister &= 0x00FF;
            // Add the bottom 6 bits of the second command word byte
            _addressRegister |= (ushort)((value & 0x3F) << 8);

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
                // A byte of VRAM is read from the location defined by the
                // address register and is stored in the read buffer.
                // The address register is incremented by one.
                _readBuffer = ReadFromVideoRam(_addressRegister);
                IncrementAddressRegister();
                _writeMode = DataPortWriteMode.VideoRam;
            }
                break;
            case 1:
            {
                // Writes to the data port go to VRAM.
                _writeMode = DataPortWriteMode.VideoRam;
            }
                break;
            case 2:
            {
                // VDP register write
                // Writes to the data port go to VRAM.
                _writeMode = DataPortWriteMode.VideoRam;

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
                _writeMode = DataPortWriteMode.ColourRam;
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

    private void IncrementAddressRegister()
    {
        // Since this is only 14 bits, we wrap around at 0x3FFF
        if (_addressRegister >= 0x3FFF)
        {
            // Wrap around to 0 but don't change control word code at the top 2 bits
            _addressRegister = 0;
        }
        else
        {
            _addressRegister++;
        }
    }

    private byte ReadFromVideoRam(ushort address)
    {
        return _vRam[address];
    }

    private void WriteToVideoRam(ushort address, byte value)
    {
        _vRam[address] = value;
    }

    private void WriteToColourRam(ushort address, byte value)
    {
        // Only 32 bytes in size, so ignore any higher bits which essentially makes this wrap at 31
        _cRam[address & 0x1F] = value;
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

    private void IncrementHCounter()
    {
        _hCounter++;
    }

    private void IncrementVCounter()
    {
        // These jumps change depending on mode and display type
        _vCounter++;
        if (_secondVCount)
        {
            return;
        }

        // Quick implementation, but this needs to be mapped using a better data structure to make this easier
        // TODO: Support the other modes, this only works with PAL 192 lines for now
        if (_vCounter == 0xF3)
        {
            _vCounter = 0xBA;
            _secondVCount = true;
        }
    }

    private void ResetHCounter()
    {
        _hCounter = 0;
    }

    private void ResetVCounter()
    {
        _vCounter = 0;
        _secondVCount = false;
    }

    private bool EndOfScanline()
    {
        return _hCounter >= GetHorizontalLineCount();
    }

    private bool EndOfFrame()
    {
        // The increment will adjust to always make this end up at 0xFF as the last line in a complete frame (active and inactive)
        return _vCounter == 0xFF;
    }

    private bool EndOfActiveFrame()
    {
        return _vCounter == GetActiveFrameSize();
    }

    private void UpdateLineCounter()
    {
        // Apply line counter when drawing active display
        // Otherwise simply load from VDP Register 10 ready for the next active display screen

        if (_vCounter < GetVerticalLineCount() + 1)
        {
            // Decrement the line counter and when zero, trigger a line interrupt
            // This is used to notify applications when reach specific line in rendering
            var underflow = _lineCounter == 0;

            _lineCounter--;
            if (!underflow)
            {
                return;
            }

            _isLineInterruptPending = true;
        }

        _lineCounter = _registers.GetLineCounterValue();
    }

    private int GetVerticalLineCount()
    {
        return _displayType switch
        {
            VdpDisplayType.Ntsc => 262,
            VdpDisplayType.Pal => 313,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private int GetActiveFrameSize()
    {
        var currentMode = _registers.GetVideoMode();
        return currentMode switch
        {
            VdpVideoMode.Mode4With224Lines => 224,
            VdpVideoMode.Mode4With240Lines => 240,
            _ => 192
        };
    }

    private int GetHorizontalLineCount()
    {
        return 342;

    }
}