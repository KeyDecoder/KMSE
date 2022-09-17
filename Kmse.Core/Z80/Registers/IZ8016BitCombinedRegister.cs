using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public interface IZ8016BitCombinedRegister : IZ8016BitRegister
{
    public ushort ShadowValue { get; }
    public void SwapWithShadow();
    public Z80Register ShadowAsRegister();
}