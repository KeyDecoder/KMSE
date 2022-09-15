using Kmse.Core.Z80;
using Kmse.Core.Z80.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests;

public class Z80InstructionLoggerFixture
{
    private const byte TestInstructionAddress = 0x03;
    private const string OpCode = "03";
    private const string Name = "Test instruction";
    private const string Description = "Test instruction for testing";
    private ICpuLogger _cpuLogger;
    private Z80InstructionLogger _instructionLogger;


    [SetUp]
    public void Setup()
    {
        _cpuLogger = Substitute.For<ICpuLogger>();
        _instructionLogger = new Z80InstructionLogger(_cpuLogger);
    }

    [Test]
    public void WhenStartingANewInstructionAllDataIsClearedExceptPc()
    {
        _instructionLogger.StartNewInstruction(0x05);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(0x05, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    private void AddSimpleInstruction()
    {
        _instructionLogger.StartNewInstruction(TestInstructionAddress);
        _instructionLogger.SetOpCode(OpCode, Name, Description);
    }

    [Test]
    public void WhenSettingOpCodeItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, string.Empty);
    }

    [Test]
    public void WhenAddingByteDataItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.AddOperationData(0x4B);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, "4B");
    }

    [Test]
    public void WhenAddingUshortDataItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.AddOperationData(0x1234);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, "1234");
    }

    [Test]
    public void WhenAddingMultipleByteDataItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.AddOperationData(0x4B);
        _instructionLogger.AddOperationData(0x7A);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, "4B7A");
    }

    [Test]
    public void WhenAddingMultipleUshortDataItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.AddOperationData(0x1234);
        _instructionLogger.AddOperationData(0x4321);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, "12344321");
    }

    [Test]
    public void WhenAddingByteAndUshortDataItIsIncludedInLog()
    {
        AddSimpleInstruction();
        _instructionLogger.AddOperationData(0x12);
        _instructionLogger.AddOperationData(0x3456);
        _instructionLogger.Log();
        _cpuLogger.Received(1).LogInstruction(TestInstructionAddress, OpCode, Name, Description, "123456");
    }
}