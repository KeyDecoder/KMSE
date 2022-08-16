namespace Kmse.Core.IO.DebugConsole;

public interface IDebugConsolePort
{
    void Reset();
    void WritePort(ushort port, byte value);
}