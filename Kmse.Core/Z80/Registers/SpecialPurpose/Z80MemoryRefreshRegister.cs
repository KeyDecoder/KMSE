using Kmse.Core.Memory;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80MemoryRefreshRegister : Z808BitRegister, IZ80MemoryRefreshRegister
{
    public Z80MemoryRefreshRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
    
    public void Increment(byte value)
    {
        // Increment value but counter is only 7 bits
        var newValue = (byte)((Value + value) & 0x7F);

        // Preserve the top bit which can only be set via Load operation (LD R,A)
        Bitwise.SetIf(ref newValue, 7, () => Bitwise.IsSet(Value, 7));

        Set(newValue);
    }
}