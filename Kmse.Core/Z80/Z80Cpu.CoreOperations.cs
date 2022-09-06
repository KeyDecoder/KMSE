using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;
using Microsoft.Win32;

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

        private void InvertFlag(Z80StatusFlags flag)
        {
            SetClearFlagConditional(flag, !IsFlagSet(flag));
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
            PopRegisterFromStack(ref _pc);
        }

        private void PushRegisterToStack(Z80Register register)
        {
            var oldPointer = _stackPointer.Word;
            var currentPointer = _stackPointer.Word;
            _memory[--currentPointer] = register.High;
            _memory[--currentPointer] = register.Low;
            _stackPointer.Word = currentPointer;
            _cpuLogger.LogDebug($"Push to stack - Old - {oldPointer}, New = {_stackPointer.Word}");
        }

        private void PopRegisterFromStack(ref Z80Register register)
        {
            var oldPointer = _stackPointer.Word;
            var currentPointer = _stackPointer.Word;
            register.Low = _memory[currentPointer++];
            register.High = _memory[currentPointer++];
            _stackPointer.Word = currentPointer;
            _cpuLogger.LogDebug($"Pop from stack - Old - {oldPointer}, New = {_stackPointer.Word}");
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

        private byte ReadFromIo(byte high, byte low)
        {
            var address = (ushort)((high << 8) + low);
            return _io.ReadPort(address);
        }

        private byte ReadFromIoAndSetFlags(byte high, byte low)
        {
            var address = (ushort)((high << 8) + low);
            var data = _io.ReadPort(address);

            // If high bit set, then negative so set sign flag
            SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(data, 7));
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, data == 0);

            ClearFlag(Z80StatusFlags.HalfCarryH);
            SetParityFromValue(data);
            ClearFlag(Z80StatusFlags.AddSubtractN);

            return data;
        }

        private void ReadFromIoIntoRegister(byte high, byte low, ref byte destination)
        {
            var data = ReadFromIoAndSetFlags(high, low);
            destination = data;
        }

        private void WriteToIo(byte high, byte low, byte value)
        {
            var address = (ushort)((high << 8) + low);
            _io.WritePort(address, value);
        }

        private void SetParityFromValue(byte value)
        {
            // Count the number of 1 bits in the value
            // If odd, then clear flag and if even, then set flag
            var bitsSet = 0;
            for (var i = 0; i < 8; i++)
            {
                if (Bitwise.IsSet(value, i))
                {
                    bitsSet++;
                }
            }

            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, bitsSet == 0 || bitsSet % 2 == 0);
        }

        private void SwapRegisters(ref Z80Register register1, ref Z80Register register2)
        {
            (register1.Word, register2.Word) = (register2.Word, register1.Word);
        }

        private void SwapRegisterWithStackPointerLocation(ref Z80Register register)
        {
            var currentRegisterDataLow = register.Low;
            var currentRegisterDataHigh = register.High;
            register.Low = _memory[_stackPointer.Word];
            register.High = _memory[(ushort)(_stackPointer.Word + 1)];

            _memory[_stackPointer.Word] = currentRegisterDataLow;
            _memory[(ushort)(_stackPointer.Word + 1)] = currentRegisterDataHigh;
        }

        private void LoadRR(byte opCode)
        {
            // LD r,r' is 0 1 r r r r' r' r'
            var sourceRegisterId = (byte)(opCode & 0x07);
            var destinationRegisterId = (byte)((opCode & 0x38) >> 3);

            if (sourceRegisterId == 0x06 && destinationRegisterId == 0x06)
            {
                // 16 bit register load not supported in this method
                throw new InvalidOperationException($"Invalid op code, 16-bit load to same register LoadRR - OP code {opCode:X2}");
            }

            // Special cases where we are loading from or into memory location referenced by HL register rather than actual register
            if (sourceRegisterId == 0x06)
            {
                LoadFrom16BitRegisterMemoryLocationInto8BitRegisterById(_hl, destinationRegisterId);
                _currentCycleCount += 3;
                return;
            }

            if (destinationRegisterId == 0x06)
            {
                SaveTo16BitRegisterMemoryLocationFrom8BitRegisterById(_hl, sourceRegisterId);
                _currentCycleCount += 3;
                return;
            }

            ref byte sourceRegister = ref Get8BitRegisterByRIdentifier(sourceRegisterId);
            ref byte destinationRegister = ref Get8BitRegisterByRIdentifier(destinationRegisterId);

            destinationRegister = sourceRegister;
        }

        private void LoadFrom16BitRegisterMemoryLocationInto8BitRegisterById(Z80Register sourceRegister, byte destinationRegisterId)
        {
            ref var destinationRegister = ref Get8BitRegisterByRIdentifier(destinationRegisterId);
            LoadInto8BitRegisterFromMemory(ref destinationRegister, sourceRegister.Word);
        }

        private void SaveTo16BitRegisterMemoryLocationFrom8BitRegisterById(Z80Register destinationRegister, byte sourceRegisterId, byte offset = 0)
        {
            ref var sourceRegister = ref Get8BitRegisterByRIdentifier(sourceRegisterId);
            Save8BitRegisterValueToMemory(sourceRegister, (ushort)(destinationRegister.Word + offset));
        }

        private void SaveTo16BitRegisterMemoryLocationFrom8BitRegister(Z80Register destinationRegister, byte sourceRegister, byte offset = 0)
        {
            Save8BitRegisterValueToMemory(sourceRegister, (ushort)(destinationRegister.Word + offset));
        }

        private ref byte Get8BitRegisterByRIdentifier(byte identifier)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            // Return ref is not supported for switch expressions - https://github.com/dotnet/csharplang/issues/3326
            switch (identifier)
            {
                case 0: return ref _bc.High;
                case 1: return ref _bc.Low;
                case 2: return ref _de.High;
                case 3: return ref _de.Low;
                case 4: return ref _hl.High;
                case 5: return ref _hl.Low;
                // 6 is HL
                case 7: return ref _af.High;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadInto8BitRegisterFromMemory(ref byte register, ushort memoryLocation, byte offset = 0)
        {
            register = _memory[(ushort)(memoryLocation + offset)];
        }

        private void LoadInto16BitRegisterFromMemory(ref Z80Register register, ushort memoryLocation, byte offset = 0)
        {
            var location = (ushort)(memoryLocation + offset);
            register.Low = _memory[location];
            location++;
            register.High = _memory[location];
        }

        private void LoadValueInto8BitRegister(ref byte register, byte value)
        {
            register = value;
        }

        private void LoadValueInto16BitRegister(ref Z80Register register, ushort value)
        {
            register.Word = value;
        }

        private void Save8BitRegisterValueToMemory(byte value, ushort memoryLocation)
        {
            _memory[memoryLocation] = value;
        }

        private byte GetValueFromMemoryByRegisterLocation(Z80Register register, byte offset = 0)
        {
            return _memory[(ushort)(register.Word + offset)];
        }

        private void Save16BitRegisterToMemory(Z80Register register, ushort memoryLocation)
        {
            _memory[memoryLocation] = register.Low;
            _memory[(ushort)(memoryLocation + 1)] = register.High;
        }

        private void LoadValueIntoRegisterMemoryLocation(byte value, Z80Register register, byte offset = 0)
        {
            _memory[(ushort)(register.Word + offset)] = value;
        }

        private void Load16BitRegisterFrom16BitRegister(ref Z80Register source, Z80Register destination)
        {
            destination.Word = source.Word;
        }

        private void Load8BitRegisterFrom8BitRegister(byte sourceData, ref byte destination)
        {
            destination = sourceData;
        }

        private void LoadSpecial8BitRegisterToAccumulator(byte sourceData)
        {
            _af.High = sourceData;

            // Check flags since copying from special register into accumulator
            SetClearFlagConditional(Z80StatusFlags.SignS, !Bitwise.IsSet(sourceData, 7));
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, sourceData == 0);
            ClearFlag(Z80StatusFlags.HalfCarryH);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _interruptFlipFlop2);
            ClearFlag(Z80StatusFlags.AddSubtractN);
        }

        private void CopyMemoryByRegisterLocations(Z80Register source, Z80Register destination)
        {
            _memory[destination.Word] = _memory[source.Word];
        }

        private void Increment8Bit(ref byte register)
        {
            var oldValue = register;
            register = (byte)(register + 1);
            CheckIncrementFlags(register, oldValue);
        }

        private void IncrementAtRegisterMemoryLocation(Z80Register register, byte offset = 0)
        {
            var address = (ushort)(register.Word + offset);
            var value = _memory[address];
            Increment8Bit(ref value);

            _memory[address] = value;
        }

        private void CheckIncrementFlags(byte newValue, byte oldValue)
        {
            SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
            // Set half carry is carry from bit 3
            // Basically if all 4 lower bits are set, then incrementing means it would set bit 5 which in the high nibble
            // https://en.wikipedia.org/wiki/Half-carry_flag
            SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x0F);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x7F);
            ClearFlag(Z80StatusFlags.AddSubtractN);
        }

        private void Decrement8Bit(ref byte register)
        {
            var oldValue = register;
            register = (byte)(register - 1);
            CheckDecrementFlags(register, oldValue);
        }

        private void DecrementAtRegisterMemoryLocation(Z80Register register, byte offset = 0)
        {
            var address = (ushort)(register.Word + offset);
            var value = _memory[address];
            Decrement8Bit(ref value);
            _memory[address] = value;
        }

        private void CheckDecrementFlags(byte newValue, byte oldValue)
        {
            SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
            // Set half carry is borrow from bit 4
            // Basically if all 4 lower bits are clear, then decrementing would essentially set all the lower bits
            // ie. 0x20 - 1 = 0x1F
            // https://en.wikipedia.org/wiki/Half-carry_flag
            // This could also check by seeing if the new value & 0x0F == 0x0F means all the lower bits were set
            SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x00);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x80);
            SetFlag(Z80StatusFlags.AddSubtractN);
        }
        
        private void Increment16Bit(ref Z80Register register)
        {
            register.Word++;
        }

        private void Decrement16Bit(ref Z80Register register)
        {
            register.Word--;
        }
    }
}
