using FluentAssertions;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

[TestFixture]
public class CpuInstructionsFixture
{
    [SetUp]
    public void Setup()
    {
        _cpuLogger = Substitute.For<ICpuLogger>();
        _memory = Substitute.For<IMasterSystemMemory>();
        _io = Substitute.For<IMasterSystemIoManager>();
        _cpu = new Z80Cpu(_cpuLogger);
        _cpu.Initialize(_memory, _io);
    }

    private void PrepareForTest()
    {
        _cpu.Reset();
        _memory.ClearReceivedCalls();
        _io.ClearReceivedCalls();
        _io.NonMaskableInterrupt.Returns(false);
        _io.MaskableInterrupt.Returns(false);
    }

    private Z80Cpu _cpu;
    private ICpuLogger _cpuLogger;
    private IMasterSystemMemory _memory;
    private IMasterSystemIoManager _io;

    private static object[] _jrTestCases =
    {
        // This is 500 + 2 + offset (the 2 is for the instruction)
        new object[] { (byte)0, (ushort)502 },
        new object[] { (byte)1, (ushort)503 },
        new object[] { (byte)2, (ushort)504 },
        new object[] { (byte)3, (ushort)505 },
        new object[] { (byte)129, (ushort)631 },
        // Since 130 is actually -126 here, we go backwards
        new object[] { (byte)130, (ushort)376 },
        // Since 255 is actually -1 here, we go backwards by 1
        new object[] { (byte)255, (ushort) 501 },
    };

    [Test]
    [TestCaseSource(nameof(_jrTestCases))]
    public void WhenExecutingRelativeJump(byte offset, ushort expectedPc)
    {
        PrepareForTest();

        // Execute at least 500 instructions
        for (var i = 0; i < 500; i++)
        {
            _memory[(ushort)i].Returns((byte)0x00);
            _cpu.ExecuteNextCycle().Should().Be(4);
        }

        _memory[500].Returns((byte)0x18);
        _memory[501].Returns(offset);
        _cpu.ExecuteNextCycle().Should().Be(12);

        var status = _cpu.GetStatus();
        status.Pc.Word.Should().Be(expectedPc);
    }
}