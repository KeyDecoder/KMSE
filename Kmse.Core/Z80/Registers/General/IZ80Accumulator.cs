using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.Z80.Registers.General;

public interface IZ80Accumulator : IZ808BitRegister
{
    void SetFromInterruptRegister(IZ80InterruptPageAddressRegister register, bool interruptFlipFlop2Status);
    void SetFromMemoryRefreshRegister(IZ80MemoryRefreshRegister register, bool interruptFlipFlop2Status);
}