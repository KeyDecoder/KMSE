namespace Kmse.Core.IO.Sound;

public interface ISoundPort
{
    void Reset();
    void WritePort(ushort port, byte value);
}