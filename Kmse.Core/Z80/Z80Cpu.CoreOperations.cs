using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80
{
    /// <summary>
    /// Core operations, memory operations, reset, flags, stack operations
    /// </summary>
    public partial class Z80Cpu
    {
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
}
