using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80
{
    /// <summary>
    /// Core operations, memory operations, reset, flags, stack operations
    /// </summary>
    public partial class Z80Cpu
    {
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
            _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(data, 7));
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, data == 0);

            _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
            _flags.SetParityFromValue(data);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);

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

        private void SwapRegisters(ref Z80Register register1, ref Z80Register register2)
        {
            (register1.Word, register2.Word) = (register2.Word, register1.Word);
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

        private void Load8BitRegisterFrom8BitRegister(byte sourceData, ref byte destination)
        {
            destination = sourceData;
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
            _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
            // Set half carry is carry from bit 3
            // Basically if all 4 lower bits are set, then incrementing means it would set bit 5 which in the high nibble
            // https://en.wikipedia.org/wiki/Half-carry_flag
            _flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x0F);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x7F);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
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
            _flags.SetClearFlagConditional(Z80StatusFlags.SignS, Bitwise.IsSet(newValue, 7));
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, newValue == 0);
            // Set half carry is borrow from bit 4
            // Basically if all 4 lower bits are clear, then decrementing would essentially set all the lower bits
            // ie. 0x20 - 1 = 0x1F
            // https://en.wikipedia.org/wiki/Half-carry_flag
            // This could also check by seeing if the new value & 0x0F == 0x0F means all the lower bits were set
            _flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, (oldValue & 0x0F) == 0x00);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, oldValue == 0x80);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
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
