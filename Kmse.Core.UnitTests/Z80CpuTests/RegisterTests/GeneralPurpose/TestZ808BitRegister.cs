using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

public class TestZ808BitRegister : Z808BitRegister
{
    public TestZ808BitRegister(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
}