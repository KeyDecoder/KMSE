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
    ushort GetTileInformation(ushort baseAddress, int tileIndex);
    (byte spriteX, byte spriteY, byte patternIndex) GetSpriteInformation(ushort baseAddress, int spriteNumber);
    byte[] GetTile(ushort patternAddress, int yOffset, byte tileWidth);
    (byte blue, byte green, byte red, byte alpha) GetColor(ushort address, bool useSecondPalette);
}