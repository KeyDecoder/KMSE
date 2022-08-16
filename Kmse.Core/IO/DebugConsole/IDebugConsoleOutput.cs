namespace Kmse.Core.IO.DebugConsole;

public interface IDebugConsoleOutput
{
    void WriteCharacter(char value);
    void NewLine();

    // TODO: Add support for clearing, positioning, color etc
}