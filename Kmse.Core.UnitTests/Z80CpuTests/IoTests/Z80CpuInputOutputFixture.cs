using FluentAssertions;
using Kmse.Core.IO;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.IoTests;

public class Z80CpuInputOutputFixture
{
    private Z80CpuInputOutputManager _cpuIoManager;
    private IZ80FlagsManager _flags;
    private IMasterSystemIoManager _io;

    [SetUp]
    public void Setup()
    {
        _io = Substitute.For<IMasterSystemIoManager>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _cpuIoManager = new Z80CpuInputOutputManager(_io, _flags);
    }

    [Test]
    public void WhenReadFromIoWithoutFlagsThenValueReturnedAndNoFlagsChanged()
    {
        _io.ReadPort(0x1234).Returns((byte)0x21);
        var value = _cpuIoManager.Read(0x12, 0x34, false);
        value.Should().Be(0x21);
    }

    [Test]
    public void WhenReadFromIoWithFlagsThenFlagsAreUpdated()
    {
        const byte expectedValue = 0x81;
        _io.ReadPort(0x1234).Returns(expectedValue);
        var value = _cpuIoManager.Read(0x12, 0x34, true);

        value.Should().Be(expectedValue);
        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    public void WhenReadFromIoAndSet8BitRegisterThen8BitRegisterIsSetWithValue()
    {
        const byte expectedValue = 0x81;
        _io.ReadPort(0x1234).Returns(expectedValue);
        var register = Substitute.For<IZ808BitRegister>();
        _cpuIoManager.ReadAndSetRegister(0x12, 0x34, register);

        register.Received(1).Set(expectedValue);
        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    public void WhenReadFromIoAndSetUsing16BitRegisterThen8BitRegisterIsSetWithValue()
    {
        const byte expectedValue = 0x81;
        _io.ReadPort(0x1234).Returns(expectedValue);
        var destinationRegister = Substitute.For<IZ808BitRegister>();
        var sourceRegister = Substitute.For<IZ8016BitRegister>();
        sourceRegister.High.Returns((byte)0x12);
        sourceRegister.Low.Returns((byte)0x34);
        _cpuIoManager.ReadAndSetRegister(sourceRegister, destinationRegister);

        destinationRegister.Received(1).Set(expectedValue);
        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.HalfCarryH);
        _flags.Received(1).SetParityFromValue(expectedValue);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
    }


    [Test]
    public void WhenWriteToIoThenIoPortIsWrittenTo()
    {
        var register = Substitute.For<IZ808BitRegister>();
        register.Value.Returns((byte)0x23);
        _cpuIoManager.Write(0x22, 0x12, register);

        _io.Received(1).WritePort(0x2212, 0x23);
    }
}