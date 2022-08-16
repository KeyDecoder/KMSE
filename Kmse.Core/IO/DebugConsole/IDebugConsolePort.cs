namespace Kmse.Core.IO.DebugConsole;

public interface IDebugConsolePort
{
    void WritePort(ushort port, byte value);
}