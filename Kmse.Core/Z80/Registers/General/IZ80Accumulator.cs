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
    void AddFromMemory(Z80Register register, int offset, bool withCarry = false);
    void AddFromMemory(Z80Register register, int offset, byte valueToAndAgainst);
    void Add(byte value, bool withCarry = false);
    void SubtractFromMemory(Z80Register register, int offset, bool withCarry = false);
    void Subtract(byte value, bool withCarry = false);
    void Compare(byte value);
    void CompareFromMemory(Z80Register register, int offset);
    void And(byte value, byte valueToAndAgainst);
    void Or(byte value, byte valueToAndAgainst);
    void OrFromMemory(Z80Register register, int offset, byte valueToOrAgainst);
    void Xor(byte value, byte valueToXorAgainst);
    void XorFromMemory(Z80Register register, int offset, byte valueToXorAgainst);
    void RotateLeftCircularAccumulator();
    void RotateLeftAccumulator();
    void RotateRightCircularAccumulator();
    void RotateRightAccumulator();
}