using Kmse.Core.Z80.Registers;

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
                var register = Get8BitRegisterByRIdentifier(destinationRegisterId);
                _memoryManagement.WriteToMemory(_hl, register.Value);
                _currentCycleCount += 3;
                return;
            }

            var sourceRegister = Get8BitRegisterByRIdentifier(sourceRegisterId);
            var destinationRegister = Get8BitRegisterByRIdentifier(destinationRegisterId);

            destinationRegister.Set(sourceRegister);
        }

        private IZ808BitRegister Get8BitRegisterByRIdentifier(byte identifier)
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
    }
}
