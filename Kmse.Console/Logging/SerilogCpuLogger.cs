using Kmse.Core.Z80;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogCpuLogger : ICpuLogger
{
    private readonly ILogger _log;

    public SerilogCpuLogger(ILogger log)
    {
        _log = log;
    }

    public void LogDebug(string message)
    {
        _log.Debug("CPU Debug: {Message}",message);
    }

    public void LogMemoryRead(ushort address, byte data)
    {
        _log.Debug($"0x{address:X4} 0x{data:X2}");
    }

    public void LogInstruction(ushort baseAddress, string operation, string data)
    {
        _log.Debug($"0x{baseAddress:X4} {operation} {data}");
    }
}