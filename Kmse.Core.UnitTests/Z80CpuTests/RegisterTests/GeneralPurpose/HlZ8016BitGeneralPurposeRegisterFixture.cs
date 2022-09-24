using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class HlZ8016BitGeneralPurposeRegisterFixture : Z8016BitGeneralPurposeRegisterBaseTest
{
    protected override Z8016BitGeneralPurposeRegisterBase CreateRegister(IMasterSystemMemory memory,
        IZ80FlagsManager flags)
    {
        return new Z80HlRegister(memory, flags, () => new Z808BitGeneralPurposeRegister(memory, flags));
    }

    [Test]
    public void WhenSwappingWithDeRegister()
    {
        var deRegister = Substitute.For<IZ80DeRegister>();
        deRegister.Value.Returns((ushort)0x1234);

        Register.Set(0x3426);

        ((Z80HlRegister)Register).SwapWithDeRegister(deRegister);

        Register.Value.Should().Be(0x1234);
        deRegister.Received(1).Set(0x3426);
    }
}