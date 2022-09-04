namespace Kmse.Console.Logging;

public class DebugFileLogger
{
    private readonly StreamWriter _debugFile;

    public DebugFileLogger()
    {
        //_debugFile = new StreamWriter(@"..\..\..\..\..\kmse-output.txt");
    }

    public void LogDebug(string error)
    {
        _debugFile?.WriteLine($"DEBUG: {error}");
    }

    public void LogInformation(string error)
    {
        _debugFile?.WriteLine($"INFO: {error}");
    }

    public void LogError(string error)
    {
        _debugFile?.WriteLine($"ERROR: {error}");
    }

    public void LogInstruction(ushort baseAddress, byte opCode, string operation, string data)
    {
        _debugFile?.WriteLine($"0x{baseAddress:X4} 0x{opCode:X2} {operation} {data}");
    }

    public void CloseAndFlush()
    {
        _debugFile?.Flush();
        _debugFile?.Close();
    }
}