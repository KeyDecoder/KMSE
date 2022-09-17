using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.General;

public interface IZ80FlagsManager : IZ808BitRegister
{
    void SetFlag(Z80StatusFlags flags);
    void ClearFlag(Z80StatusFlags flags);
    void InvertFlag(Z80StatusFlags flag);
    void SetClearFlagConditional(Z80StatusFlags flags, bool condition);
    bool IsFlagSet(Z80StatusFlags flags);
    void SetParityFromValue(byte value);
}