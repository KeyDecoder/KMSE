using Kmse.Core.Z80.Model;

namespace Kmse.Core.Z80.Registers.General;

/// <summary>
///     Provides a 16 bit interface to the combined Accumulator and Flag (AF) register
/// </summary>
/// <remarks>
///     Note that AF is technically a special purpose register but all the others operates on Flags but this one contains
///     the flags
///     So it is a special case
/// </remarks>
public interface IZ80AfRegister : IZ8016BitRegister
{
    ushort ShadowValue { get; }

    IZ80Accumulator Accumulator { get; }
    IZ80FlagsManager Flags { get; }
    void SwapWithShadow();
    Z80Register ShadowAsRegister();
}