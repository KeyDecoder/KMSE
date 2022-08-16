namespace Kmse.Core.IO.Vdp;

public interface IVdpPort
{
    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}