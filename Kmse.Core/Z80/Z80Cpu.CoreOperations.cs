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

        /// <summary>
        /// Set program counter to new value, but don't save the old value to the stack 
        /// </summary>
        /// <param name="address">New address to set PC to</param>
        private void SetProgramCounter(ushort address)
        {
            // Update PC to execute from new address
            _pc.Word = address;
        }

        private void SaveAndUpdateProgramCounter(ushort address)
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

        private void Jump16BitIfFlagCondition(Z80StatusFlags flag, ushort address)
        {
            if (IsFlagSet(flag))
            {
                SetProgramCounter(address);
            }
        }

        private void Jump16BitIfNotFlagCondition(Z80StatusFlags flag, ushort address)
        {
            if (!IsFlagSet(flag))
            {
                SetProgramCounter(address);
            }
        }

        private void JumpByOffset(byte offset)
        {
            var newPcLocation = _pc.Word;

            // Range is -126 to +129 so we need a signed version
            // However sbyte only goes from -128 to +127 but we need -126 to +129 so have to do this manually
            if (offset <= 129)
            {
                newPcLocation += offset;
            }
            else
            {
                // 256 minus our offset gives us a positive number for where it would rollover at 129
                // And we minus this since this would be negative number
                newPcLocation -= (ushort)(256 - offset);
            }

            // Note we don't need to add one here since we always increment the Pc after we read from it, so it's already pointing to next command at this point
            SetProgramCounter(newPcLocation);
        }

        private void JumpByOffsetIfFlagHasStatus(Z80StatusFlags flag, byte offset, bool status)
        {
            if (IsFlagSet(flag) == status)
            {
                JumpByOffset(offset);
                _currentCycleCount += 12;
            }

            _currentCycleCount += 7;
        }

        private void JumpByOffsetIfFlag(Z80StatusFlags flag, byte offset)
        {
            JumpByOffsetIfFlagHasStatus(flag, offset, true);
        }

        private void JumpByOffsetIfNotFlag(Z80StatusFlags flag, byte offset)
        {
            JumpByOffsetIfFlagHasStatus(flag, offset, false);
        }

        private void CallIfFlagCondition(Z80StatusFlags flag, ushort address)
        {
            if (IsFlagSet(flag))
            {
                SaveAndUpdateProgramCounter(address);
            }
        }

        private void CallIfNotFlagCondition(Z80StatusFlags flag, ushort address)
        {
            if (!IsFlagSet(flag))
            {
                SaveAndUpdateProgramCounter(address);
            }
        }

        private void ReturnIfFlagHasStatus(Z80StatusFlags flag, bool status)
        {
            if (IsFlagSet(flag) == status)
            {
                ResetProgramCounterFromStack();
                _currentCycleCount += 11;
            }

            _currentCycleCount += 5;
        }

        private void ReturnIfFlag(Z80StatusFlags flag)
        {
            ReturnIfFlagHasStatus(flag, true);
        }

        private void ReturnIfNotFlag(Z80StatusFlags flag)
        {
            ReturnIfFlagHasStatus(flag, false);
        }
    }
}
