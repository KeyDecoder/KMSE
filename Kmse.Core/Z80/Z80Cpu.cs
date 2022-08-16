using System.Runtime.InteropServices;
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

    private Z80Register _pc;
    private Z80Register _stackPointer;
    private bool _interruptFlipFlop1 = false;
    private bool _interruptFlipFlop2 = false;

    private ushort _currentAddress;
    private readonly StringBuilder _currentData = new StringBuilder();

    public Z80Cpu(ICpuLogger cpuLogger)
    {
        _cpuLogger = cpuLogger;
        _pc = new Z80Register();
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

        var operation = GetNextOperation();
        switch (operation)
        {
            case 0xF3:
                DI();
                break;
            default:
                _currentCycleCount += NopCycleCount;
                break;
        }
        return _currentCycleCount;
    }

    private void DI()
    {
        _cpuLogger.LogInstruction(_currentAddress, "DI", _currentData.ToString().TrimEnd());
        _interruptFlipFlop1 = false;
        _interruptFlipFlop1 = false;
        _currentCycleCount += 4;
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
}