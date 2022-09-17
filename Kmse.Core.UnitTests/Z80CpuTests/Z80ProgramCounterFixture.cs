using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

public class Z80ProgramCounterFixture
{
    private IZ80FlagsManager _flags;
    private IZ80InstructionLogger _instructionLogger;
    private IMasterSystemMemory _memory;
    private Z80ProgramCounter _programCounter;
    private IZ80StackManager _stack;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _instructionLogger = Substitute.For<IZ80InstructionLogger>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _stack = Substitute.For<IZ80StackManager>();
        _programCounter = new Z80ProgramCounter(_memory, _instructionLogger, _flags, _stack);
    }

    [Test]
    public void WhenResetThenValueIsZero()
    {
        _programCounter.Reset();
        _programCounter.Value.Should().Be(0);
        _programCounter.AsRegister().Word.Should().Be(0);
    }

    [Test]
    public void WhenSettingProgramCounterThenValueIsUpdated()
    {
        _programCounter.Set(0x1234);
        _programCounter.Value.Should().Be(0x1234);
        _programCounter.AsRegister().Word.Should().Be(0x1234);
    }

    [Test]
    public void WhenProgramCounterMovedForwardOffsetWhichOverflowsThenWrapsAround()
    {
        _programCounter.Set(ushort.MaxValue);
        _programCounter.MoveProgramCounterForward(3);
        _programCounter.Value.Should().Be(0x02);
        _programCounter.AsRegister().Word.Should().Be(0x02);
    }

    [Test]
    public void WhenProgramCounterMovedForwardByOffsetThenValueIsUpdated()
    {
        _programCounter.Set(0x1234);
        _programCounter.MoveProgramCounterForward(10);
        _programCounter.Value.Should().Be(0x123E);
        _programCounter.AsRegister().Word.Should().Be(0x123E);
    }

    [Test]
    public void WhenProgramCounterMovedBackwardByOffsetThenValueIsUpdated()
    {
        _programCounter.Set(0x1234);
        _programCounter.MoveProgramCounterBackward(10);
        _programCounter.Value.Should().Be(0x122A);
        _programCounter.AsRegister().Word.Should().Be(0x122A);
    }

    [Test]
    public void WhenProgramCounterMovedBackwardByOffsetWhichOverflowsThenWrapsAround()
    {
        _programCounter.Set(0);
        _programCounter.MoveProgramCounterBackward(3);
        _programCounter.Value.Should().Be(ushort.MaxValue - 2);
        _programCounter.AsRegister().Word.Should().Be(ushort.MaxValue - 2);
    }

    [Test]
    public void WhenGettingNextDataByteFromMemoryUsingPcValueTheCorrectDataIsReturnedAndPcIncremented()
    {
        _memory[0x05].Returns((byte)0x12);
        _programCounter.Set(0x05);
        var data = _programCounter.GetNextDataByte();
        data.Should().Be(0x12);

        _programCounter.Value.Should().Be(0x06);
        _programCounter.AsRegister().Word.Should().Be(0x06);

        _instructionLogger.Received(1).AddOperationData(0x12);
    }

    [Test]
    public void WhenGettingNextTwoByteDataFromMemoryUsingPcValueTheCorrectDataIsReturnedAndPcIncrementedByTwo()
    {
        _memory[0x05].Returns((byte)0x12);
        _memory[0x06].Returns((byte)0x34);
        _programCounter.Set(0x05);
        var data = _programCounter.GetNextTwoDataBytes();
        // Loading byte order is low and then high byte hence swapped order
        data.Should().Be(0x3412);

        _programCounter.Value.Should().Be(0x07);
        _programCounter.AsRegister().Word.Should().Be(0x07);

        _instructionLogger.Received(1).AddOperationData(0x3412);
    }
}