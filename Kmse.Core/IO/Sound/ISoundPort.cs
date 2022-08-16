namespace Kmse.Core.IO.Sound;

public interface ISoundPort
{
    void WritePort(ushort port, byte value);
}