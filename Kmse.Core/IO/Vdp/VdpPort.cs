namespace Kmse.Core.IO.Vdp;

public class VdpPort : IVdpPort
{
    public void Reset()
    {
    }

    public byte ReadPort(ushort port)
    {
        return 0x00;
    }

    public void WritePort(ushort port, byte value)
    {
    }
}