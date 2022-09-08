namespace Kmse.Core.IO.Sound;

public interface ISoundPort
{
    void Reset();
    void WritePort(byte port, byte value);
    void Execute(int cycles);
}