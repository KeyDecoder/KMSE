using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Ram;

public class VdpRam : IVdpRam
{
    private byte[] _cRam;
    private byte _readBuffer;
    private byte[] _vRam;
    public DataPortWriteMode WriteMode { get; private set; }
    public ushort AddressRegister { get; private set; }

    public void Reset()
    {
        AddressRegister = 0x00;
        _readBuffer = 0x00;

        // Assume 16K of video RAM and 32 bytes of color RAM 
        // https://segaretro.org/Sega_Master_System/Technical_specifications
        _vRam = new byte[0x4000];
        _cRam = new byte[32];

        WriteMode = DataPortWriteMode.VideoRam;
    }

    public byte ReadData()
    {
        // Reads from VRAM are buffered.Every time the data port is read
        // (regardless of the code register) the contents of a buffer are returned.
        // The VDP will then read a byte from VRAM at the current address, and increment the address
        // register.
        // In this way data for the next data port read is ready with no delay while the VDP reads VRAM.
        var dataToReturn = _readBuffer;
        _readBuffer = ReadFromVideoRam(AddressRegister);
        IncrementAddressRegister();

        return dataToReturn;
    }

    public void WriteData(byte value)
    {
        switch (WriteMode)
        {
            case DataPortWriteMode.VideoRam:
                WriteToVideoRam(AddressRegister, value);
                break;
            case DataPortWriteMode.ColourRam:
                WriteToColourRam(AddressRegister, value);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        IncrementAddressRegister();

        // An additional quirk is that writing to the data port will also load the buffer with the value written.
        _readBuffer = value;
    }

    public byte GetReadBufferValue()
    {
        return _readBuffer;
    }

    public void IncrementAddressRegister()
    {
        // Since this is only 14 bits, we wrap around at 0x3FFF
        if (AddressRegister >= 0x3FFF)
        {
            // Wrap around to 0 but don't change control word code at the top 2 bits
            AddressRegister = 0;
        }
        else
        {
            AddressRegister++;
        }
    }

    public void SetWriteModeToVideoRam()
    {
        WriteMode = DataPortWriteMode.VideoRam;
    }

    public void SetWriteModeToColourRam()
    {
        WriteMode = DataPortWriteMode.ColourRam;
    }

    public void UpdateAddressRegisterLowerByte(byte value)
    {
        //When the first byte is written, the lower 8 bits of the address register are updated
        // Clear lower 8 bits and set from first byte of command word but preserve the upper 6 bits until the next write
        AddressRegister &= 0xFF00;
        AddressRegister |= value;
    }

    public void UpdateAddressRegisterUpperByte(byte value)
    {
        //When the second byte is written, the upper 6 bits of the address
        //register and the code register are updated
        AddressRegister &= 0x00FF;
        AddressRegister |= (ushort)(value << 8);
    }

    public byte[] DumpVideoRam()
    {
        return _vRam.ToArray();
    }

    public byte[] DumpColourRam()
    {
        return _cRam.ToArray();
    }

    public void ReadFromVideoRamIntoBuffer()
    {
        // A byte of VRAM is read from the location defined by the
        // address register and is stored in the read buffer.
        // The address register is incremented by one.

        _readBuffer = ReadFromVideoRam(AddressRegister);
        IncrementAddressRegister();
    }

    private byte ReadFromVideoRam(ushort address)
    {
        return _vRam[address];
    }

    public void WriteToVideoRam(ushort address, byte value)
    {
        _vRam[address] = value;
    }

    public void WriteToColourRam(ushort address, byte value)
    {
        // Only 32 bytes in size, so ignore any higher bits which essentially makes this wrap at 31
        _cRam[address & 0x1F] = value;
    }

    public byte ReadRawVideoRam(ushort address)
    {
        return _vRam[address];
    }

    public byte ReadRawColourRam(ushort address)
    {
        return _cRam[address];
    }
}