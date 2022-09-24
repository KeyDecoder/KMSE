using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

[TestFixture]
public class Z8016BitSpecialRegisterFixture
{
    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _register = new TestZ8016BitSpecialRegisterBase(_memory, _flags);
    }

    private IMasterSystemMemory _memory;
    private IZ80FlagsManager _flags;
    private TestZ8016BitSpecialRegisterBase _register;

    [Test]
    public void WhenSettingValueThenValueIsUpdated()
    {
        _register.Set(0x0522);
        _register.Value.Should().Be(0x0522);
    }

    [Test]
    public void WhenSettingValueFromRegisterThenValueIsUpdated()
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x0412);
        _register.Set(register);
        _register.Value.Should().Be(0x0412);
    }

    [Test]
    public void WhenGetValueAsUnsigned16BitValue()
    {
        _register.Set(0x0522);
        var value = _register.AsUnsigned16BitValue();
        value.Word.Should().Be(0x0522);
        value.Low.Should().Be(0x22);
        value.High.Should().Be(0x05);
    }

    [Test]
    public void WhenSettingValueFromDataInMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x1235].Returns((byte)0x34);
        _memory[0x1236].Returns((byte)0x12);
        _register.Set(0x0011);
        _register.SetFromDataInMemory(0x1234, 1);
        _register.Value.Should().Be(0x1234);
    }

    [Test]
    public void WhenSettingValueFrom16RegisterPointerToMemoryThenValueIsUpdatedFromDataAtMemoryAddress()
    {
        _memory[0x2233].Returns((byte)0x13);
        _memory[0x2234].Returns((byte)0x03);
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x2231);

        _register.Set(0x0511);
        _register.SetFromDataInMemory(register, 2);
        _register.Value.Should().Be(0x0313);
    }

    [Test]
    public void WhenSavingToMemoryLocationByAddressThenMemoryIsWritten()
    {
        _register.Set(0x1234);
        _register.SaveToMemory(0x1235, 5);
        _memory.Received()[0x123A] = 0x34;
        _memory.Received()[0x123B] = 0x12;
    }

    [Test]
    public void WhenResettingRegisterThenValueIsZero()
    {
        _register.Set(0x3457);
        _register.Reset();
        _register.Value.Should().Be(0x0000);
    }

    [Test]
    public void WhenIncrementingValueThenValueIsIncrementedAndFlagsUnchanged()
    {
        _register.Set(0x1234);
        _register.Increment();
        _register.Value.Should().Be(0x1235);
        _flags.ReceivedCalls().Should().BeEmpty();
    }

    [Test]
    public void WhenDecrementingValueThenValueIsDecrementedAndFlagsUnchanged()
    {
        _register.Set(0x1234);
        _register.Decrement();
        _register.Value.Should().Be(0x1233);
        _flags.ReceivedCalls().Should().BeEmpty();
    }
}