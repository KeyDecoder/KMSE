using Kmse.Core.Z80.Registers;

namespace Kmse.Core.Z80.Memory;

public interface IZ80CpuMemoryManagement
{
    void CopyMemory(IZ8016BitRegister source, IZ8016BitRegister destination);
    byte ReadFromMemory(IZ8016BitRegister register, byte offset = 0);
    void WriteToMemory(IZ8016BitRegister register, byte value, byte offset = 0);
    void IncrementMemory(IZ8016BitRegister register, byte offset = 0);
    void DecrementMemory(IZ8016BitRegister register, byte offset = 0);
}