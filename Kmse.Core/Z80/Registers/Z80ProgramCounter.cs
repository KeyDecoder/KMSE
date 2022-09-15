using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers
{
    public class Z80ProgramCounter : IZ80ProgramCounter
    {
        private Z80Register _pc;
        private readonly IMasterSystemMemory _memory;
        private readonly IZ80InstructionLogger _z80InstructionLogger;

        public Z80ProgramCounter(IMasterSystemMemory memory, IZ80InstructionLogger z80InstructionLogger)
        {
            _memory = memory;
            _z80InstructionLogger = z80InstructionLogger;
        }

        public void Reset()
        {
            _pc.Word = 0x00;
        }

        public ushort GetValue()
        {
            return _pc.Word;
        }

        public Z80Register AsRegister()
        {
            return _pc;
        }

        public byte GetNextInstruction()
        {
            return GetNextByteByProgramCounter();
        }

        public byte GetNextDataByte()
        {
            var data = GetNextByteByProgramCounter();
            _z80InstructionLogger.AddOperationData(data);
            return data;
        }

        public ushort GetNextTwoDataBytes()
        {
            ushort data = GetNextByteByProgramCounter();
            data += (ushort)(GetNextByteByProgramCounter() << 8);
            _z80InstructionLogger.AddOperationData(data);
            return data;
        }

        /// <summary>
        /// Set program counter to new value, but don't save the old value to the stack 
        /// </summary>
        /// <param name="address">New address to set PC to</param>
        public void SetProgramCounter(ushort address)
        {
            _pc.Word = address;
        }

        /// <summary>
        /// Move program counter forward by the provided value
        /// </summary>
        /// <param name="offset">Add this offset to the current PC</param>
        public void MoveProgramCounterForward(ushort offset)
        {
            // If this goes above ushort max, we assume that when PC hits the limit it just wraps around instead of throwing an error or failing
            _pc.Word += offset;
        }

        /// <summary>
        /// Move program counter backward by the provided value
        /// </summary>
        /// <param name="offset">Subtract this offset from the current PC</param>
        public void MoveProgramCounterBackward(ushort offset)
        {
            // If this goes below zero/negative, we assume that when PC hits -1 it just wraps around instead of throwing an error or failing
            _pc.Word -= offset;
        }

        private byte GetNextByteByProgramCounter()
        {
            // Note: We don't increment the cycle count here since this operation is included in overall cycle count for each instruction
            var data = _memory[_pc.Word];
            _pc.Word++;
            return data;
        }

    }
}
