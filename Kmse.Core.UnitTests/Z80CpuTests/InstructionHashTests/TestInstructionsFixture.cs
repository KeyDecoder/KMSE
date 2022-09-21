using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Model;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Running;
using NSubstitute;
using NUnit.Framework;

namespace Kmse.Core.UnitTests.Z80CpuTests.InstructionHashTests;

/// <summary>
/// Test all the instructions by executing each possible instruction and validating that a hash of the current CPU, IO and Memory state matches expected
/// This follows a very similar concept to zexall where we take a known good execution and generate test data from that including hashes of state
/// We save that to a JSON file and run this as a unit test to validate no regressions in any instruction implementations
/// This does not cover tests for register or other support classes but primarily ensure that the mapping of instruction op codes to operations is maintained and not broken
/// These tests are really designed to stop new regressions more than testing the current implementation and this relies on testing with zexdoc and other unit tests to validate the implementation is correct
/// So it is important to remember that if any instruction is broken when the hashes are generated, it will then be testing against a incorrect implementation
/// so care must be taken that when tests fail, it may be because an issue is actually fixed and the hashes need to be regenerated
/// </summary>
[TestFixture]
public class TestInstructionsFixture
{
    private Z80Cpu _cpu;
    private ICpuLogger _cpuLogger;
    private TestMemory _memory;
    private TestIo _io;

    [SetUp]
    public void Setup()
    {
        _cpuLogger = Substitute.For<ICpuLogger>();
        _memory = new TestMemory();
        _io = new TestIo();

        var instructionLogger = new Z80InstructionLogger(_cpuLogger);

        var flags = new Z80FlagsManager();
        var accumulator = new Z80Accumulator(flags, _memory);
        var af = new Z80AfRegister(_memory, flags, accumulator);
        var bc = new Z80BcRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var de = new Z80DeRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var hl = new Z80HlRegister(_memory, af.Flags, () => new Z808BitGeneralPurposeRegister(_memory, flags));
        var ix = new Z80IndexRegisterXy(_memory, af.Flags);
        var iy = new Z80IndexRegisterXy(_memory, af.Flags);
        var rRegister = new Z80MemoryRefreshRegister(_memory, af.Flags);
        var iRegister = new Z80InterruptPageAddressRegister(_memory, af.Flags);

        var stack = new Z80StackManager(_memory, af.Flags, _cpuLogger);
        var pc = new Z80ProgramCounter(_memory, af.Flags, stack, instructionLogger);

        var ioManagement = new Z80CpuInputOutput(_io, af.Flags);
        var memoryManagement = new Z80CpuMemoryManagement(_memory, af.Flags);
        var interruptManagement = new Z80InterruptManagement(pc, _cpuLogger);
        var cycleCounter = new Z80CpuCycleCounter();
        var runningStateManager = new Z80CpuRunningStateManager(_cpuLogger);

        var registers = new Z80CpuRegisters
        {
            Pc = pc,
            Stack = stack,
            Af = af,
            Bc = bc,
            De = de,
            Hl = hl,
            IX = ix,
            IY = iy,
            R = rRegister,
            I = iRegister
        };
        var cpuManagement = new Z80CpuManagement
        {
            IoManagement = ioManagement,
            InterruptManagement = interruptManagement,
            MemoryManagement = memoryManagement,
            CycleCounter = cycleCounter,
            RunningStateManager = runningStateManager
        };

        var cpuInstructions = new Z80CpuInstructions(_memory, _io, _cpuLogger, registers, cpuManagement);
        _cpu = new Z80Cpu(_memory, _io, _cpuLogger, instructionLogger, cpuInstructions, registers, cpuManagement);
    }

    protected static T DeserializeTestFile<T>(string filename)
        where T : class
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(filename);
        if (stream != null)
        {
            return JsonSerializer.Deserialize<T>(stream);
        }

