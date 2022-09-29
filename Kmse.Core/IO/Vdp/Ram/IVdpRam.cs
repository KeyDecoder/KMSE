using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Ram;

public interface IVdpRam
{
    DataPortWriteMode WriteMode { get; }
    ushort AddressRegister { get; }
    void Reset();
    byte ReadData();
    void WriteData(byte value);
    byte GetReadBufferValue();
    void IncrementAddressRegister();
    void SetWriteModeToVideoRam();
    void SetWriteModeToColourRam();
    void UpdateAddressRegisterUpperByte(byte value);
    void UpdateAddressRegisterLowerByte(byte value);
    byte[] DumpVideoRam();
    byte[] DumpColourRam();
    void ReadFromVideoRamIntoBuffer();
    void WriteToVideoRam(ushort address, byte value);
    void WriteToColourRam(ushort address, byte value);
    byte ReadRawVideoRam(ushort address);
    byte ReadRawColourRam(ushort address);
}