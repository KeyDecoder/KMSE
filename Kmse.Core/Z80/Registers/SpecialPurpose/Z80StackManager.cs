using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers.General;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public class Z80StackManager : Z8016BitSpecialRegisterBase, IZ80StackManager
{
    private const ushort DefaultStackAddress = 0xDFF0;
    private readonly ICpuLogger _cpuLogger;
    private readonly IMasterSystemMemory _memory;
    private int _maximumMemorySize;

    public Z80StackManager(IMasterSystemMemory memory, ICpuLogger cpuLogger, IZ80FlagsManager flags)
        : base(memory, flags)
    {
        _memory = memory;
        _cpuLogger = cpuLogger;
    }

    public override void Reset()
    {
        // Stack pointer starts at highest point in RAM
        // but various hardware register control writes, generally we set to 0xDFF0
        // https://www.smspower.org/Development/Stack
        Register.Word = DefaultStackAddress;
        _maximumMemorySize = _memory.GetMaximumAvailableMemorySize();
    }

    public void IncrementStackPointer()
    {
        if (Register.Word >= _maximumMemorySize)
        {
            _cpuLogger.Error($"Warning: Stack Pointer has been incremented beyond maximum RAM size and will rollover to 0, current stack pointer '{Register.Word:X4}'");
            Register.Word = 0x00;
            return;
        }

        Register.Word++;
    }

    public void DecrementStackPointer()
    {
        // We check this each time instead of caching since this can change if RAM banking is enabled
        // Although it is unlikely any ROM is going to use so much stack that it basically uses all the RAM
        if (Register.Word <= _memory.GetMinimumAvailableMemorySize())
        {
            // We use this as a warning since zexdoc will decrement this down pass ROM space as part of testing so this may be a legitimate, if unusual, manipulation of the stack
            _cpuLogger.Error($"Warning: Stack Pointer has been decremented into ROM memory slots, current stack pointer '{Register.Word:X4}'");
        }

        Register.Word--;
    }

    public void PushRegisterToStack(IZ8016BitRegister register)
    {
        var oldPointer = Register.Word;
        var currentPointer = Register.Word;
        _memory[--currentPointer] = register.High;
        _memory[--currentPointer] = register.Low;
        Register.Word = currentPointer;
        _cpuLogger.Debug($"Push to stack - Old - {oldPointer}, New = {Register.Word}");
    }

    public void PopRegisterFromStack(IZ8016BitRegister register)
    {
        var oldPointer = Register.Word;
        var currentPointer = Register.Word;
        register.SetLow(_memory[currentPointer++]);
        register.SetHigh(_memory[currentPointer++]);
        Register.Word = currentPointer;
        _cpuLogger.Debug($"Pop from stack - Old - {oldPointer}, New = {Register.Word}");
    }

    public void SwapRegisterWithDataAtStackPointerAddress(IZ8016BitRegister register)
    {
        var currentRegisterDataLow = register.Low;
        var currentRegisterDataHigh = register.High;
        register.SetLow(_memory[Register.Word]);
        register.SetHigh(_memory[(ushort)(Register.Word + 1)]);

        _memory[Register.Word] = currentRegisterDataLow;
        _memory[(ushort)(Register.Word + 1)] = currentRegisterDataHigh;
    }
}