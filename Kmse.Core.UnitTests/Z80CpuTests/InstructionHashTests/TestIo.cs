using System.Security.Cryptography;
using Kmse.Core.IO;

namespace Kmse.Core.UnitTests.Z80CpuTests.InstructionHashTests;

public class TestIo : IMasterSystemIoManager
{
    private readonly Memory<byte> _portData = new(new byte[0xFF + 1]);

    public TestIo()
    {
        _portData.Span.Fill(0xBB);
    }

    public bool NonMaskableInterrupt { get; private set; }
    public bool MaskableInterrupt { get; private set; }

    public void Reset()
    {
        _portData.Span.Fill(0xBB);
        ClearMaskableInterrupt();
        ClearNonMaskableInterrupt();
    }

    public byte ReadPort(ushort port)
    {
        return _portData.Span[port & 0xFF];
    }

    public void WritePort(ushort port, byte value)
    {
        _portData.Span[port & 0xFF] = value;
    }

    public void SetMaskableInterrupt()
    {
        MaskableInterrupt = true;
    }

    public void ClearMaskableInterrupt()
    {
        MaskableInterrupt = false;
    }

    public void SetNonMaskableInterrupt()
    {
        NonMaskableInterrupt = true;
    }

    public void ClearNonMaskableInterrupt()
    {
        NonMaskableInterrupt = false;
    }

    public string GetHash()
    {
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(_portData.Span.ToArray());
        return BitConverter.ToString(bytes).Replace("-", "");
    }
}