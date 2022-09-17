namespace Kmse.Core.Z80.Registers.General;

/// <summary>
///     Provides a 16 bit interface to the combined Accumulator and Flag (AF) register
/// </summary>
public interface IZ80AfRegister : IZ8016BitCombinedRegister
{
    IZ80Accumulator Accumulator { get; }
    IZ80FlagsManager Flags { get; }
}