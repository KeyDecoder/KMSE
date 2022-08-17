using System.Text;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80;

public class Z80Cpu : IZ80Cpu
{
    private readonly ICpuLogger _cpuLogger;
    private int _currentCycleCount;
    private IMasterSystemIoManager _io;
    private IMasterSystemMemory _memory;
    private bool _halted;

    private const int NopCycleCount = 4;

    private Z80Register _af, _bc, _de, _hl;
    private Z80Register _afShadow, _bcShadow, _deShadow, _hlShadow;
    private Z80Register _ix, _iy;
    private Z80Register _pc, _stackPointer;
    private byte _iRegister, _rRegister;

    private bool _interruptFlipFlop1 = false;
    private bool _interruptFlipFlop2 = false;

    private readonly Dictionary<byte, Instruction> _genericInstructions = new();

    // TODO: Can we improve handling of logging of instructions and fetching of memory reads
    private ushort _currentAddress;
    private readonly StringBuilder _currentData = new();

    public Z80Cpu(ICpuLogger cpuLogger)
    {
        _cpuLogger = cpuLogger;
        _pc = new Z80Register();
        _af = new Z80Register();
        _bc = new Z80Register();
        _de = new Z80Register();
        _hl = new Z80Register();
        _afShadow = new Z80Register();
        _bcShadow = new Z80Register();
        _deShadow = new Z80Register();
        _hlShadow = new Z80Register();
        _ix = new Z80Register();
        _iy = new Z80Register();
        _stackPointer = new Z80Register();
        _iRegister = 0;
        _rRegister = 0;
        PopulateInstructions();
    }

    public void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io)
    {
        _memory = memory;
        _io = io;
    }

    public void Reset()
    {
        _currentCycleCount = 0;

        // Reset program counter back to start
        _pc.Word = 0x00;

        _halted = false;
        _interruptFlipFlop1 = false;
        _interruptFlipFlop2 = false;

        _iRegister = 0;
        _rRegister = 0;

        // Stack pointer starts at highest point in RAM
        // but various hardware register control writes, generally we set to 0xDFF0
        // https://www.smspower.org/Development/Stack
        _stackPointer.Word = 0xDFF0;

        _af.Word = 0x00;
        _bc.Word = 0x00;
        _de.Word = 0x00;
        _hl.Word = 0x00;

        _afShadow.Word = 0x00;
        _bcShadow.Word = 0x00;
        _deShadow.Word = 0x00;
        _hlShadow.Word = 0x00;

        _io.ClearNonMaskableInterrupt();
        _io.ClearMaskableInterrupt();
    }

    public int ExecuteNextCycle()
    {
        _currentCycleCount = 0;
        _currentAddress = 0;
        _currentData.Clear();

        if (_io.NonMaskableInterrupt)
        {
            // Handle NMI by jumping to 0x66
            ResetProgramCounter(0x66);

            // If halted then NMI starts it up again
            _halted = false;

            // Clear interrupt since serviced
            _io.ClearNonMaskableInterrupt();
            return _currentCycleCount;
        }

        // TODO: Handle Maskable interrupt

        if (_halted)
        {
            // NOP until interrupt
            return NopCycleCount;
        }

        // https://www.smspower.org/Development/InstructionSet
        var opCode = GetNextOperation();

        // Check for CB, DD, ED, FD instructions and handle differently

        if (!_genericInstructions.TryGetValue(opCode, out var instruction))
        {
            _cpuLogger.LogInstruction(_currentAddress, "Unhandled", "");
            _currentCycleCount += NopCycleCount;
            return _currentCycleCount;
        }

        instruction.Execute();
        _currentCycleCount += instruction.ClockCycles;
        _cpuLogger.LogInstruction(_currentAddress, instruction.Name, "");

        return _currentCycleCount;
    }

    // TODO: Split classes into core management, execution methods and instruction set

    private void PopulateInstructions()
    {
        AddGenericInstruction(0xF3, "DI", "Disable Interrupts", 4, () => {  _interruptFlipFlop1 = false;  _interruptFlipFlop2 = false; });
        AddGenericInstruction(0x00, "NOP", "No Operation", 4, () => { });
        AddGenericInstruction(0x76, "HALT", "Halt", 4, () => { _halted = true; });
    }

    private void AddGenericInstruction(byte opCode, string name, string description, int cycles, Action handleFunc)
    {
        _genericInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc));
    }

    private void SetFlag(Z80StatusFlags flags)
    {
        _af.Low |= (byte)flags;
    }

    private void ClearFlag(Z80StatusFlags flags)
    {
        _af.Low &= (byte)~flags;
    }

    private void SetClearFlagConditional(Z80StatusFlags flags, bool condition)
    {
        if (condition)
        {
            SetFlag(flags);
        }
        else
        {
            ClearFlag(flags);
        }
    }

    private bool IsFlagSet(Z80StatusFlags flags)
    {
        var currentSetFlags = (Z80StatusFlags)_af.Low & flags;
        return currentSetFlags == flags;
    }

    private byte GetNextOperation()
    {
        var data = _memory[_pc.Word];

        if (_currentAddress == 0)
        {
            _currentAddress = _pc.Word;
        }
        else
        {
            // Only set current data if reading additional information beyond command itself
            _currentData.Append($"{data:X2} ");
        }

        _cpuLogger.LogMemoryRead(_pc.Word, data);

        _pc.Word++;
        return data;
    }

    private void ResetProgramCounter(ushort address)
    {
        // Four cycles to fetch current Pc register
        _currentCycleCount += 4;

        // Storing PC in Stack so can resume later
        PushRegisterToStack(_pc);

        // Update PC to execute from new address
        _pc.Word = address;

        // 1 cycle to jump?
        _currentCycleCount += 1;
    }

    private void PushRegisterToStack(Z80Register register)
    {
        var currentPointer = _stackPointer.Word;
        _memory[--currentPointer] = register.High;
        _memory[--currentPointer] = register.Low;
        _stackPointer.Word = currentPointer;

        // 4 cycles to write to memory and 2 cycles total to decrement stack pointer (decremented twice)
        _currentCycleCount += 2 + 4;
    }

    private class Instruction
    {
        public Instruction(byte opCode, string name, string description, int cycles, Action handleMethod)
        {
            OpCode = opCode;
            Name = name;
            Description = description;
            ClockCycles = cycles;
            _handleMethod = handleMethod;
        }

        private readonly Action _handleMethod;

        public byte OpCode { get; }
        public string Name { get; }
        public string Description { get; }
        public int ClockCycles { get; }

        public void Execute()
        {
            _handleMethod();
        }
    }
}