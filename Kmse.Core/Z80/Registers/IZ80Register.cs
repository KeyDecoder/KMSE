using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public interface IZ80Register
{
    public void Reset();
    public ushort GetValue();
    public Z80Register AsRegister();
}