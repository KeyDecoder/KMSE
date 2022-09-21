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
    private ushort _addressRegister;
    private byte _codeRegister;
    private bool _commandWordSecondByte;
    private byte[] _cRam;
    private byte _hCounter;

    private byte _readBuffer;
    private byte _vCounter;
    private ushort _vdpCommandWord;

    private VdpStatusFlags _vdpStatus;
    private byte[] _vRam;
    private DataPortWriteMode _writeMode;

    private VdpRegisters _registers;

    public void Reset()
    {
        _hCounter = 0;
        _vCounter = 0;
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
        _registers = new VdpRegisters();
    }

    public byte ReadPort(byte port)
    {
        return port switch
        {
            < 0x40 => throw new InvalidOperationException(
                $"Invalid operation reading from port at address {port} which is not a valid VDP port"),
            < 0x80 => HandleHvCounterReads(port),
            < 0xBF => HandleVdpRead(port),
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

    /// <summary>
    ///     Execute update of VDP based on the number of cycles that have been executed by the CPU
    /// </summary>
    /// <param name="cycles">Number of cycles executed since last update</param>
    public void Execute(int cycles)
    {
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
        // Reads from odd address return the H counter.
        return port % 2 == 0 ? _vCounter : _hCounter;
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
}