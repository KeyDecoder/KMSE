using FluentAssertions;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.RegisterTests.SpecialPurpose;

public class Z80StackManagementFixture
{
    private ICpuLogger _cpuLogger;
    private IZ80FlagsManager _flags;
    private IMasterSystemMemory _memory;
    private Z80StackManager _stackManager;

    [SetUp]
    public void Setup()
    {
        _memory = Substitute.For<IMasterSystemMemory>();
        _cpuLogger = Substitute.For<ICpuLogger>();
        _flags = Substitute.For<IZ80FlagsManager>();
        _stackManager = new Z80StackManager(_memory, _flags, _cpuLogger);

        _memory.GetMinimumAvailableMemorySize().Returns(100);
        _memory.GetMaximumAvailableMemorySize().Returns(0x5000);
        _stackManager.Reset();
    }

    [Test]
    public void WhenResetThenValueIsTopOfMemoryStack()
    {
        _stackManager.Reset();
        _stackManager.Value.Should().Be(0xDFF0);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(0xDFF0);
    }

    [Test]
    public void WhenSettingStackPointerThenValueIsUpdated()
    {
        _stackManager.Set(0x1235);
        _stackManager.Value.Should().Be(0x1235);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(0x1235);
    }

    [Test]
    public void WhenSettingStackPointerFromDataInMemoryThenValueIsUpdated()
    {
        _memory[0x1122].Returns((byte)0x56);
        _memory[0x1123].Returns((byte)0x27);
        _stackManager.SetFromDataInMemory(0x1122);

        _stackManager.Value.Should().Be(0x2756);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(0x2756);
    }

    [Test]
    public void WhenStackPointerIncrementedThenValueIsUpdated()
    {
        _stackManager.Set(0x1133);
        _stackManager.IncrementStackPointer();
        _stackManager.Value.Should().Be(0x1134);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(0x1134);
    }

    [Test]
    public void WhenStackPointerDecrementedThenValueIsUpdated()
    {
        _stackManager.Set(0x1133);
        _stackManager.DecrementStackPointer();
        _stackManager.Value.Should().Be(0x1132);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(0x1132);
    }

    [Test]
    public void WhenStackPointerIncrementedWhichOverflowsThenWrapsAround()
    {
        _memory.GetMaximumAvailableMemorySize().Returns(1000);
        _stackManager.Reset();
        _stackManager.Set(1000);
        _stackManager.IncrementStackPointer();
        _stackManager.Value.Should().Be(0);
        _cpuLogger.Received(1).Error(Arg.Is<string>(x =>
            x.Contains("Stack Pointer has been incremented", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void WhenStackPointerDecrementedWhichOverflowsThenLogsError()
    {
        _memory.GetMinimumAvailableMemorySize().Returns(100);
        _stackManager.Reset();
        _stackManager.Set(100);
        _stackManager.DecrementStackPointer();
        _stackManager.Value.Should().Be(99);
        _cpuLogger.Received(1).Error(Arg.Is<string>(x =>
            x.Contains("Stack Pointer has been decremented", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Test]
    public void WhenPushingRegisterToStackThenValueIsWrittenToMemoryAtStackPointerAddressAndPointerDecremented()
    {
        var register = new Test16BitClass(_memory, _flags);
        register.Set(0x1234);
        _stackManager.Set(1000);
        _stackManager.PushRegisterToStack(register);

        _memory.Received()[999] = 0x12;
        _memory.Received()[998] = 0x34;

        _stackManager.Value.Should().Be(998);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(998);
    }

    [Test]
    public void WhenPoppingRegisterFromStackThenValueIsReadFromMemoryAndStackPointerIncremented()
    {
        _memory[1000].Returns((byte)0x23);
        _memory[1001].Returns((byte)0x67);

        var register = new Test16BitClass(_memory, _flags);
        register.Set(0x00);
        _stackManager.Set(1000);
        _stackManager.PopRegisterFromStack(register);

        register.Value.Should().Be(0x6723);

        _stackManager.Value.Should().Be(1002);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(1002);
    }

    [Test]
    public void WhenSwappingRegisterWithDataInMemoryAtStackPointerLocationThenValueIsReturnedAndStackPointerUnchanged()
    {
        _memory[1000].Returns((byte)0x23);
        _memory[1001].Returns((byte)0x47);

        var register = new Test16BitClass(_memory, _flags);
        register.Set(0x1234);
        _stackManager.Set(1000);
        _stackManager.SwapRegisterWithDataAtStackPointerAddress(register);

        register.Value.Should().Be(0x4723);

        _memory.Received()[1000] = 0x34;
        _memory.Received()[1001] = 0x12;

        _stackManager.Value.Should().Be(1000);
        _stackManager.AsUnsigned16BitValue().Word.Should().Be(1000);
    }

    private class Test16BitClass : Z8016BitSpecialRegisterBase
    {
        public Test16BitClass(IMasterSystemMemory memory, IZ80FlagsManager flags) : base(memory, flags) { }
    }
}