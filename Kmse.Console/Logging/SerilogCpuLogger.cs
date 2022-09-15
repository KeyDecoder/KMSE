using Kmse.Core.Z80;
using Kmse.Core.Z80.Logging;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogCpuLogger : ICpuLogger
{
    private readonly ILogger _log;
    private bool _verboseLoggingEnabled;

    public SerilogCpuLogger(ILogger log)
    {
        _log = log;
    }

    public void Debug(string message)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _log.Debug("CPU Debug: {Message}", message);
    }

    public void Error(string message)
    {
        _log.Error("CPU Debug: {Message}", message);
    }

    public void LogInstruction(ushort baseAddress, string opCode, string operationName, string operationDescription,
        string data)
    {
        if (!_verboseLoggingEnabled)
        {
            return;
        }

        _log.Debug(!string.IsNullOrWhiteSpace(data)
            ? $"0x{baseAddress:X4}: {opCode} - {data} ({operationName} - {operationDescription})"
            : $"0x{baseAddress:X4}: {opCode} ({operationName} - {operationDescription})");
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