namespace Kmse.Core.IO.Vdp.Control;

public interface IVdpControlPortManager
{
    ushort CommandWord { get; }
    byte CodeRegister { get; }
    void Reset();
    void ResetControlByte();
    void WriteToVdpControlPort(byte value);
}