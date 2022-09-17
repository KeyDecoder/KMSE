﻿using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80.Registers.SpecialPurpose;

public interface IZ80ProgramCounter : IZ8016BitRegister
{
    public byte GetNextInstruction();
    public byte GetNextDataByte();
    public ushort GetNextTwoDataBytes();

    /// <summary>
    ///     Move program counter forward by the provided value
    /// </summary>
    /// <param name="offset">Add this offset to the current PC</param>
    public void MoveProgramCounterForward(ushort offset);

    /// <summary>
    ///     Move program counter backward by the provided value
    /// </summary>
    /// <param name="offset">Subtract this offset from the current PC</param>
    public void MoveProgramCounterBackward(ushort offset);

    /// <summary>
    ///     Set the Program Counter but first push the current PC value to the stack
    /// </summary>
    /// <param name="address">New address to set PC to</param>
    void SetAndSaveExisting(ushort address);

    /// <summary>
    ///     Set program counter value to register value
    /// </summary>
    /// <param name="register">Z80 register</param>
    void Set(Z80Register register);

    /// <summary>
    ///     Set the Program Counter by popping address from top of the stack
    ///     This will pop two bytes for a 16 bit address
    /// </summary>
    void SetFromStack();

    void Jump16BitIfFlagCondition(Z80StatusFlags flag, ushort address);
    void Jump16BitIfNotFlagCondition(Z80StatusFlags flag, ushort address);
    void JumpByOffset(byte offset);
    bool JumpByOffsetIfFlagHasStatus(Z80StatusFlags flag, byte offset, bool status);
    void JumpByOffsetIfFlag(Z80StatusFlags flag, byte offset);
    void JumpByOffsetIfNotFlag(Z80StatusFlags flag, byte offset);
    void CallIfFlagCondition(Z80StatusFlags flag, ushort address);
    void CallIfNotFlagCondition(Z80StatusFlags flag, ushort address);
    bool ReturnIfFlagHasStatus(Z80StatusFlags flag, bool status);
    bool ReturnIfFlag(Z80StatusFlags flag);
    bool ReturnIfNotFlag(Z80StatusFlags flag);
}