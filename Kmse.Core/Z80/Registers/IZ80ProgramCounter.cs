using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers;

public interface IZ80ProgramCounter
{
    void Reset();
    ushort GetValue();
    Z80Register AsRegister();
    byte GetNextInstruction();
    byte GetNextDataByte();
    ushort GetNextTwoDataBytes();

    /// <summary>
    /// Set program counter to new value, but don't save the old value to the stack 
    /// </summary>
    /// <param name="address">New address to set PC to</param>
    void SetProgramCounter(ushort address);

    /// <summary>
    /// Move program counter forward by the provided value
    /// </summary>
    /// <param name="offset">Add this offset to the current PC</param>
    void MoveProgramCounterForward(ushort offset);

    /// <summary>
    /// Move program counter backward by the provided value
    /// </summary>
    /// <param name="offset">Subtract this offset from the current PC</param>
    void MoveProgramCounterBackward(ushort offset);
}