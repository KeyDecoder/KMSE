using Kmse.Core.IO.Logging;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogIoLogger : IIoPortLogger
{
    private readonly ILogger _logger;

    public SerilogIoLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Debug(string port, string message)
    {
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

    public void ReadPort(ushort address, byte data)
    {
#if CONSOLE_LOG
        _logger.Debug("Read I/O Port at address {Address:X4}, got {Data}", address, data);
#endif
    }

    public void WritePort(ushort address, byte newData)
    {
#if CONSOLE_LOG
        _logger.Debug("Wrote to I/O port at address {Address:X4} to {NewData:X2}", address, newData);
#endif
    }

    public void SetMaskableInterruptStatus(bool status)
    {
        _logger.Debug("Set MI to {Status}", status);
    }

    public void SetNonMaskableInterruptStatus(bool status)
    {
        _logger.Debug("Set NMI to {Status}", status);
    }
}