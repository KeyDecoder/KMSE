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
            return GetNextByte();
        }

        private byte GetNextByte()
        {
            // Note: We don't increment the cycle count here since this operation is included in overall cycle count for each instruction
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

        private ushort GetNextTwoBytes()
        {
            ushort data = GetNextOperation();
            data += (ushort)(GetNextOperation() << 8);
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
            // Storing PC in Stack so can resume later
            PushRegisterToStack(_pc);

            // Update PC to execute from new address
            _pc.Word = address;
        }

        private void SetProgramCounterFromRegister(Z80Register register)
        {
            // Update PC to execute from the value of the register
            _pc.Word = register.Word;
        }

        private void ResetProgramCounterFromStack()
        {
            var currentPointer = _stackPointer.Word;
            _pc.Low = _memory[++currentPointer];
            _pc.High = _memory[++currentPointer];
            _stackPointer.Word = currentPointer;
        }

        private void PushRegisterToStack(Z80Register register)
        {
            var currentPointer = _stackPointer.Word;
            _memory[--currentPointer] = register.High;
            _memory[--currentPointer] = register.Low;
            _stackPointer.Word = currentPointer;
        }
    }
}
