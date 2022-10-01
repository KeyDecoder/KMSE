using Kmse.Core.Memory;
using Serilog;

namespace Kmse.Console.Logging;

/// <summary>
///     Memory subsystem logger which logs memory usage and diagnostics
///     Simple logger for console debugging
/// </summary>
public class SerilogMemoryLogger : IMemoryLogger
{
    private readonly ILogger _logger;
    private bool _verboseLoggingEnabled;

    public SerilogMemoryLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Debug(string message)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug(message);
    }

    public void Error(string message)
    {
        _logger.Error(message);
    }

    public void MemoryRead(ushort address, byte data)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Read system RAM at address {Address:X4}, got {Data}", address, data);
    }

    public void CartridgeRead(int address, byte data)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Read cartridge ROM memory at address {Address:X4}, got {Data:X2}", address, data);
    }

    public void RamBankMemoryRead(int bank, ushort address, byte data)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Read RAM bank {Bank} at address {Address:X4}, got {Data:X2}", bank, address, data);
    }

    public void MemoryWrite(ushort address, byte oldData, byte newData)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Wrote to RAM at address {Address:X4}, was {OldData:X2}, now {NewData:X2}", address, oldData,
            newData);
    }

    public void RamBankMemoryWrite(int bank, ushort address, byte oldData, byte newData)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Wrote to RAM bank {Bank} at address {Address:X4}, was {OldData:X2}, now {NewData:X2}", bank,
            address, oldData, newData);
    }

    public void EnableVerboseLogging()
    {
        _verboseLoggingEnabled = true;
    }

    public void DisableVerboseLogging()
    {
        _verboseLoggingEnabled = false;
    }

    public void EnableDisableVerboseLogging()
    {
        if (_verboseLoggingEnabled)
        {
            DisableVerboseLogging();
        }
        else
        {
            EnableVerboseLogging();
        }
    }
}