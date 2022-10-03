using Kmse.Core.IO.Logging;
using Kmse.TestUi;

namespace Kmse.TestUI.Logging;

public class UiIoLogger : IIoPortLogger
{
    private readonly frmMain _form;

    public UiIoLogger(frmMain form)
    {
        _form = form;
    }

    public void Debug(string port, string message)
    {
        _form.LogDebug($"{port}: {message}");
    }

    public void Information(string port, string message)
    {
        _form.LogInformation($"{port}: {message}");
    }

    public void Error(string port, string message)
    {
        _form.LogError($"{port}: {message}");
    }

    public void PortRead(ushort address, byte data)
    {
        _form.LogDebug($"Read I/O Port at address {address:X4}, got {data}");
    }

    public void PortWrite(ushort address, byte newData)
    {
        _form.LogDebug($"Wrote to I/O port at address {address:X4} to {newData:X2}");
    }
}