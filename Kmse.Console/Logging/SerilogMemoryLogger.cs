using Kmse.Core.Memory;
using Serilog;
using System.Net;

namespace Kmse.Console.Logging;

/// <summary>
/// Memory subsystem logger which logs memory usage and diagnostics
/// Simple logger for console debugging
/// </summary>
public class SerilogMemoryLogger : IMemoryLogger
{
    private readonly ILogger _logger;
    private readonly DebugFileLogger _fileLogger;

    public SerilogMemoryLogger(ILogger logger, DebugFileLogger fileLogger)
    {
        _logger = logger;
        _fileLogger = fileLogger;
    }

    public void Information(string message)
    {
        _logger.Information(message);
    }

    public void Error(string message)
    {
        _logger.Error(message);
        _fileLogger.LogError(message);
    }

    public void MemoryRead(ushort address, byte data)
    {
#if CONSOLE_LOG
        _logger.Debug("Read system RAM at address {Address:X4}, got {Data}", address, data);
#endif
    }

    public void CartridgeRead(ushort address, byte data)
    {
#if CONSOLE_LOG
        _logger.Debug("Read cartridge ROM memory at address {Address:X4}, got {Data:X2}", address, data);
#endif
    }

    public void RamBankMemoryRead(int bank, ushort address, byte data)
    {
#if CONSOLE_LOG
        _logger.Debug("Read RAM bank {Bank} at address {Address:X4}, got {Data:X2}", bank, address, data);
#endif
    }

    public void MemoryWrite(ushort address, byte oldData, byte newData)
    {
#if CONSOLE_LOG
        _logger.Debug("Wrote to RAM at address {Address:X4}, was {OldData:X2}, now {NewData:X2}", address, oldData, newData);
#endif
    }

    public void RamBankMemoryWrite(int bank, ushort address, byte oldData, byte newData)
    {
#if CONSOLE_LOG
        _logger.Debug("Wrote to RAM bank {Bank} at address {Address:X4}, was {OldData:X2}, now {NewData:X2}", bank, address, oldData, newData);
#endif
    }
}