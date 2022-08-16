namespace Kmse.Core.IO.DebugConsole;

/// <summary>
///     Debug console I/O port
///     Used in custom ROMs but not in production games
///     Useful for testing using ZXDOC/ZXALL rom to output debug information quickly and easily
/// </summary>
internal class DebugConsolePort : IDebugConsolePort
{
    private const byte DebugConsoleControlPort = 0xFC;
    private const byte DebugConsoleDataPort = 0xFD;
    private readonly IDebugConsoleOutput _output;

    public DebugConsolePort(IDebugConsoleOutput output)
    {
        _output = output;
    }

    public void Reset()
    {
    }

    public void WritePort(ushort port, byte value)
    {
        switch (port)
        {
            case DebugConsoleControlPort:
                WriteDebugConsoleControl(value);
                break;
            case DebugConsoleDataPort:
                WriteDebugConsoleData(value);
                break;
        }
    }

    private void WriteDebugConsoleData(byte value)
    {
        // https://www.smspower.org/Development/SDSCDebugConsoleSpecification

        switch (value)
        {
            case 10 or 13:
                _output.NewLine();
                break;
            case >= 32 and < 127:
                _output.WriteCharacter(Convert.ToChar(value));
                break;
        }
    }

    private void WriteDebugConsoleControl(byte value)
    {
        // TODO: Implement control codes for position, colour etc
    }
}