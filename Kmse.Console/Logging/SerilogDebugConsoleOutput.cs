using System.Text;
using Kmse.Core.IO.DebugConsole;
using Serilog;

namespace Kmse.Console.Logging;

public class SerilogDebugConsoleOutput : IDebugConsoleOutput
{
    private readonly ILogger _logger;
    private readonly DebugFileLogger _fileLogger;
    private readonly StringBuilder _output = new();

    public SerilogDebugConsoleOutput(ILogger logger, DebugFileLogger fileLogger)
    {
        _logger = logger;
        _fileLogger = fileLogger;
    }

    public void WriteCharacter(char value)
    {
        _output.Append(value);
    }

    public void NewLine()
    {
        _logger.Information($"{_output}");
        _fileLogger.LogInformation($"Debug Console: {_output}");
        _output.Clear();
    }
}