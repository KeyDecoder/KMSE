using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.GeneralPurpose;

[TestFixture]
public class Z808BitRegisterFixture
{
    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _register = new TestZ808BitRegister(_memory, _flags);
    }

    private IMasterSystemMemory _memory;
    private IZ80FlagsManager _flags;
    private TestZ808BitRegister _register;

    [Test]
    public void WhenSettingValueThenValueIsUpdated()
    {
        _register.Set(0x05);
        _register.Value.Should().Be(0x05);
    }

    [Test]
    public void WhenSettingValueFromRegisterThenValueIsUpdated()
    {
        var register = Substitute.For<IZ808BitRegister>();
        register.Value.Returns((byte)0x04);
        _register.Set(register);
        _register.Value.Should().Be(0x04);
    }

    [Test]
    public void WhenSettingValueFromDataInMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x1235].Returns((byte)0x12);
        _register.Set(0x05);
        _register.SetFromDataInMemory(0x1234, 1);
        _register.Value.Should().Be(0x12);
    }

    [Test]
    public void WhenSettingValueFrom16RegisterPointerToMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x2233].Returns((byte)0x13);
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x2231);

        _register.Set(0x05);
        _register.SetFromDataInMemory(register, 2);
        _register.Value.Should().Be(0x13);
    }

    [Test]
    public void WhenSavingToMemoryLocationByAddressThenMemoryIsWritten()
    {
        _register.Set(0x23);
        _register.SaveToMemory(0x1235, 5);
        _memory.Received()[0x123A] = 0x23;
    }

    [Test]
    public void WhenSavingToMemoryLocationBy16BitRegisterAddressThenMemoryIsWritten()
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x2231);

        _register.Set(0x43);
        _register.SaveToMemory(register, 2);
        _memory.Received()[0x2233] = 0x43;
    }

    [Test]
    public void WhenResettingRegisterThenValueIsZero()
    {
        _register.Set(0x05);
        _register.SwapWithShadow();
        _register.Set(0x01);
        _register.Reset();
        _register.Value.Should().Be(0x00);
        _register.ShadowValue.Should().Be(0x00);
    }

    [Test]
    public void WhenSwappingValueWithShadow()
    {
        _register.Set(0x05);
        _register.SwapWithShadow();
        _register.Value.Should().Be(0x00);
        _register.ShadowValue.Should().Be(0x05);
    }

    [Test]
    public void WhenSwappingValueBackFromShadow()
    {
        _register.Set(0x12);
        _register.SwapWithShadow();
        _register.Set(0x33);
        _register.SwapWithShadow();

        _register.Value.Should().Be(0x12);
        _register.ShadowValue.Should().Be(0x33);
    }
}