        throw new ArgumentException($"Test file {filename} not found");
    }

    private class TestInstructionData
    {
        public string Name { get; set; }
        public string HexInstructions { get; set; }
        public string CpuHash { get; set; }
        public string IoHash { get; set; }
        public string MemoryHash { get; set; }
    }

    private class InstructionTestCases : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            var testInstructions = DeserializeTestFile<IEnumerable<TestInstructionData>>("Kmse.Core.UnitTests.Z80CpuTests.InstructionHashTests.Data.TestInstructions.json");
            foreach (var instruction in testInstructions)
            {
                var instructions = instruction.HexInstructions.Split(',').Select(x => byte.Parse(x, NumberStyles.AllowHexSpecifier)).ToArray();
                yield return new object[] { instruction.Name, instructions, instruction.CpuHash, instruction.IoHash, instruction.MemoryHash };
            }
        }
    }

    [Test]
    [TestCaseSource(typeof(InstructionTestCases))]
    public void ValidateInstructionsAgainstPrecalculatedHashes(string name, byte[] instructions, string expectedCpuHash, string expectedIoHash, string expectedMemoryHash)
    {
        _cpu.Reset();
        _memory.Reset();
        _io.Reset();

        // Load some data into the registers - a,b,c,d,e,h,l
        _memory.AddInstruction(new[] { (byte)0x3E, (byte)0x01 });
        _memory.AddInstruction(new[] { (byte)0x06, (byte)0x02 });
        _memory.AddInstruction(new[] { (byte)0x0E, (byte)0x03 });
        _memory.AddInstruction(new[] { (byte)0x16, (byte)0x04 });
        _memory.AddInstruction(new[] { (byte)0x1E, (byte)0x05 });
        _memory.AddInstruction(new[] { (byte)0x26, (byte)0x06 });
        _memory.AddInstruction(new[] { (byte)0x2E, (byte)0x07 });
        // 16 bit registers sp, ix, iy
        // Note we don't set the stack pointer since already defaults to a sensible value
        _memory.AddInstruction(new[] { (byte)0xDD, (byte)0x21, (byte)0x08, (byte)0x01 });
        _memory.AddInstruction(new[] { (byte)0xFD, (byte)0x21, (byte)0x09, (byte)0x02 });

        _memory.AddInstruction(instructions);
        for (var i = 0; i < _memory.InstructionCount; i++)
        {
            _cpu.ExecuteNextCycle();
        }

        var status = _cpu.GetStatus();
        var cpuHash = HashCpuStatus(status);
        var ioHash = _io.GetHash();
        var memoryHash = _memory.GetHash();

        cpuHash.Should().Be(expectedCpuHash);
        ioHash.Should().Be(expectedIoHash);
        memoryHash.Should().Be(expectedMemoryHash);
    }

    // Uncomment to regenerate hashes from known good instructions
    // Note this requires the instruction lists and associated classes to be made public
    //[Test]
    //public void GenerateTestData()
    //{
    //    // Generate a list of instruction bytes
    //    var instructions = new List<TestInstructionData>();
    //    instructions.AddRange(_cpu._genericInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._cbInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"CB,{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._ddInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"DD,{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._edInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"ED,{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._fdInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"FD,{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._specialDdcbInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"DD,CB,12,{instruction.Key:X2}" }));
    //    instructions.AddRange(_cpu._specialFdcbInstructions.Select(instruction => new TestInstructionData { Name = instruction.Value.Name, HexInstructions = $"FD,CB,13,{instruction.Key:X2}" }));

    //    foreach (var instruction in instructions)
    //    {
    //        _cpu.Reset();
    //        _memory.Reset();
    //        _io.Reset();

    //        //AddStandardInstruction(0x3E, 7, "LD A,N", "Load n into A", _ => { LoadValueInto8BitRegister(ref _af.High, GetNextDataByte()); });
    //        //AddStandardInstruction(0x06, 7, "LD B,N", "Load n into B", _ => { LoadValueInto8BitRegister(ref _bc.High, GetNextDataByte()); });
    //        //AddStandardInstruction(0x0E, 7, "LD C,N", "Load n into C", _ => { LoadValueInto8BitRegister(ref _bc.Low, GetNextDataByte()); });
    //        //AddStandardInstruction(0x16, 7, "LD D,N", "Load n into D", _ => { LoadValueInto8BitRegister(ref _de.High, GetNextDataByte()); });
    //        //AddStandardInstruction(0x1E, 7, "LD E,N", "Load n into E", _ => { LoadValueInto8BitRegister(ref _de.Low, GetNextDataByte()); });
    //        //AddStandardInstruction(0x26, 7, "LD H,N", "Load n into H", _ => { LoadValueInto8BitRegister(ref _hl.High, GetNextDataByte()); });
    //        //AddStandardInstruction(0x2E, 7, "LD L,N", "Load n into L", _ => { LoadValueInto8BitRegister(ref _hl.Low, GetNextDataByte()); });
    //        //AddStandardInstruction(0x01, 10, "LD BC,NN", "Load nn value into BC", _ => { LoadValueInto16BitRegister(ref _bc, GetNextTwoDataBytes()); });
    //        //AddStandardInstruction(0x11, 10, "LD DE,NN", "Load nn value into DE", _ => { LoadValueInto16BitRegister(ref _de, GetNextTwoDataBytes()); });
    //        //AddStandardInstruction(0x21, 10, "LD HL,NN", "Load nn value into HL", _ => { LoadValueInto16BitRegister(ref _hl, GetNextTwoDataBytes()); });
    //        //AddStandardInstruction(0x31, 10, "LD SP,NN", "Load nn value into SP", _ => { LoadValueInto16BitRegister(ref _stackPointer, GetNextTwoDataBytes()); });
    //        //AddDoubleByteInstruction(0xDD, 0x21, 14, "LD IX, NN", "Load nn value into IX", _ => { LoadValueInto16BitRegister(ref _ix, GetNextTwoDataBytes()); });
    //        //AddDoubleByteInstruction(0xFD, 0x21, 14, "LD IY, NN", "Load nn value into IY", _ => { LoadValueInto16BitRegister(ref _iy, GetNextTwoDataBytes()); });

    //        // Load some data into the registers - a,b,c,d,e,h,l
    //        _memory.AddInstruction(new[] { (byte)0x3E, (byte)0x01 });
    //        _memory.AddInstruction(new[] { (byte)0x06, (byte)0x02 });
    //        _memory.AddInstruction(new[] { (byte)0x0E, (byte)0x03 });
    //        _memory.AddInstruction(new[] { (byte)0x16, (byte)0x04 });
    //        _memory.AddInstruction(new[] { (byte)0x1E, (byte)0x05 });
    //        _memory.AddInstruction(new[] { (byte)0x26, (byte)0x06 });
    //        _memory.AddInstruction(new[] { (byte)0x2E, (byte)0x07 });
    //        // 16 bit registers sp, ix, iy
    //        // Note we don't set the stack pointer since already defaults to a sensible value
    //        _memory.AddInstruction(new[] { (byte)0xDD, (byte)0x21, (byte)0x08 });
    //        _memory.AddInstruction(new[] { (byte)0xFD, (byte)0x21, (byte)0x09 });

    //        _memory.AddInstruction(instruction.HexInstructions.Split(',').Select(x => byte.Parse(x, NumberStyles.AllowHexSpecifier)).ToArray());
    //        for (var i = 0; i < _memory.InstructionCount; i++)
    //        {
    //            _cpu.ExecuteNextCycle();
    //        }

    //        var status = _cpu.GetStatus();
    //        var cpuHash = HashCpuStatus(status);
    //        var ioHash = _io.GetHash();
    //        var memoryHash = _memory.GetHash();

    //        instruction.CpuHash = cpuHash;
    //        instruction.IoHash = ioHash;
    //        instruction.MemoryHash = memoryHash;
    //    }

    //    var json = JsonSerializer.Serialize(instructions);
    //}

    private string HashCpuStatus(CpuStatus status)
    {
        using var sha256Hash = SHA256.Create();

        var sb = new StringBuilder();
        sb.Append(status.CurrentCycleCount.ToString());
        sb.Append(status.Halted.ToString());
        sb.Append(status.Af.Word.ToString());
        sb.Append(status.Bc.Word.ToString());
        sb.Append(status.De.Word.ToString());
        sb.Append(status.Hl.Word.ToString());
        sb.Append(status.AfShadow.Word.ToString());
        sb.Append(status.BcShadow.Word.ToString());
        sb.Append(status.DeShadow.Word.ToString());
        sb.Append(status.HlShadow.Word.ToString());
        sb.Append(status.Ix.Word.ToString());
        sb.Append(status.Iy.Word.ToString());
        sb.Append(status.Pc.ToString());
        sb.Append(status.StackPointer.ToString());
        sb.Append(status.IRegister.ToString());
        sb.Append(status.RRegister.ToString());
        sb.Append(status.InterruptFlipFlop1.ToString());

        sb.Append(status.InterruptFlipFlop2.ToString());
        sb.Append(status.InterruptMode.ToString());

        sb.Append(status.NonMaskableInterruptStatus.ToString());
        sb.Append(status.MaskableInterruptStatus.ToString());

        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));

        return BitConverter.ToString(bytes).Replace("-", "");
    }
}