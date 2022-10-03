using Kmse.Core.Memory;
using Kmse.TestUi;

namespace Kmse.TestUI.Logging;

/// <summary>
///     Memory subsystem logger which logs memory usage and diagnostics
///     Simple logger for console debugging
/// </summary>
public class UiMemoryLogger : IMemoryLogger
{
    private readonly frmMain _form;

    public UiMemoryLogger(frmMain form)
    {
        _form = form;
    }

    public void Debug(string message)
    {
        _form.LogDebug(message);
    }

    public void Error(string message)
    {
        _form.LogError(message);
    }

    public void MemoryRead(ushort address, byte data)
    {
        _form.LogMemoryOperation($"Read system RAM at address {address:X4}, got {data}");
    }

    public void CartridgeRead(int address, byte data)
    {
        _form.LogMemoryOperation($"Read cartridge ROM memory at address {address:X4}, got {data:X2}");
    }

    public void RamBankMemoryRead(int bank, ushort address, byte data)
    {
        // Disable this for now to avoid overloading the UI
        _form.LogMemoryOperation($"Read RAM bank {bank} at address {address:X4}, got {data:X2}");
    }

    public void MemoryWrite(ushort address, byte oldData, byte newData)
    {
        _form.LogMemoryOperation($"Wrote to RAM at address {address:X4}, was {oldData:X2}, now {newData:X2}");
    }

    public void RamBankMemoryWrite(int bank, ushort address, byte oldData, byte newData)
    {
        _form.LogMemoryOperation($"Wrote to RAM bank {bank} at address {address:X4}, was {oldData:X2}, now {newData:X2}");
    }
}