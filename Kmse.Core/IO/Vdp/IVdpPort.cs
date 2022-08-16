namespace Kmse.Core.IO.Vdp;

public interface IVdpPort
{
    void Reset();
    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}