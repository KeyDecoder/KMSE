using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

public class TestZ80IndexRegisterXy : Z80IndexRegisterXy
{
    public TestZ80IndexRegisterXy(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}