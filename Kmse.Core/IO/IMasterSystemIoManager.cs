namespace Kmse.Core.IO;

public interface IMasterSystemIoManager
{
    void Reset();

    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}