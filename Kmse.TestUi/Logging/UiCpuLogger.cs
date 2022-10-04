using Kmse.Core.Z80.Logging;
using Kmse.TestUi;

namespace Kmse.TestUI.Logging;

public class UiCpuLogger : ICpuLogger
{
    private readonly frmMain _form;

    public UiCpuLogger(frmMain form)
    {
        _form = form;
    }

    public void Debug(string message)
    {
        _form.LogDebug($"CPU: {message}");
    }

    public void Error(string message)
    {
        _form.LogError($"CPU: {message}");
    }

    public void LogInstruction(ushort baseAddress, string opCode, string operationName, string operationDescription,
        string data)
    {
        _form.LogInstruction(!string.IsNullOrWhiteSpace(data)
            ? $"{baseAddress:X4}: {opCode} - {data} ({operationName} - {operationDescription})"
            : $"{baseAddress:X4}: {opCode} ({operationName} - {operationDescription})");
    }

    public void SetMaskableInterruptStatus(bool status)
    {
        _form.LogDebug($"CPU: Set MI to {status}");
    }

    public void SetNonMaskableInterruptStatus(bool status)
    {
        _form.LogDebug($"CPU: Set NMI to {status}");
    }
}