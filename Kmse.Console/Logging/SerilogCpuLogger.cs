using Kmse.Core.Z80;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogCpuLogger : ICpuLogger
{
    private readonly ILogger _log;
    private readonly DebugFileLogger _fileLogger;


    public SerilogCpuLogger(ILogger log, DebugFileLogger fileLogger)
    {
        _log = log;
        _fileLogger = fileLogger;
    }

    public void LogDebug(string message)
    {
        _log.Debug("CPU Debug: {Message}",message);
        _fileLogger.LogDebug($"CPU: {message}");
    }

    public void LogMemoryRead(ushort address, byte data)
    {
#if CONSOLE_LOG
        _log.Debug($"0x{address:X4} 0x{data:X2}");
#endif
        _fileLogger.LogInformation($"Mem Read: 0x{address:X4} 0x{data:X2}");
    }

    public void LogInstruction(ushort baseAddress, byte opCode, string operation, string data)
    {
#if CONSOLE_LOG
        _log.Debug($"0x{baseAddress:X4} 0x{opCode:X2} {operation} {data}");
#endif
        _fileLogger.LogInstruction(baseAddress, opCode, operation, data);
    }
}