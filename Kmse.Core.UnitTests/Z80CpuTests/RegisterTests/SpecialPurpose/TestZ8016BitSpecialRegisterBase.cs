using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

public class TestZ8016BitSpecialRegisterBase : Z8016BitSpecialRegisterBase
{
    public TestZ8016BitSpecialRegisterBase(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}