using Kmse.Core.Utilities;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80
{
    /// <summary>
    /// Core operations, memory operations, reset, flags, stack operations
    /// </summary>
    public partial class Z80Cpu
    {
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
                Get8BitRegisterByRIdentifier(destinationRegisterId).SetFromDataInMemory(_hl);
                _currentCycleCount += 3;
                return;
            }

            if (destinationRegisterId == 0x06)
            {
                var register = Get8BitRegisterByRIdentifier(sourceRegisterId);
                _memoryManagement.WriteToMemory(_hl, register.Value);
                _currentCycleCount += 3;
                return;
            }

            var sourceRegister = Get8BitRegisterByRIdentifier(sourceRegisterId);
            var destinationRegister = Get8BitRegisterByRIdentifier(destinationRegisterId);

            destinationRegister.Set(sourceRegister);
        }

        private IZ808BitGeneralPurposeRegister Get8BitRegisterByRIdentifier(byte identifier)
        {
            return identifier switch
            {
                0 => _b,
                1 => _c,
                2 => _d,
                3 => _e,
                4 => _h,
                5 => _l,
                // 6 is HL so cannot return here since 16 bit register
                7 => _accumulator,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void ResetBitByOpCode(byte opCode)
        {
            var bit = (opCode & 0x38) >> 3;
            if (bit is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to reset");
            }
            var registerId = (byte)(opCode & 0x07);

            if (registerId == 0x06)
            {
                _hl.ResetBitByRegisterLocation(bit, 0);
                // Accessing (HL) increases cycle count
                _currentCycleCount += 7;
                return;
            }

            var register = Get8BitRegisterByRIdentifier(registerId);
            register.ClearBit(bit);
        }

        private void SetBitByOpCode(byte opCode)
        {
            var bit = (opCode & 0x38) >> 3;
            if (bit is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to set");
            }
            var registerId = (byte)(opCode & 0x07);

            if (registerId == 0x06)
            {
                _hl.SetBitByRegisterLocation(bit, 0);
                // Accessing (HL) increases cycle count
                _currentCycleCount += 7;
                return;
            }

            var register = Get8BitRegisterByRIdentifier(registerId);
            register.SetBit(bit);
        }

        private void TestBitByOpCode(byte opCode)
        {
            var bit = (opCode & 0x38) >> 3;
            if (bit is < 0 or > 7)
            {
                throw new ArgumentOutOfRangeException($"Bit {bit} is not a valid bit to test");
            }
            var registerId = (byte)(opCode & 0x07);

            if (registerId == 0x06)
            {
                _hl.TestBitByRegisterLocation(bit, 0);
                // Testing bit via (HL) memory location increases cycle count
                _currentCycleCount += 4;
                return;
            }

            var register = Get8BitRegisterByRIdentifier(registerId);
            var valueToCheck = register.Value;
            var bitSet = Bitwise.IsSet(valueToCheck, bit);
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, !bitSet);
            _flags.SetFlag(Z80StatusFlags.HalfCarryH);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);

            // This behaviour is not documented
            _flags.SetClearFlagConditional(Z80StatusFlags.SignS, (bit == 7 && bitSet));
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, !bitSet);
        }

        private void CompareIncrement()
        {
            var value = _memory[_hl.Value];
            // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
            var difference = _accumulator.Value - (sbyte)value;

            _hl.Increment();
            _bc.Decrement();

            _flags.SetIfNegative((byte)difference);
            _flags.SetIfZero((byte)(difference & 0xFF));

            _flags.SetIfHalfCarry(_accumulator.Value, value, difference);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Value != 0);
        }

        private void CompareDecrement()
        {
            var value = _memory[_hl.Value];
            // The compare is the difference and we do a subtract so we can tell if the comparison would be negative or not
            var difference = _accumulator.Value - (sbyte)value;

            _hl.Decrement();
            _bc.Decrement();

            _flags.SetIfNegative((byte)difference);
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, _af.High == value);
            _flags.SetIfHalfCarry(_accumulator.Value, value, difference);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Value != 0);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
        }
    }
}
