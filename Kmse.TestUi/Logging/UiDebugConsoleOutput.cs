using System.Text;
using Kmse.Core.IO.DebugConsole;
using Kmse.TestUi;

namespace Kmse.TestUI.Logging;

public class UiDebugConsoleOutput : IDebugConsoleOutput
{
    private readonly frmMain _form;
    private readonly StringBuilder _output = new();

    public UiDebugConsoleOutput(frmMain form)
    {
        _form = form;
    }

    public void WriteCharacter(char value)
    {
        _output.Append(value);
    }

    public void NewLine()
    {
        _form.LogInformation(_output.ToString());
        _output.Clear();
    }
}