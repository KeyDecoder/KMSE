using Kmse.Core.Z80.Registers;

namespace Kmse.Core.Z80.IO;

public interface IZ80CpuInputOutput
{
    byte Read(byte high, byte low, bool setFlags);
    void ReadAndSetRegister(byte high, byte low, IZ808BitRegister register);
    void ReadAndSetRegister(IZ8016BitRegister addressRegister, IZ808BitRegister register);
    void Write(byte high, byte low, IZ808BitRegister register);
}