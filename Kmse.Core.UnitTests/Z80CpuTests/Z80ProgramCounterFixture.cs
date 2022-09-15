using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

public class Z80ProgramCounterFixture
{
    private IZ80InstructionLogger _instructionLogger;
    private Z80ProgramCounter _programCounter;
    private IMasterSystemMemory _memory;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _instructionLogger = Substitute.For<IZ80InstructionLogger>();
        _programCounter = new Z80ProgramCounter(_memory, _instructionLogger);
    }

    [Test]
    public void WhenResetThenValueIsZero()
    {
        _programCounter.Reset();
        _programCounter.GetValue().Should().Be(0);
        _programCounter.AsRegister().Word.Should().Be(0);
    }

    [Test]
    public void WhenSettingProgramCounterThenValueIsUpdated()
    {
        _programCounter.SetProgramCounter(0x1234);
        _programCounter.GetValue().Should().Be(0x1234);
        _programCounter.AsRegister().Word.Should().Be(0x1234);
    }

    [Test]
    public void WhenProgramCounterMovedForwardOffsetWhichOverflowsThenWrapsAround()
    {
        _programCounter.SetProgramCounter(ushort.MaxValue);
        _programCounter.MoveProgramCounterForward(3);
        _programCounter.GetValue().Should().Be(0x02);
        _programCounter.AsRegister().Word.Should().Be(0x02);
    }

    [Test]
    public void WhenProgramCounterMovedForwardByOffsetThenValueIsUpdated()
    {
        _programCounter.SetProgramCounter(0x1234);
        _programCounter.MoveProgramCounterForward(10);
        _programCounter.GetValue().Should().Be(0x123E);
        _programCounter.AsRegister().Word.Should().Be(0x123E);
    }

    [Test]
    public void WhenProgramCounterMovedBackwardByOffsetThenValueIsUpdated()
    {
        _programCounter.SetProgramCounter(0x1234);
        _programCounter.MoveProgramCounterBackward(10);
        _programCounter.GetValue().Should().Be(0x122A);
        _programCounter.AsRegister().Word.Should().Be(0x122A);
    }

    [Test]
    public void WhenProgramCounterMovedBackwardByOffsetWhichOverflowsThenWrapsAround()
    {
        _programCounter.SetProgramCounter(0);
        _programCounter.MoveProgramCounterBackward(3);
        _programCounter.GetValue().Should().Be(ushort.MaxValue-2);
        _programCounter.AsRegister().Word.Should().Be(ushort.MaxValue - 2);
    }

    [Test]
    public void WhenGettingNextDataByteFromMemoryUsingPcValueTheCorrectDataIsReturnedAndPcIncremented()
    {
        _memory[0x05].Returns((byte)0x12);
        _programCounter.SetProgramCounter(0x05);
        var data = _programCounter.GetNextDataByte();
        data.Should().Be(0x12);

        _programCounter.GetValue().Should().Be(0x06);
        _programCounter.AsRegister().Word.Should().Be(0x06);

        _instructionLogger.Received(1).AddOperationData(0x12);
    }

    [Test]
    public void WhenGettingNextTwoByteDataFromMemoryUsingPcValueTheCorrectDataIsReturnedAndPcIncrementedByTwo()
    {
        _memory[0x05].Returns((byte)0x12);
        _memory[0x06].Returns((byte)0x34);
        _programCounter.SetProgramCounter(0x05);
        var data = _programCounter.GetNextTwoDataBytes();
        // Loading byte order is low and then high byte hence swapped order
        data.Should().Be(0x3412);

        _programCounter.GetValue().Should().Be(0x07);
        _programCounter.AsRegister().Word.Should().Be(0x07);

        _instructionLogger.Received(1).AddOperationData(0x3412);
    }
}