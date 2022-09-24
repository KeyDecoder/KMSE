using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class DeZ8016BitGeneralPurposeRegisterFixture : Z8016BitGeneralPurposeRegisterBaseTest
{
    protected override Z8016BitGeneralPurposeRegisterBase CreateRegister(IMasterSystemMemory memory,
        IZ80FlagsManager flags)
    {
        return new Z80DeRegister(memory, flags, () => new Z808BitGeneralPurposeRegister(memory, flags));
    }
}