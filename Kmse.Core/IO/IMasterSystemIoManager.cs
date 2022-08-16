namespace Kmse.Core.IO;

public interface IMasterSystemIoManager
{
    bool NonMaskableInterrupt { get; }
    bool MaskableInterrupt { get; }

    void SetMaskableInterrupt();
    void ClearMaskableInterrupt();
    void SetNonMaskableInterrupt();
    void ClearNonMaskableInterrupt();

    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}