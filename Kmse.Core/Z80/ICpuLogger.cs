namespace Kmse.Core.Z80;

public interface ICpuLogger
{
    void LogDebug(string message);
    void LogMemoryRead(ushort address, byte data);
    void LogInstruction(ushort baseAddress, string operation, string data);
}