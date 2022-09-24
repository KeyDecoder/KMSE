using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

public abstract class Z8016BitGeneralPurposeRegisterBaseTest
{
    private IZ80FlagsManager _flags;

    private IMasterSystemMemory _memory;
    protected Z8016BitGeneralPurposeRegisterBase Register;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        Register = CreateRegister(_memory, _flags);
    }

    protected abstract Z8016BitGeneralPurposeRegisterBase CreateRegister(IMasterSystemMemory memory,
        IZ80FlagsManager flags);

    [Test]
    public void WhenSettingValueThenValueIsUpdated()
    {
        Register.Set(0x0522);
        Register.Value.Should().Be(0x0522);
    }

    [Test]
    public void WhenSettingValueFromRegisterThenValueIsUpdated()
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x0412);
        Register.Set(register);
        Register.Value.Should().Be(0x0412);
    }

    [Test]
    public void WhenGetValueAsUnsigned16BitValue()
    {
        Register.Set(0x0522);
        var value = Register.AsUnsigned16BitValue();
        value.Word.Should().Be(0x0522);
        value.Low.Should().Be(0x22);
        value.High.Should().Be(0x05);
    }

    [Test]
    public void WhenSettingValueFromDataInMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x1235].Returns((byte)0x34);
        _memory[0x1236].Returns((byte)0x12);
        Register.Set(0x0011);
        Register.SetFromDataInMemory(0x1234, 1);
        Register.Value.Should().Be(0x1234);
    }

    [Test]
    public void WhenSettingValueFrom16RegisterPointerToMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x2233].Returns((byte)0x13);
        _memory[0x2234].Returns((byte)0x03);
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x2231);

        Register.Set(0x0511);
        Register.SetFromDataInMemory(register, 2);
        Register.Value.Should().Be(0x0313);
    }

    [Test]
    public void WhenSavingToMemoryLocationByAddressThenMemoryIsWritten()
    {
        Register.Set(0x1234);
        Register.SaveToMemory(0x1235, 5);
        _memory.Received()[0x123A] = 0x34;
        _memory.Received()[0x123B] = 0x12;
    }

    [Test]
    public void WhenResettingRegisterThenValueIsZero()
    {
        Register.Set(0x3457);
        Register.SwapWithShadow();
        Register.Set(0x1234);
        Register.Reset();
        Register.Value.Should().Be(0x0000);
        Register.ShadowValue.Should().Be(0x0000);
    }

    [Test]
    public void WhenSwappingWithShadowRegister()
    {
        Register.Set(0x3457);
        Register.SwapWithShadow();
        Register.Set(0x1234);
        Register.Value.Should().Be(0x1234);
        Register.ShadowValue.Should().Be(0x3457);
    }

    [Test]
    public void WhenSwappingBackWithShadowRegister()
    {
        Register.Set(0x3457);
        Register.SwapWithShadow();
        Register.Set(0x1234);
        Register.SwapWithShadow();
        Register.Value.Should().Be(0x3457);
        Register.ShadowValue.Should().Be(0x1234);
    }

    [Test]
    public void WhenIncrementingValueThenValueIsIncrementedAndFlagsUnchanged()
    {
        Register.Set(0x1234);
        Register.Increment();
        Register.Value.Should().Be(0x1235);
        _flags.ReceivedCalls().Should().BeEmpty();
    }

    [Test]
    public void WhenDecrementingValueThenValueIsDecrementedAndFlagsUnchanged()
    {
        Register.Set(0x1234);
        Register.Decrement();
        Register.Value.Should().Be(0x1233);
        _flags.ReceivedCalls().Should().BeEmpty();
    }
}