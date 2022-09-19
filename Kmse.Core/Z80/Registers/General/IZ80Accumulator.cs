using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.General;

public interface IZ80Accumulator : IZ808BitGeneralPurposeRegister
{
    void SetFromInterruptRegister(IZ80InterruptPageAddressRegister register, bool interruptFlipFlop2Status);
    void SetFromMemoryRefreshRegister(IZ80MemoryRefreshRegister register, bool interruptFlipFlop2Status);

    void RotateLeftDigit(IZ80HlRegister hl);
    void RotateRightDigit(IZ80HlRegister hl);

    void DecimalAdjustAccumulator();
    void InvertAccumulatorRegister();
    void NegateAccumulatorRegister();
    void AddFromMemory(IZ8016BitRegister register, int offset, bool withCarry = false);
    void Add(byte value, bool withCarry = false);
    void SubtractFromMemory(IZ8016BitRegister register, int offset, bool withCarry = false);
    void Subtract(byte value, bool withCarry = false);
    void Compare(byte value);
    void CompareFromMemory(IZ8016BitRegister register, int offset);
    void And(byte value, byte valueToAndAgainst);
    void AndFromMemory(IZ8016BitRegister register, int offset, byte valueToAndAgainst);
    void Or(byte value, byte valueToAndAgainst);
    void OrFromMemory(IZ8016BitRegister register, int offset, byte valueToOrAgainst);
    void Xor(byte value, byte valueToXorAgainst);
    void XorFromMemory(IZ8016BitRegister register, int offset, byte valueToXorAgainst);
    void RotateLeftCircularAccumulator();
    void RotateLeftAccumulator();
    void RotateRightCircularAccumulator();
    void RotateRightAccumulator();
}