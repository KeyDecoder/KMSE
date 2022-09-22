using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.MemoryTests;

public class Z80CpuMemoryManagementFixture
{
    private IZ80FlagsManager _flags;
    private IMasterSystemMemory _memory;
    private Z80CpuMemoryManagement _memoryManager;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _memoryManager = new Z80CpuMemoryManagement(_memory, _flags);
    }

    [Test]
    public void WhenCopyingMemoryThenMemoryIsCopiedFromSourceToDestination()
    {
        var source = Substitute.For<IZ8016BitRegister>();
        source.Value.Returns((ushort)0x1234);

        var destination = Substitute.For<IZ8016BitRegister>();
        destination.Value.Returns((ushort)0x0102);

        _memory[0x1234].Returns((byte)0x33);

        _memoryManager.CopyMemory(source, destination);

        _memory.Received()[0x0102] = 0x33;
    }

    [Test]
    public void WhenReadFromMemoryWithOffsetThenValueInMemoryAtAddressIsReturned()
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x1234);
        _memory[0x1235].Returns((byte)0x31);

        var value = _memoryManager.ReadFromMemory(register, 1);
        value.Should().Be(0x31);
    }

    [Test]
    public void WhenWriteToMemoryWithOffsetThenValueInMemoryAtAddressIsSet()
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x1122);

        _memoryManager.WriteToMemory(register, 0x53, 5);
        _memory.Received()[0x1127] = 0x53;
    }

    [Test]
    [TestCase(0x01, 0x02, false)]
    [TestCase(0x0F, 0x10, true)]
    [TestCase(0x10, 0x11, false)]
    [TestCase(0xFE, 0xFF, false)]
    [TestCase(0xFF, 0x00, true)]
    public void WhenIncrementDataInMemoryThenValueIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x1122);

        _memory[0x1123] = value;

        _memoryManager.IncrementMemory(register, 1);
        _memory.Received()[0x1123] = expectedValue;

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, halfCarryStatus);
        _flags.Received(1).SetIfIncrementOverflow(value);
        _flags.Received(1).ClearFlag(Z80StatusFlags.AddSubtractN);
    }

    [Test]
    [TestCase(0x02, 0x01, false)]
    [TestCase(0x10, 0x0F, true)]
    [TestCase(0x11, 0x10, false)]
    [TestCase(0xFF, 0xFE, false)]
    [TestCase(0x00, 0xFF, true)]
    public void WhenDecrementDataInMemoryThenValueIncrementedAndFlagsSet(byte value, byte expectedValue,
        bool halfCarryStatus)
    {
        var register = Substitute.For<IZ8016BitRegister>();
        register.Value.Returns((ushort)0x1122);

        _memory[0x1123] = value;

        _memoryManager.DecrementMemory(register, 1);
        _memory.Received()[0x1123] = expectedValue;

        _flags.Received(1).SetIfNegative(expectedValue);
        _flags.Received(1).SetIfZero(expectedValue);
        _flags.Received(1).SetClearFlagConditional(Z80StatusFlags.HalfCarryH, halfCarryStatus);
        _flags.Received(1).SetIfDecrementOverflow(value);
        _flags.Received(1).SetFlag(Z80StatusFlags.AddSubtractN);
    }
}