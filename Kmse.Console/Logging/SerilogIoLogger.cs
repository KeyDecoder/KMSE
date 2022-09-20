using Kmse.Core.IO.Logging;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogIoLogger : IIoPortLogger
{
    private readonly ILogger _logger;
    private bool _verboseLoggingEnabled;

    public SerilogIoLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Debug(string port, string message)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("{Port}: {Message}", port, message);
    }

    public void Information(string port, string message)
    {
        _logger.Information("{Port}: {Message}", port, message);
    }

    public void Error(string port, string message)
    {
        _logger.Error("{Port}: {Message}", port, message);
    }

    public void PortRead(ushort address, byte data)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Read I/O Port at address {Address:X4}, got {Data}", address, data);
    }

    public void PortWrite(ushort address, byte newData)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _logger.Debug("Wrote to I/O port at address {Address:X4} to {NewData:X2}", address, newData);
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