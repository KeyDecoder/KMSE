using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.General;

/// <summary>
///     Register for CPU Flags
/// </summary>
/// <remarks>
///     Even though this is a general purpose register, it only is used via flag interfaces primarily
///     So we don't use the IZ80Register interface
///     Also we avoid implementing this same as normal general purpose registers which have helper methods that affect
///     flags
///     but obviously the flags register doesn't do any of that
/// </remarks>
public interface IZ80FlagsManager
{
    byte Value { get; }
    byte ShadowValue { get; }

    void Reset();
    void Set(byte value);
    void SwapWithShadow();

    void SetFlag(Z80StatusFlags flags);
    void ClearFlag(Z80StatusFlags flags);
    void InvertFlag(Z80StatusFlags flag);
    void SetClearFlagConditional(Z80StatusFlags flags, bool condition);
    bool IsFlagSet(Z80StatusFlags flags);
    void SetParityFromValue(byte value);
}