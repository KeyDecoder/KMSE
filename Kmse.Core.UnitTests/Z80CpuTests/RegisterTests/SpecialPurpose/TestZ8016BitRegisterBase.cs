using Kmse.Core.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

public class TestZ8016BitRegisterBase : Z8016BitRegisterBase
{
    private Unsigned16BitValue _register;

    public TestZ8016BitRegisterBase(IMasterSystemMemory memory, IZ80FlagsManager flags)
        : base(memory, flags)
    {
        _register = new Unsigned16BitValue();
    }

    public override ushort Value => _register.Word;
    public override byte High => _register.High;
    public override byte Low => _register.Low;

    public override void Set(ushort value)
    {
        _register.Word = value;
    }
}