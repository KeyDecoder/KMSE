using System.Security.Cryptography;
using Kmse.Core.Cartridge;
using Kmse.Core.Memory;

namespace Kmse.Core.UnitTests.Z80CpuTests.InstructionHashTests;

public class TestMemory : IMasterSystemMemory
{
    private readonly Memory<byte> _internalRam = new(new byte[0xFFFF + 1]);
    private int _instructionPointer;

    public TestMemory()
    {
        _internalRam.Span.Fill(0xAA);
    }

    public int InstructionCount { get; private set; }

    public void LoadCartridge(IMasterSystemCartridge masterSystemCartridge) { }

    public byte this[ushort address]
    {
        get => _internalRam.Span[address];
        set => _internalRam.Span[address] = value;
    }

    public int GetMaximumAvailableMemorySize()
    {
        return 0xFFFF + 1;
    }

    public int GetMinimumAvailableMemorySize()
    {
        return 0;
    }

    public void Reset()
    {
        _internalRam.Span.Fill(0xAA);
        _instructionPointer = 0;
        InstructionCount = 0;
    }

    public void AddInstruction(byte[] instructionBytes)
    {
        foreach (var value in instructionBytes)
        {
            _internalRam.Span[_instructionPointer++] = value;
        }

        InstructionCount++;
    }

    public string GetHash()
    {
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(_internalRam.Span.ToArray());
        return BitConverter.ToString(bytes).Replace("-", "");
    }
}