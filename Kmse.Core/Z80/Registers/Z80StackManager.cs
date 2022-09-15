using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Support;
using System;

namespace Kmse.Core.Z80.Registers;

public class Z80StackManager : IZ80StackManager
{
    private readonly IMasterSystemMemory _memory;
    private readonly ICpuLogger _cpuLogger;
    private Z80Register _stackPointer;
    private int _maximumMemorySize;

    public Z80StackManager(IMasterSystemMemory memory, ICpuLogger cpuLogger)
    {
        _memory = memory;
        _cpuLogger = cpuLogger;
        Reset();
    }

    public void Reset()
    {
        // Stack pointer starts at highest point in RAM
        // but various hardware register control writes, generally we set to 0xDFF0
        // https://www.smspower.org/Development/Stack
        _stackPointer.Word = 0xDFF0;
        _maximumMemorySize = _memory.GetMaximumAvailableMemorySize();
    }

    public ushort GetValue()
    {
        return _stackPointer.Word;
    }

    public Z80Register AsRegister()
    {
        return _stackPointer;
    }

    public void IncrementStackPointer()
    {
        if (_stackPointer.Word >= _maximumMemorySize)
        {
            throw new InvalidOperationException($"Cannot increment Stack Pointer higher than available RAM - {_maximumMemorySize} bytes");
        }

        _stackPointer.Word++;
    }

    public void DecrementStackPointer()
    {
        // We check this each time instead of caching since this can change if RAM banking is enabled
        // Although it is unlikely any ROM is going to use so much stack that it basically uses all the RAM
        if (_stackPointer.Word <= _memory.GetMinimumAvailableMemorySize())
        {
            throw new InvalidOperationException($"Cannot decrement Stack Pointer lower than available RAM - {_memory.GetMinimumAvailableMemorySize()} bytes");
        }
        _stackPointer.Word--;
    }

    public void PushRegisterToStack(Z80Register register)
    {
        var oldPointer = _stackPointer.Word;
        var currentPointer = _stackPointer.Word;
        _memory[--currentPointer] = register.High;
        _memory[--currentPointer] = register.Low;
        _stackPointer.Word = currentPointer;
        _cpuLogger.Debug($"Push to stack - Old - {oldPointer}, New = {_stackPointer.Word}");
    }

    public void PopRegisterFromStack(ref Z80Register register)
    {
        var oldPointer = _stackPointer.Word;
        var currentPointer = _stackPointer.Word;
        register.Low = _memory[currentPointer++];
        register.High = _memory[currentPointer++];
        _stackPointer.Word = currentPointer;
        _cpuLogger.Debug($"Pop from stack - Old - {oldPointer}, New = {_stackPointer.Word}");
    }

    public void SwapRegisterWithStackPointerLocation(ref Z80Register register)
    {
        var currentRegisterDataLow = register.Low;
        var currentRegisterDataHigh = register.High;
        register.Low = _memory[_stackPointer.Word];
        register.High = _memory[(ushort)(_stackPointer.Word + 1)];

        _memory[_stackPointer.Word] = currentRegisterDataLow;
        _memory[(ushort)(_stackPointer.Word + 1)] = currentRegisterDataHigh;
    }

    public void SetStackPointer(ushort value)
    {
        _stackPointer.Word = value;
    }

    public void SetStackPointerFromDataInMemory(ushort memoryLocation)
    {
        _stackPointer.Low = _memory[memoryLocation];
        memoryLocation++;
        _stackPointer.High = _memory[memoryLocation];
    }
}