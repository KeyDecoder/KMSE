namespace Kmse.Core.Z80;

public interface ICpuLogger
{
    void LogMemoryRead(ushort address, byte data);
    void LogInstruction(ushort baseAddress, string operation, string data);
}