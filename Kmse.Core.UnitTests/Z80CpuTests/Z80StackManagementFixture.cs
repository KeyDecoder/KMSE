using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Support;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

public class Z80StackManagementFixture
{
    private ICpuLogger _cpuLogger;
    private IMasterSystemMemory _memory;
    private Z80StackManager _stackManager;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _cpuLogger = Substitute.For<ICpuLogger>();
        _stackManager = new Z80StackManager(_memory, _cpuLogger);

        _memory.GetMinimumAvailableMemorySize().Returns(100);
        _memory.GetMaximumAvailableMemorySize().Returns(0x5000);
        _stackManager.Reset();
    }

    [Test]
    public void WhenResetThenValueIsTopOfMemoryStack()
    {
        _stackManager.Reset();
        _stackManager.GetValue().Should().Be(0xDFF0);
        _stackManager.AsRegister().Word.Should().Be(0xDFF0);
    }

    [Test]
    public void WhenSettingStackPointerThenValueIsUpdated()
    {
        _stackManager.SetStackPointer(0x1235);
        _stackManager.GetValue().Should().Be(0x1235);
        _stackManager.AsRegister().Word.Should().Be(0x1235);
    }

    [Test]
    public void WhenSettingStackPointerFromDataInMemoryThenValueIsUpdated()
    {
        _memory[0x1122].Returns((byte)0x56);
        _memory[0x1123].Returns((byte)0x27);
        _stackManager.SetStackPointerFromDataInMemory(0x1122);

        _stackManager.GetValue().Should().Be(0x2756);
        _stackManager.AsRegister().Word.Should().Be(0x2756);
    }

    [Test]
    public void WhenStackPointerIncrementedThenValueIsUpdated()
    {
        _stackManager.SetStackPointer(0x1133);
        _stackManager.IncrementStackPointer();
        _stackManager.GetValue().Should().Be(0x1134);
        _stackManager.AsRegister().Word.Should().Be(0x1134);
    }

    [Test]
    public void WhenStackPointerDecrementedThenValueIsUpdated()
    {
        _stackManager.SetStackPointer(0x1133);
        _stackManager.DecrementStackPointer();
        _stackManager.GetValue().Should().Be(0x1132);
        _stackManager.AsRegister().Word.Should().Be(0x1132);
    }

    [Test]
    public void WhenStackPointerIncrementedWhichOverflowsThenThrowsException()
    {
        _memory.GetMaximumAvailableMemorySize().Returns(1000);
        _stackManager.Reset();
        _stackManager.SetStackPointer(1000);
        var action = () => _stackManager.IncrementStackPointer();
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void WhenStackPointerDecrementedWhichOverflowsThenThrowsException()
    {
        _memory.GetMinimumAvailableMemorySize().Returns(100);
        _stackManager.Reset();
        _stackManager.SetStackPointer(100);
        var action = () => _stackManager.DecrementStackPointer();
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void WhenPushingRegisterToStackThenValueIsWrittenToMemoryAtStackPointerAddressAndPointerDecremented()
    {
        var register = new Z80Register
        {
            Word = 0x1234
        };
        _stackManager.SetStackPointer(1000);
        _stackManager.PushRegisterToStack(register);

        _memory.Received()[999] = 0x12;
        _memory.Received()[998] = 0x34;

        _stackManager.GetValue().Should().Be(998);
        _stackManager.AsRegister().Word.Should().Be(998);
    }

    [Test]
    public void WhenPoppingRegisterFromStackThenValueIsReadFromMemoryAndStackPointerIncremented()
    {
        _memory[1000].Returns((byte)0x23);
        _memory[1001].Returns((byte)0x67);

        var register = new Z80Register
        {
            Word = 0
        };
        _stackManager.SetStackPointer(1000);
        _stackManager.PopRegisterFromStack(ref register);

        register.Word.Should().Be(0x6723);

        _stackManager.GetValue().Should().Be(1002);
        _stackManager.AsRegister().Word.Should().Be(1002);
    }

    [Test]
    public void WhenSwappingRegisterWithDataInMemoryAtStackPointerLocationThenValueIsReturnedAndStackpointerUnchanged()
    {
        _memory[1000].Returns((byte)0x23);
        _memory[1001].Returns((byte)0x47);

        var register = new Z80Register
        {
            Word = 0x1234
        };
        _stackManager.SetStackPointer(1000);
        _stackManager.SwapRegisterWithStackPointerLocation(ref register);

        register.Word.Should().Be(0x4723);

        _memory.Received()[1000] = 0x34;
        _memory.Received()[1001] = 0x12;

        _stackManager.GetValue().Should().Be(1000);
        _stackManager.AsRegister().Word.Should().Be(1000);
    }
}