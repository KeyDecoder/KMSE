using Kmse.Core.Z80.Support;
using System.Data.Common;
using System.IO.Abstractions;
using System.Reflection;
using Kmse.Core.Utilities;
using Microsoft.VisualBasic.CompilerServices;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Kmse.Core.Z80;

/// <summary>
/// Generate the list of instructions and how to handle them
/// Created using this amazing page which summaries all the instructions - https://www.smspower.org/Development/InstructionSet
/// </summary>
public partial class Z80Cpu
{
    // TODO: In future, maybe we can combine these into a single dictionary and lookup but need to evaluate how different the handling is first
    private readonly Dictionary<byte, Instruction> _genericInstructions = new();
    private readonly Dictionary<byte, Instruction> _cbInstructions = new();
    private readonly Dictionary<byte, Instruction> _ddInstructions = new();
    private readonly Dictionary<byte, Instruction> _edInstructions = new();
    private readonly Dictionary<byte, Instruction> _fdInstructions = new();
    private readonly Dictionary<byte, SpecialCbInstruction> _specialDdcbInstructions = new();
    private readonly Dictionary<byte, SpecialCbInstruction> _specialFdcbInstructions = new();
    private const int DynamicCycleHandling = -1;

    private void AddStandardInstructionWithMask(byte opCode, byte mask, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        // These op codes do the same thing but generally have some information in the op code itself, but we can handle them with the same function
        // An example being ADD A, r where 80 is base r is low 3 bits (mask is 7) so op codes are 0x80 - 0x87 which all do the same add, just different registers
        
        // NOTE: We don't use i <= opCode+mask since the increment is before the check and this will wrap around (since byte type) and loop forever
        for (var i = opCode; i < opCode + mask; i++)
        {
            AddStandardInstruction(i, cycles, name, description, handleFunc);
        }
        AddStandardInstruction((byte)(opCode + mask), cycles, name, description, handleFunc);
    }

    private void AddDoubleByteInstructionWithMask(byte prefix, byte opCode, byte mask, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        // These op codes do the same thing but generally have some information in the op code itself, but we can handle them with the same function
        // An example being ADD A, r where 80 is base r is low 3 bits (mask is 7) so op codes are 0x80 - 0x87 which all do the same add, just different registers
        // NOTE: We don't use i <= opCode+mask since the increment is before the check and this will wrap around (since byte type) and loop forever
        for (var i = opCode; i < opCode + mask; i++)
        {
            AddDoubleByteInstruction(prefix, i, cycles, name, description, handleFunc);
        }
        AddDoubleByteInstruction(prefix, (byte)(opCode+mask), cycles, name, description, handleFunc);
    }

    private void AddStandardInstruction(byte opCode, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        _genericInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc));
    }

    private void AddDoubleByteInstruction(byte prefix, byte opCode, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        switch (prefix)
        {
            case 0xCB: _cbInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc)); break;
            case 0xDD: _ddInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc)); break;
            case 0xED: _edInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc)); break;
            case 0xFD: _fdInstructions.Add(opCode, new Instruction(opCode, name, description, cycles, handleFunc)); break;
            default:
                throw new ArgumentException("Only CB/DD/ED/FD double byte instructions supported");
        }
    }

    /// <summary>
    /// Add special instructions which are a double byte start (ie. FD/DD) prefix, then CB and then a value and then normal op code
    /// These require special handling since not a normal CB XX instruction
    /// </summary>
    /// <param name="prefix">First op code, usually 0xFD or 0xDD</param>
    /// <param name="opCode">Actual op code, after CB and value byte</param>
    /// <param name="cycles">Number of cycles</param>
    /// <param name="name">Name of instruction</param>
    /// <param name="description">Description of instruction</param>
    /// <param name="handleFunc">Action to call when op code executed</param>
    private void AddSpecialCbInstruction(byte prefix, byte opCode, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        switch (prefix)
        {
            case 0xDD: _specialDdcbInstructions.Add(opCode, new SpecialCbInstruction(opCode, name, description, cycles, handleFunc)); break;
            case 0xFD: _specialFdcbInstructions.Add(opCode, new SpecialCbInstruction(opCode, name, description, cycles, handleFunc)); break;
            default:
                throw new ArgumentException("Only FD/DD special CB instructions supported");
        }
    }

    private void AddSpecialCbInstructionWithMask(byte prefix, byte opCode, byte mask, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        // These op codes do the same thing but generally have some information in the op code itself, but we can handle them with the same function
        // An example being ADD A, r where 80 is base r is low 3 bits (mask is 7) so op codes are 0x80 - 0x87 which all do the same add, just different registers
        // NOTE: We don't use i <= opCode+mask since the increment is before the check and this will wrap around (since byte type) and loop forever
        for (var i = opCode; i < opCode + mask; i++)
        {
            AddSpecialCbInstruction(prefix, i, cycles, name, description, handleFunc);
        }
        AddSpecialCbInstruction(prefix, (byte)(opCode + mask), cycles, name, description, handleFunc);
    }

    private void PopulateInstructions()
    {
        PopulateCpuControlOperations();
        PopulateJumpCallAndReturnOperations();
        PopulateArthmeticAndLogicalInstructions();
        PopulateBitSetResetAndTestGroupInstructions();
        PopulateRotateAndShiftInstructions();
        PopulateLoadAndExchangeInstructions();
        PopulateExchangeBlockTransferAndSearchInstructions();
        PopulateInputOutputInstructions();
    }

    private void PopulateCpuControlOperations()
    {
        AddStandardInstruction(0x00, 4, "NOP", "No Operation", (_) => { });
        AddStandardInstruction(0x76, 4, "HALT", "Halt", (_) => { _halted = true; });

        AddStandardInstruction(0xF3, 4, "DI", "Disable Interrupts", (_) => { _interruptFlipFlop1 = false; _interruptFlipFlop2 = false; });
        AddStandardInstruction(0xFB, 4, "EI", "Enable Interrupts", (_) => { _interruptFlipFlop1 = true; _interruptFlipFlop2 = true; });
        AddDoubleByteInstruction(0xED, 0x46, 8, "IM 0", "Set Maskable Interupt to Mode 0", (_) => { _interruptMode = 0; });
        AddDoubleByteInstruction(0xED, 0x56, 8, "IM 1", "Set Maskable Interupt to Mode 1", (_) => { _interruptMode = 1; });
        AddDoubleByteInstruction(0xED, 0x5E, 8, "IM 2", "Set Maskable Interupt to Mode 2", (_) => { _interruptMode = 2; });
    }

    private void PopulateJumpCallAndReturnOperations()
    {
        AddStandardInstruction(0xE9, 4, "JP (HL)", "Unconditional Jump", _ => { SetProgramCounterFromRegister(_hl); });
        AddDoubleByteInstruction(0xDD, 0xE9, 8, "JP (IX)", "Unconditional Jump", _ => { SetProgramCounterFromRegister(_ix); });
        AddDoubleByteInstruction(0xFD, 0xE9, 8, "JP (IY)", "Unconditional Jump", _ => { SetProgramCounterFromRegister(_iy); });
        AddStandardInstruction(0xC3, 10, "JP $NN", "Unconditional Jump", _ =>  { SetProgramCounter(GetNextTwoBytes()); });

        AddStandardInstruction(0xDA, 10, "JP C,$NN", "Conditional Jump If Carry Set", _ => { Jump16BitIfFlagCondition(Z80StatusFlags.CarryC, GetNextTwoBytes()); });
        AddStandardInstruction(0xD2, 10, "JP NC,$NN", "Conditional Jump If Carry Not Set", _ => { Jump16BitIfNotFlagCondition(Z80StatusFlags.CarryC, GetNextTwoBytes()); });
        AddStandardInstruction(0xFA, 10, "JP M,$NN", "Conditional Jump If Negative", _ => { Jump16BitIfFlagCondition(Z80StatusFlags.SignS, GetNextTwoBytes()); });
        AddStandardInstruction(0xF2, 10, "JP P,$NN", "Conditional Jump If Positive", _ => { Jump16BitIfNotFlagCondition(Z80StatusFlags.SignS, GetNextTwoBytes()); });
        AddStandardInstruction(0xCA, 10, "JP Z,$NN", "Conditional Jump if Zero", _ => { Jump16BitIfFlagCondition(Z80StatusFlags.ZeroZ, GetNextTwoBytes()); });
        AddStandardInstruction(0xC2, 10, "JP NZ,$NN", "Conditional Jump If Not Zero", _ => { Jump16BitIfNotFlagCondition(Z80StatusFlags.ZeroZ, GetNextTwoBytes()); });
        AddStandardInstruction(0xEA, 10, "JP PE,$NN", "Conditional Jump If Parity Even", _ => { Jump16BitIfFlagCondition(Z80StatusFlags.ParityOverflowPV, GetNextTwoBytes()); });
        AddStandardInstruction(0xE2, 10, "JP PO,$NN", "Conditional Jump If Parity Odd", _ => { Jump16BitIfNotFlagCondition(Z80StatusFlags.ParityOverflowPV, GetNextTwoBytes()); });

        AddStandardInstruction(0x18, 12, "JR $N+2", "Relative Jump By Offset", _ => { JumpByOffset(GetNextByte()); });
        AddStandardInstruction(0x38, DynamicCycleHandling, "JR C,$N+2", "Cond. Relative Jump", _ => { JumpByOffsetIfFlag(Z80StatusFlags.CarryC, GetNextByte()); });
        AddStandardInstruction(0x30, DynamicCycleHandling, "JR NC,$N+2", "Cond. Relative Jump", _ => { JumpByOffsetIfNotFlag(Z80StatusFlags.CarryC, GetNextByte()); });
        AddStandardInstruction(0x28, DynamicCycleHandling, "JR Z,$N+2", "Cond. Relative Jump", _ => { JumpByOffsetIfFlag(Z80StatusFlags.ZeroZ, GetNextByte()); });
        AddStandardInstruction(0x20, DynamicCycleHandling, "JR NZ,$N+2", "Cond. Relative Jump", _ => { JumpByOffsetIfNotFlag(Z80StatusFlags.ZeroZ, GetNextByte()); });

        AddStandardInstruction(0x10, DynamicCycleHandling, "DJNZ $+2", "Decrement, Jump if Non-Zero", _ =>
        {
            Decrement8Bit(ref _bc.High);
            var offset = GetNextByte();
            if (_bc.High != 0)
            {
                JumpByOffset(offset);
                _currentCycleCount += 13;
            }

            // Not jumping, continue to next instruction
            _currentCycleCount += 8;
        });

        AddStandardInstruction(0xCD, 17, "CALL NN", "Unconditional Call", _ => { SaveAndUpdateProgramCounter(GetNextTwoBytes()); });
        AddStandardInstruction(0xDC, DynamicCycleHandling, "CALL C,NN", "Conditional Call If Carry Set", _ => { CallIfFlagCondition(Z80StatusFlags.CarryC, GetNextTwoBytes()); });
        AddStandardInstruction(0xD4, DynamicCycleHandling, "CALL NC,NN", "Conditional Call If Carry Not Set", _ => { CallIfNotFlagCondition(Z80StatusFlags.CarryC, GetNextTwoBytes()); });
        AddStandardInstruction(0xFC, DynamicCycleHandling, "CALL M,NN", "Conditional Call If Negative", _ => { CallIfFlagCondition(Z80StatusFlags.SignS, GetNextTwoBytes()); });
        AddStandardInstruction(0xF4, DynamicCycleHandling, "CALL P,NN", "Conditional Call If Negative", _ => { CallIfNotFlagCondition(Z80StatusFlags.SignS, GetNextTwoBytes()); });
        AddStandardInstruction(0xCC, DynamicCycleHandling, "CALL Z,NN", "Conditional Call If Zero", _ => { CallIfFlagCondition(Z80StatusFlags.ZeroZ, GetNextTwoBytes()); });
        AddStandardInstruction(0xC4, DynamicCycleHandling, "CALL NZ,NN", "Conditional Call If Not Zero", _ => { CallIfNotFlagCondition(Z80StatusFlags.ZeroZ, GetNextTwoBytes()); });
        AddStandardInstruction(0xEC, DynamicCycleHandling, "CALL PE,NN", "Conditional Call If Parity Even", _ => { CallIfFlagCondition(Z80StatusFlags.ParityOverflowPV, GetNextTwoBytes()); });
        AddStandardInstruction(0xE4, DynamicCycleHandling, "CALL PO,NN", "Conditional Call If Parity Odd", _ => { CallIfNotFlagCondition(Z80StatusFlags.ParityOverflowPV, GetNextTwoBytes()); });

        AddStandardInstruction(0xC9, 10, "RET", "Return", _ => { ResetProgramCounterFromStack(); });
        AddStandardInstruction(0xD8, DynamicCycleHandling, "RET C", "Conditional Return If Carry Set", _ => { ReturnIfFlag(Z80StatusFlags.CarryC); });
        AddStandardInstruction(0xD0, DynamicCycleHandling, "RET NC", "Conditional Return If Carry Not Set", _ => { ReturnIfNotFlag(Z80StatusFlags.CarryC); });
        AddStandardInstruction(0xF8, DynamicCycleHandling, "RET M", "Conditional Return If Negative", _ => { ReturnIfFlag(Z80StatusFlags.SignS); });
        AddStandardInstruction(0xF0, DynamicCycleHandling, "RET P", "Conditional Return If Positive", _ => { ReturnIfNotFlag(Z80StatusFlags.SignS); });
        AddStandardInstruction(0xC8, DynamicCycleHandling, "RET Z", "Conditional Return If Zero", _ => { ReturnIfFlag(Z80StatusFlags.ZeroZ); });
        AddStandardInstruction(0xC0, DynamicCycleHandling, "RET NZ", "Conditional Return If Not Zero", _ => { ReturnIfNotFlag(Z80StatusFlags.ZeroZ); });
        AddStandardInstruction(0xE8, DynamicCycleHandling, "RET PE", "Conditional Return If Parity Even", _ => { ReturnIfFlag(Z80StatusFlags.ParityOverflowPV); });
        AddStandardInstruction(0xE0, DynamicCycleHandling, "RET PO", "Conditional Return If Parity Odd", _ => { ReturnIfNotFlag(Z80StatusFlags.ParityOverflowPV); });

        AddStandardInstruction(0xC7, 11, "RST 0", "Restart", _ => { SaveAndUpdateProgramCounter(0x00); });
        AddStandardInstruction(0xCF, 11, "RST 08H", "", _ => { SaveAndUpdateProgramCounter(0x08); });
        AddStandardInstruction(0xD7, 11, "RST 10H", "", _ => { SaveAndUpdateProgramCounter(0x10); });
        AddStandardInstruction(0xDF, 11, "RST 18H", "", _ => { SaveAndUpdateProgramCounter(0x18); });
        AddStandardInstruction(0xE7, 11, "RST 20H", "", _ => { SaveAndUpdateProgramCounter(0x20); });
        AddStandardInstruction(0xEF, 11, "RST 28H", "", _ => { SaveAndUpdateProgramCounter(0x28); });
        AddStandardInstruction(0xF7, 11, "RST 30H", "", _ => { SaveAndUpdateProgramCounter(0x30); });
        AddStandardInstruction(0xFF, 11, "RST 38H", "", _ => { SaveAndUpdateProgramCounter(0x38); });
        
        AddDoubleByteInstruction(0xED, 0x4D, 14, "RETI", "Return from Interrupt", _ => { ResetProgramCounterFromStack(); _io.ClearMaskableInterrupt(); });
        AddDoubleByteInstruction(0xED, 0x45, 14, "RETN", "Return from NMI", _ => { ResetProgramCounterFromStack(); _interruptFlipFlop1 = _interruptFlipFlop2; });
    }

    private void PopulateArthmeticAndLogicalInstructions()
    {
        AddStandardInstruction(0xCE, 7, "ADC A, N", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xDD, 0x8E, 19, "ADC A,(IX+d)", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xFD, 0x8E, 19, "ADC A,(IY+d)", "Add with Carry", _ => { });

        AddStandardInstructionWithMask(0x88, 7, 4, "ADC A, r", "Add with Carry", _ => { });
        AddStandardInstructionWithMask(0x80, 7, 4, "ADD A,r", "Add (8-bit)", _ => { });
        AddStandardInstruction(0x09, 11, "ADD HL,BC", "Add (16-bit)", _ => { });
        AddStandardInstruction(0x19, 11, "ADD HL,DE", "", _ => { });
        AddStandardInstruction(0x29, 11, "ADD HL,HL", "", _ => { });
        AddStandardInstruction(0x39, 11, "ADD HL,SP", "", _ => { });
        AddStandardInstructionWithMask(0xA0, 7, 4, "AND r", "Logical AND", _ => { });
        AddStandardInstruction(0xC6,7, "ADD A, N", "Add", _ => { });
        AddDoubleByteInstruction(0xED, 0x4A, 15, "ADC HL,BC", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x5A, 15, "ADC HL,DE", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x6A, 15, "ADC HL,HL", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x7A, 15, "ADC HL,SP", "Add with Carry", _ => { });
        AddDoubleByteInstruction(0xDD, 0x09, 15, "ADD IX,BC", "Add (IX register)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x19, 15, "ADD IX,DE", "Add", _ => { });
        AddDoubleByteInstruction(0xDD, 0x29, 15, "ADD IX,IX", "Add", _ => { });
        AddDoubleByteInstruction(0xDD, 0x39, 15, "ADD IX,SP", "Add", _ => { });
        AddDoubleByteInstruction(0xFD, 0x09, 15, "ADD IY,BC", "Add (IY register)", _ => { });
        AddDoubleByteInstruction(0xFD, 0x19, 15, "ADD IY,DE", "Add", _ => { });
        AddDoubleByteInstruction(0xFD, 0x29, 15, "ADD IY,IY", "Add", _ => { });
        AddDoubleByteInstruction(0xFD, 0x39, 15, "ADD IY,SP", "Add", _ => { });
        AddDoubleByteInstruction(0xDD, 0x86, 19, "ADD A,(IX+d)", "Add", _ => { });
        AddDoubleByteInstruction(0xFD, 0x86, 19, "ADD A,(IY+d)", "Add", _ => { });
        AddDoubleByteInstruction(0xDD, 0xA6, 19, "AND (IX+d)", "And", _ => { });
        AddDoubleByteInstruction(0xFD, 0xA6, 19, "AND (IY+d)", "And", _ => { });

        AddStandardInstructionWithMask(0x98, 7, 4, "SBC r", "Subtract with Carry", _ => { });
        AddStandardInstructionWithMask(0x90, 7, 4, "SUB r", "Subtract", _ => { });
        AddStandardInstruction(0xD6, 7, "SUB N", "Subtract", _ => { });
        AddDoubleByteInstruction(0xDD, 0x96, 19, "SUB (IX+d)", "Subtract", _ => { });
        AddDoubleByteInstruction(0xFD, 0x96, 19, "SUB (IY+d)", "Subtract", _ => { });

        AddDoubleByteInstruction(0xED, 0x42, 15, "SBC HL,BC", "Subtract with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x52, 15, "SBC HL,DE", "Subtract with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x62, 15, "SBC HL,HL", "Subtract with Carry", _ => { });
        AddDoubleByteInstruction(0xED, 0x72, 15, "SBC HL,SP", "Subtract with Carry", _ => { });
        AddDoubleByteInstruction(0xDD, 0x9E, 19, "SBC A,(IX+d)", "Subtract with Carry", _ => { });
        AddDoubleByteInstruction(0xFD, 0x9E, 19, "SBC A,(IY+d)", "Subtract with Carry", _ => { });

        AddStandardInstructionWithMask(0xB8, 7, 4, "CP r", "Compare", _ => { });
        AddStandardInstruction(0xFE, 7, "CP N", "Compare", _ => { });
        AddDoubleByteInstruction(0xDD, 0xBE, 19, "CP (IX+d)", "Compare", _ => { });
        AddDoubleByteInstruction(0xFD, 0xBE, 19, "CP (IY+d)", "Compare", _ => { });

        AddDoubleByteInstruction(0xED, 0xA9, 16, "CPD", "Compare and Decrement", _ => { });
        AddDoubleByteInstruction(0xED, 0xB9, 21 / 16, "CPDR", "Compare, Decrement, Repeat", _ => { });
        AddDoubleByteInstruction(0xED, 0xA1, 16, "CPI", "Compare and Increment", _ => { });
        AddDoubleByteInstruction(0xED, 0xB1, DynamicCycleHandling, "CPIR", "Compare, Increment, Repeat", _ => { });

        AddStandardInstruction(0xE6, 7, "AND N", "And", _ => { });

        AddStandardInstructionWithMask(0xB0, 7, 4, "OR r", "Logical inclusive OR", _ => { });
        AddStandardInstruction(0xF6, 7, "OR N", "Or", _ => { });
        AddDoubleByteInstruction(0xDD, 0xB6, 19, "OR (IX+d)", "Or", _ => { });
        AddDoubleByteInstruction(0xFD, 0xB6, 19, "OR (IY+d)", "Or", _ => { });

        AddStandardInstructionWithMask(0xA8, 7, 4, "XOR r", "Logical Exclusive OR", _ => { });
        AddStandardInstruction(0xEE, 7, "XOR N", "Xor", _ => { });
        AddDoubleByteInstruction(0xDD, 0xAE, 19, "XOR (IX+d)", "Xor", _ => { });
        AddDoubleByteInstruction(0xFD, 0xAE, 19, "XOR (IY+d)", "Xor", _ => { });

        AddStandardInstruction(0x3C, 4, "INC A", "Increment A", _ => { Increment8Bit(ref _af.High, true); });
        AddStandardInstruction(0x04, 4, "INC B", "Increment B", _ => { Increment8Bit(ref _bc.High, true); });
        AddStandardInstruction(0x0C, 4, "INC C", "Increment C", _ => { Increment8Bit(ref _bc.Low, true); });
        AddStandardInstruction(0x14, 4, "INC D", "Increment D", _ => { Increment8Bit(ref _de.High, true); });
        AddStandardInstruction(0x1C, 4, "INC E", "Increment E", _ => { Increment8Bit(ref _de.Low, true); });
        AddStandardInstruction(0x24, 4, "INC H", "Increment H", _ => { Increment8Bit(ref _hl.High, true); });
        AddStandardInstruction(0x2C, 4, "INC L", "Increment L", _ => { Increment8Bit(ref _hl.Low, true); });
        AddStandardInstruction(0x3, 6, "INC BC", "Increment BC", _ => { Increment16Bit(ref _bc); });
        AddStandardInstruction(0x13, 6, "INC DE", "Increment DE", _ => { Increment16Bit(ref _de); });
        AddStandardInstruction(0x23, 6, "INC HL", "Increment HL", _ => { Increment16Bit(ref _hl); });
        AddStandardInstruction(0x33, 6, "INC SP", "Increment SP", _ => { Increment16Bit(ref _stackPointer); });
        AddDoubleByteInstruction(0xDD, 0x23, 10, "INC IX", "Increment", _ => { Increment16Bit(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0x23, 10, "INC IY", "Increment", _ => { Increment16Bit(ref _iy); });

        AddStandardInstruction(0x34, 11, "INC (HL)", "Increment (indirect)", _ => { IncrementAtRegisterMemoryLocation(_hl, 0, true); });
        AddDoubleByteInstruction(0xDD, 0x34, 23, "INC (IX+d)", "Increment", _ => { IncrementAtRegisterMemoryLocation(_ix, GetNextByte(), true); });
        AddDoubleByteInstruction(0xFD, 0x34, 23, "INC (IY+d)", "Increment", _ => { IncrementAtRegisterMemoryLocation(_iy, GetNextByte(), true); });

        AddStandardInstruction(0x3D, 4, "DEC A", "Decrement A", _ => { Decrement8Bit(ref _af.High, true); });
        AddStandardInstruction(0x05, 4, "DEC B", "Decrement B", _ => { Decrement8Bit(ref _bc.High, true); });
        AddStandardInstruction(0x0D, 4, "DEC C", "Decrement C", _ => { Decrement8Bit(ref _bc.Low, true); });
        AddStandardInstruction(0x15, 4, "DEC D", "Decrement D", _ => { Decrement8Bit(ref _de.High, true); });
        AddStandardInstruction(0x1D, 4, "DEC E", "Decrement E", _ => { Decrement8Bit(ref _de.Low, true); });
        AddStandardInstruction(0x25, 4, "DEC H", "Decrement H", _ => { Decrement8Bit(ref _hl.High, true); });
        AddStandardInstruction(0x2D, 4, "DEC L", "Decrement L", _ => { Decrement8Bit(ref _hl.Low, true); });

        AddStandardInstruction(0x0B, 6, "DEC BC", "Decrement BC", _ => { Decrement16Bit(ref _bc); });
        AddStandardInstruction(0x1B, 6, "DEC DE", "Decrement DE", _ => { Decrement16Bit(ref _de); });
        AddStandardInstruction(0x2B, 6, "DEC HL", "Decrement HL", _ => { Decrement16Bit(ref _hl); });
        AddStandardInstruction(0x3B, 6, "DEC SP", "Decrement SP", _ => { Decrement16Bit(ref _stackPointer); });
        AddDoubleByteInstruction(0xDD, 0x2B, 10, "DEC IX", "Decrement IX", _ => { Decrement16Bit(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0x2B, 10, "DEC IY", "Decrement IY", _ => { Decrement16Bit(ref _iy); });

        AddStandardInstruction(0x35, 11, "DEC (HL)", "", _ => { DecrementAtRegisterMemoryLocation(_hl, 0, true); });
        AddDoubleByteInstruction(0xDD, 0x35, 23, "DEC (IX+d)", "Decrement", _ => { DecrementAtRegisterMemoryLocation(_ix, GetNextByte(), true); });
        AddDoubleByteInstruction(0xFD, 0x35, 23, "DEC (IY+d)", "Decrement", _ => { DecrementAtRegisterMemoryLocation(_iy, GetNextByte(), true); });

        AddStandardInstruction(0x3F, 4, "CCF", "Complement Carry Flag", _ => { ClearFlag(Z80StatusFlags.AddSubtractN); InvertFlag(Z80StatusFlags.CarryC); });
        AddStandardInstruction(0x27, 4, "DAA", "Decimal Adjust Accumulator", _ => { DecimalAdjustAccumulator();  });
        AddStandardInstruction(0X2F, 4, "CPL", "Complement", _ => { InvertAccumulatorRegister(); });
        AddStandardInstruction(0x37, 4, "SCF", "Set Carry Flag", _ => { ClearFlag(Z80StatusFlags.HalfCarryH); ClearFlag(Z80StatusFlags.AddSubtractN); SetFlag(Z80StatusFlags.CarryC); });
        AddDoubleByteInstruction(0xED, 0x44, 8, "NEG", "Negate", _ => { NegateAccumulatorRegister(); });
    }

    private void PopulateBitSetResetAndTestGroupInstructions()
    {
        AddDoubleByteInstructionWithMask(0xCB, 0x80, 0x3F, 8, "RES b,r", "Reset bit in register", i => { ResetBitByOpCode(i.OpCode); });
        AddSpecialCbInstructionWithMask(0xDD, 0x86, 7, 23, "RES b,(IX+d)", "Reset bit in (IX+d)", i => { ResetBitByRegisterLocation(_ix, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstructionWithMask(0xFD, 0x86, 7, 23, "RES b,(IY+d)", "Reset bit in (IY+d)", i => { ResetBitByRegisterLocation(_iy, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstructionWithMask(0xCB, 0x40, 7, 0x3F, "BIT b,r", "Test Bit", i => { TestBitByOpCode(i.OpCode); });
        AddSpecialCbInstructionWithMask(0xDD, 0x46, 7, 20, "BIT b,(IX+d)", "Test Bit", i => { TestBitByRegisterLocation(_ix, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstructionWithMask(0xFD, 0x46, 7, 20, "BIT b,(IY+d)", "Test Bit", i => { TestBitByRegisterLocation(_ix, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstructionWithMask(0xCB, 0xC0, 0x3F, 8, "SET b,r", "Set bit", i => { SetBitByOpCode(i.OpCode); });
        AddSpecialCbInstructionWithMask(0xDD, 0xC6, 7, 23, "SET b,(IX+d)", "Set Bit", i => { SetBitByRegisterLocation(_ix, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstructionWithMask(0xFD, 0xC6, 7, 23, "SET b,(IY+d)", "Set Bit", i => { SetBitByRegisterLocation(_iy, i.OpCode & 0x38, ((SpecialCbInstruction)i).DataByte); });
    }

    private void PopulateRotateAndShiftInstructions()
    {
        AddStandardInstruction(0x17, 4, "RLA", "Rotate Left Accumulator", _ => { });
        AddStandardInstruction(0x07, 4, "RLCA", "Rotate Left Circular Accumulator", _ => { });
        AddStandardInstruction(0x1F, 4, "RRA", "Rotate Right Accumulator", _ => { });
        AddStandardInstruction(0x0F, 4, "RRCA", "Rotate Right Circular Accumulator", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x20, 7, 8, "SLA r", "Shift Left Arithmetic", _ => { });
        AddSpecialCbInstruction(0xDD, 0x26, 23, "SLA (IX+d)", "Shift Left Arithmetic", _ => { });
        AddSpecialCbInstruction(0xFD, 0x26, 23, "SLA (IY+d)", "Shift Left Arithmetic", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x28, 7, 8, "SRA r", "Shift Right Arithmetic", _ => { });
        AddSpecialCbInstruction(0xDD, 0x2E, 23, "SRA (IX+d)", "Shift Right Arithmetic", _ => { });
        AddSpecialCbInstruction(0xFD, 0x2E, 23, "SRA (IY+d)", "Shift Right Arithmetic", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x30, 7, 8, "SLL r", "Shift Left Logical*", _ => { });
        AddSpecialCbInstruction(0xDD, 0x36, 23, "SLL (IX+d)", "Shift Left Logical", _ => { });
        AddSpecialCbInstruction(0xFD, 0x36, 23, "SLL (IY+d)", "Shift Left Logical", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x38, 7, 8, "SRL r", "Shift Right Logical", _ => { });
        AddSpecialCbInstruction(0xDD, 0x3E, 23, "SRL (IX+d)", "Shift Right Logical", _ => { });
        AddSpecialCbInstruction(0xFD, 0x3E, 23, "SRL (IY+d)", "Shift Right Logical", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x10, 7, 8, "RL r", "Rotate Left", _ => { });
        AddSpecialCbInstruction(0xDD, 0x16, 23, "RL (IX+d)", "Rotate Left", _ => { });
        AddSpecialCbInstruction(0xFD, 0x16, 23, "RL (IY+d)", "Rotate Left", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x00, 7, 8, "RLC r", "Rotate Left Circular", _ => { });
        AddSpecialCbInstruction(0xDD, 0x06, 23, "RLC (IX+d)", "Rotate Left Circular", _ => { });
        AddSpecialCbInstruction(0xFD, 0x06, 23, "RLC (IY+d)", "Rotate Left Circular", _ => { });
        
        AddDoubleByteInstruction(0xED, 0x6F, 18, "RLD", "Rotate Left 4 bits", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x18, 7, 8, "RR r", "Rotate Right", _ => { });
        AddSpecialCbInstruction(0xDD, 0x1E, 23, "RR (IX+d)", "Rotate Right", _ => { });
        AddSpecialCbInstruction(0xFD, 0x1E, 23, "RR (IY+d)", "Rotate Right", _ => { });

        AddDoubleByteInstructionWithMask(0xCB, 0x08, 7, 8, "RRC r", "Rotate Right Circular", _ => { });
        AddSpecialCbInstruction(0xDD, 0x0E, 23, "RRC (IX+d)", "Rotate Right Circular", _ => { });
        AddSpecialCbInstruction(0xFD, 0x0E, 23, "RRC (IY+d)", "Rotate Right Circular", _ => { });

        AddDoubleByteInstruction(0xED, 0x67, 18, "RRD", "Rotate Right 4 bits", _ => { });
        AddStandardInstruction(0xDE, 7, "SBC A,N", "Rotate Right 4 bits", _ => { });
    }

    private void PopulateLoadAndExchangeInstructions()
    {
        AddStandardInstructionWithMask(0x78, 7, 4, "LD A,r", "Load 8-bit register into A", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x40, 7, 4, "LD B,r", "Load 8-bit register into B", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x48, 7, 4, "LD C,r", "Load 8-bit register into C", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x50, 7, 4, "LD D,r", "Load 8-bit register into D", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x58, 7, 4, "LD E,r", "Load 8-bit register into E", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x60, 7, 4, "LD H,r", "Load 8-bit register into H", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x68, 7, 4, "LD L,r", "Load 8-bit register into L", i => { LoadRR(i.OpCode); });
        AddStandardInstructionWithMask(0x70, 5, 7, "LD (HL),r", "Load (Indirect)", i => { LoadRR(i.OpCode); });

        AddStandardInstruction(0x0A, 7, "LD A,(BC)", "Load A into memory location at BC", _ => { LoadInto8BitRegisterFromMemory(ref _af.High, _bc.Word); });
        AddStandardInstruction(0x1A, 7, "LD A,(DE)", "Load A into memory location at DE", _ => { LoadInto8BitRegisterFromMemory(ref _af.High, _de.Word); });
        AddStandardInstruction(0x2, 7, "LD (BC),A", "Load memory location at BC into A", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_bc, _af.High); });
        AddStandardInstruction(0x12, 7, "LD (DE),A", "Load memory location at BC into A", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_de, _af.High); });

        AddDoubleByteInstruction(0xDD, 0x77, 19, "LD (IX+d),A", "Load A into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _af.High, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x70, 19, "LD (IX+d),B", "Load B into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _bc.High, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x71, 19, "LD (IX+d),C", "Load C into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _bc.Low, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x72, 19, "LD (IX+d),D", "Load D into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _de.High, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x73, 19, "LD (IX+d),E", "Load E into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _de.Low, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x74, 19, "LD (IX+d),H", "Load H into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _hl.High, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x75, 19, "LD (IX+d),L", "Load L into memory location at IX+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_ix, _hl.Low, GetNextByte()); });

        AddDoubleByteInstruction(0xFD, 0x77, 19, "LD (IY+d),A", "Load A into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _af.High, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x70, 19, "LD (IY+d),B", "Load B into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _bc.High, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x71, 19, "LD (IY+d),C", "Load C into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _bc.Low, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x72, 19, "LD (IY+d),D", "Load D into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _de.High, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x73, 19, "LD (IY+d),E", "Load E into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _de.Low, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x74, 19, "LD (IY+d),H", "Load H into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _hl.High, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x75, 19, "LD (IY+d),L", "Load L into memory location at IY+D", _ => { SaveTo16BitRegisterMemoryLocationFrom8BitRegister(_iy, _hl.Low, GetNextByte()); });

        AddStandardInstruction(0xF9, 6, "LD SP,HL", "", _ => { Load16BitRegisterFrom16BitRegister(ref _hl, _stackPointer); });
        AddDoubleByteInstruction(0xDD, 0xF9, 10, "LD SP,IX", "Load", _ => { Load16BitRegisterFrom16BitRegister(ref _ix, _stackPointer); });
        AddDoubleByteInstruction(0xFD, 0xF9, 10, "LD SP,IY", "Load", _ => { Load16BitRegisterFrom16BitRegister(ref _iy, _stackPointer); });

        AddDoubleByteInstruction(0xED, 0x47, 9, "LD I,A", "Load A into I", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _iRegister); });
        AddDoubleByteInstruction(0xED, 0x4F, 9, "LD R,A", "Load A into R", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _rRegister); });
        AddDoubleByteInstruction(0xED, 0x57, 9, "LD A,I", "Load I into A", _ => { LoadSpecial8BitRegisterToAccumulator(_iRegister); });
        AddDoubleByteInstruction(0xED, 0x5F, 9, "LD A,R", "Load R into A", _ => { LoadSpecial8BitRegisterToAccumulator(_rRegister); });
        AddStandardInstruction(0x3E, 7, "LD A,N", "Load n into A", _ => { LoadValueInto8BitRegister(ref _af.High, GetNextByte()); });
        AddStandardInstruction(0x06, 7, "LD B,N", "Load n into B", _ => { LoadValueInto8BitRegister(ref _bc.High, GetNextByte()); });
        AddStandardInstruction(0x0E, 7, "LD C,N", "Load n into C", _ => { LoadValueInto8BitRegister(ref _bc.Low, GetNextByte()); });
        AddStandardInstruction(0x16, 7, "LD D,N", "Load n into D", _ => { LoadValueInto8BitRegister(ref _de.High, GetNextByte()); });
        AddStandardInstruction(0x1E, 7, "LD E,N", "Load n into E", _ => { LoadValueInto8BitRegister(ref _de.Low, GetNextByte()); });
        AddStandardInstruction(0x26, 7, "LD H,N", "Load n into H", _ => { LoadValueInto8BitRegister(ref _hl.High, GetNextByte()); });
        AddStandardInstruction(0x2E, 7, "LD L,N", "Load n into L", _ => { LoadValueInto8BitRegister(ref _hl.Low, GetNextByte()); });

        AddDoubleByteInstruction(0xDD, 0x7E, 19, "LD A,(IX+d)", "Load memory at IX + d into A", _ => { LoadInto8BitRegisterFromMemory(ref _af.High, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x46, 19, "LD B,(IX+d)", "Load memory at IX + d into B", _ => { LoadInto8BitRegisterFromMemory(ref _bc.High, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x4E, 19, "LD C,(IX+d)", "Load memory at IX + d into C", _ => { LoadInto8BitRegisterFromMemory(ref _bc.Low, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x56, 19, "LD D,(IX+d)", "Load memory at IX + d into D", _ => { LoadInto8BitRegisterFromMemory(ref _de.High, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x5E, 19, "LD E,(IX+d)", "Load memory at IX + d into E", _ => { LoadInto8BitRegisterFromMemory(ref _de.Low, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x66, 19, "LD H,(IX+d)", "Load memory at IX + d into H", _ => { LoadInto8BitRegisterFromMemory(ref _hl.High, _ix.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xDD, 0x6E, 19, "LD L,(IX+d)", "Load memory at IX + d into L", _ => { LoadInto8BitRegisterFromMemory(ref _hl.Low, _ix.Word, GetNextByte()); });

        AddDoubleByteInstruction(0xFD, 0x7E, 19, "LD A,(IY+d)", "Load memory at IY + d into A", _ => { LoadInto8BitRegisterFromMemory(ref _af.High, _iy.Word, GetNextByte());});
        AddDoubleByteInstruction(0xFD, 0x46, 19, "LD B,(IY+d)", "Load memory at IY + d into B", _ => { LoadInto8BitRegisterFromMemory(ref _bc.High, _iy.Word, GetNextByte());});
        AddDoubleByteInstruction(0xFD, 0x4E, 19, "LD C,(IY+d)", "Load memory at IY + d into C", _ => { LoadInto8BitRegisterFromMemory(ref _bc.Low, _iy.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x56, 19, "LD D,(IY+d)", "Load memory at IY + d into D", _ => { LoadInto8BitRegisterFromMemory(ref _de.High, _iy.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x5E, 19, "LD E,(IY+d)", "Load memory at IY + d into E", _ => { LoadInto8BitRegisterFromMemory(ref _de.Low, _iy.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x66, 19, "LD H,(IY+d)", "Load memory at IY + d into H", _ => { LoadInto8BitRegisterFromMemory(ref _hl.High, _iy.Word, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x6E, 19, "LD L,(IY+d)", "Load memory at IY + d into L", _ => { LoadInto8BitRegisterFromMemory(ref _hl.Low, _iy.Word, GetNextByte()); });

        AddStandardInstruction(0x36, 10, "LD (HL),N", "Load value n into memory at HL", _ => { LoadValueIntoRegisterMemoryLocation(GetNextByte(), _hl); });
        AddDoubleByteInstruction(0xDD, 0x36, 19, "LD(IX + d), N", "Load value n into location at IX + d", _ => { LoadValueIntoRegisterMemoryLocation(GetNextByte(), _ix, GetNextByte()); });
        AddDoubleByteInstruction(0xFD, 0x36, 19, "LD(IY + d), N", "Load value n into location at IY + d", _ => { LoadValueIntoRegisterMemoryLocation(GetNextByte(), _iy, GetNextByte()); });

        AddStandardInstruction(0x3A, 13, "LD A,(NN)", "Load value at memory location NN into A", _ => { LoadInto8BitRegisterFromMemory(ref _af.High, GetNextTwoBytes()); });
        AddStandardInstruction(0x2A, 16, "LD HL,(NN)", "Load value at memory location NN into HL", _ => { LoadInto16BitRegisterFromMemory(ref _hl, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x4B, 20, "LD BC, (NN)", "Load value at memory location NN into BC", _ => { LoadInto16BitRegisterFromMemory(ref _bc, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x5B, 20, "LD DE, (NN)", "Load value at memory location NN into DE", _ => { LoadInto16BitRegisterFromMemory(ref _de, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x7B, 20, "LD SP, (NN)", "Load value at memory location NN into SP", _ => { LoadInto16BitRegisterFromMemory(ref _stackPointer, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xDD, 0x2A, 20, "LD IX, (NN)", "Load value at memory location NN into IX", _ => { LoadInto16BitRegisterFromMemory(ref _ix, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xFD, 0x2A, 20, "LD IY, (NN)", "Load value at memory location NN into IY", _ => { LoadInto16BitRegisterFromMemory(ref _iy, GetNextTwoBytes()); });

        AddStandardInstruction(0x01, 10, "LD BC,NN", "Load nn value into BC", _ => { LoadValueInto16BitRegister(ref _bc, GetNextTwoBytes()); });
        AddStandardInstruction(0x11, 10, "LD DE,NN", "Load nn value into DE", _ => { LoadValueInto16BitRegister(ref _de, GetNextTwoBytes()); });
        AddStandardInstruction(0x21, 10, "LD HL,NN", "Load nn value into HL", _ => { LoadValueInto16BitRegister(ref _hl, GetNextTwoBytes()); });
        AddStandardInstruction(0x31, 10, "LD SP,NN", "Load nn value into SP", _ => { LoadValueInto16BitRegister(ref _stackPointer, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xDD, 0x21, 14, "LD IX, NN", "Load nn value into IX", _ => { LoadValueInto16BitRegister(ref _ix, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xFD, 0x21, 14, "LD IY, NN", "Load nn value into IY", _ => { LoadValueInto16BitRegister(ref _iy, GetNextTwoBytes()); });

        AddStandardInstruction(0x32, 13, "LD (NN),A", "Load A into memory location NN", _ => { Save8BitRegisterValueToMemory(_af.High, GetNextTwoBytes()); });
        AddStandardInstruction(0x22, 16, "LD (NN),HL", "Load HL into memory location NN", _ => { Save16BitRegisterToMemory(_hl, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x43, 20, "LD(NN), BC", "Load BC into memory location NN", _ => { Save16BitRegisterToMemory(_bc, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x53, 20, "LD(NN), DE", "Load DE into memory location NN", _ => { Save16BitRegisterToMemory(_de, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xDD, 0x22, 20, "LD(NN), IX", "Load IX into memory location NN", _ => { Save16BitRegisterToMemory(_ix, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xFD, 0x22, 20, "LD(NN), IY", "Load IY into memory location NN", _ => { Save16BitRegisterToMemory(_iy, GetNextTwoBytes()); });
        AddDoubleByteInstruction(0xED, 0x73, 20, "LD(NN), SP", "Load SP into memory location NN", _ => { Save16BitRegisterToMemory(_stackPointer, GetNextTwoBytes()); });

        AddDoubleByteInstruction(0xED, 0xA0, 16, "LDI", "Load and Increment", _ =>
        {
            CopyMemoryByRegisterLocations(_hl, _de);
            Increment16Bit(ref _hl);
            Increment16Bit(ref _de);
            Decrement16Bit(ref _bc);
            ClearFlag(Z80StatusFlags.HalfCarryH);
            ClearFlag(Z80StatusFlags.AddSubtractN);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Word != 0);
        });
        AddDoubleByteInstruction(0xED, 0xB0, DynamicCycleHandling, "LDIR", "Load, Increment, Repeat", _ =>
        {
            if (_bc.Word == 0)
            {
                // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
                //_bc.Word = 64 * 1024;

                _currentCycleCount += 16;
                return;
            }

            CopyMemoryByRegisterLocations(_hl, _de);
            Increment16Bit(ref _hl);
            Increment16Bit(ref _de);
            Decrement16Bit(ref _bc);
            ClearFlag(Z80StatusFlags.HalfCarryH);
            ClearFlag(Z80StatusFlags.AddSubtractN);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Word != 0);

            if (_bc.Word != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });
        AddDoubleByteInstruction(0xED, 0xA8, 16, "LDD", "Load and Decrement", _ =>
        {
            CopyMemoryByRegisterLocations(_hl, _de);
            Decrement16Bit(ref _hl);
            Decrement16Bit(ref _de);
            Decrement16Bit(ref _bc);
            ClearFlag(Z80StatusFlags.HalfCarryH);
            ClearFlag(Z80StatusFlags.AddSubtractN);
            SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Word != 0);
        });
        AddDoubleByteInstruction(0xED, 0xB8, DynamicCycleHandling, "LDDR", "Load, Decrement, Repeat", _ =>
        {
            if (_bc.Word == 0)
            {
                // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
                //_bc.Word = 64 * 1024;

                _currentCycleCount += 16;
                return;
            }

            CopyMemoryByRegisterLocations(_hl, _de);
            Decrement16Bit(ref _hl);
            Decrement16Bit(ref _de);
            Decrement16Bit(ref _bc);
            ClearFlag(Z80StatusFlags.HalfCarryH);
            ClearFlag(Z80StatusFlags.AddSubtractN);
            // This is not a typo, for some reason in LDDR the PV flag is reset unlike the other LDx instructions
            ClearFlag(Z80StatusFlags.ParityOverflowPV);

            if (_bc.Word != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddStandardInstruction(0xF5, 11, "PUSH AF", "Push AF", _ => { PushRegisterToStack(_af); });
        AddStandardInstruction(0xC5, 11, "PUSH BC", "Push BC", _ => { PushRegisterToStack(_bc); });
        AddStandardInstruction(0xD5, 11, "PUSH DE", "Push DE", _ => { PushRegisterToStack(_de); });
        AddStandardInstruction(0xE5, 11, "PUSH HL", "Push HL", _ => { PushRegisterToStack(_hl); });
        AddDoubleByteInstruction(0xDD, 0xE5, 15, "PUSH IX", "Push IX", _ => { PushRegisterToStack(_ix); });
        AddDoubleByteInstruction(0xFD, 0xE5, 15, "PUSH IY", "Push IY", _ => { PushRegisterToStack(_iy); });

        AddStandardInstruction(0xF1, 10, "POP AF", "Pop AF from Stack", _ => { PopRegisterFromStack(ref _af); });
        AddStandardInstruction(0xC1, 10, "POP BC", "Pop BC from Stack", _ => { PopRegisterFromStack(ref _bc); });
        AddStandardInstruction(0xD1, 10, "POP DE", "Pop DE from Stack", _ => { PopRegisterFromStack(ref _de); });
        AddStandardInstruction(0xE1, 10, "POP HL", "Pop HL from Stack", _ => { PopRegisterFromStack(ref _hl); });
        AddDoubleByteInstruction(0xDD, 0xE1, 14, "POP IX", "Pop IX from Stack", _ => { PopRegisterFromStack(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0xE1, 14, "POP IY", "Pop IY from Stack", _ => { PopRegisterFromStack(ref _iy); });
    }

    private void PopulateExchangeBlockTransferAndSearchInstructions()
    {
        AddStandardInstruction(0xE3, 19, "EX (SP),HL", "Exchange HL with Data from Memory Address in SP", _ => { SwapRegisterWithStackPointerLocation(ref _hl); });
        AddStandardInstruction(0x8, 4, "EX AF,AF'", "Exchange AF and AF Shadow", _ => { SwapRegisters(ref _af, ref _afShadow); });
        AddStandardInstruction(0xEB, 4, "EX DE,HL", "Exchange DE and HL", _ => { SwapRegisters(ref _de, ref _hl); });
        AddStandardInstruction(0xD9, 4, "EXX", "Exchange BC, DE, HL with Shadow Registers", _ => { SwapRegisters(ref _bc, ref _bcShadow); SwapRegisters(ref _de, ref _deShadow); SwapRegisters(ref _hl, ref _hlShadow); });
        AddDoubleByteInstruction(0xDD, 0xE3, 23, "EX (SP),IX", "Exchange IX with Data from Memory Address in SP", _ => { SwapRegisterWithStackPointerLocation(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0xE3, 23, "EX (SP),IY", "Exchange IY with Data from Memory Address in SP", _ => { SwapRegisterWithStackPointerLocation(ref _iy); });
    }

    private void PopulateInputOutputInstructions()
    {
        AddStandardInstruction(0xDB, 11, "IN A,(N)", "Read I/O at N into A", _ => { _af.High = ReadFromIo(_af.High, GetNextByte()); });
        AddDoubleByteInstruction(0xED, 0x70, 12, "IN (C)", "Read I/O at B/C But Only Set Flags", _ => { ReadFromIoAndSetFlags(_bc.High, _bc.Low); });
        AddDoubleByteInstruction(0xED, 0x78, 12, "IN A,(C)", "Read I/O at B/C into A with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _af.High); });
        AddDoubleByteInstruction(0xED, 0x40, 12, "IN B,(C)", "Read I/O at B/C into B with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _bc.High); });
        AddDoubleByteInstruction(0xED, 0x48, 12, "IN C,(C)", "Read I/O at B/C into C with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _bc.Low); });
        AddDoubleByteInstruction(0xED, 0x50, 12, "IN D,(C)", "Read I/O at B/C into D with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _de.High); });
        AddDoubleByteInstruction(0xED, 0x58, 12, "IN E,(C)", "Read I/O at B/C into E with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _de.Low); });
        AddDoubleByteInstruction(0xED, 0x60, 12, "IN H,(C)", "Read I/O at B/C into H with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _hl.High); });
        AddDoubleByteInstruction(0xED, 0x68, 12, "IN L,(C)", "Read I/O at B/C into L with flags", _ => { ReadFromIoIntoRegister(_bc.High, _bc.Low, ref _hl.Low); });
        
        AddDoubleByteInstruction(0xED, 0xA2, 16, "INI", "Input and Increment", _ =>
        {
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            var data = _io.ReadPort(portAddress);
            Save8BitRegisterValueToMemory(data, _hl.Word);
            Decrement8Bit(ref _bc.High);
            Increment16Bit(ref _hl);
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            SetFlag(Z80StatusFlags.AddSubtractN);
        });
        AddDoubleByteInstruction(0xED, 0xB2, DynamicCycleHandling, "INIR", "Input, Increment, Repeat", _ =>
        {
            if (_bc.High == 0)
            {
                // B was set to 0 before instruction was executed, so set to 256 bytes accordingly to documentation but no emulator does this
                //_bc.Word = 256;

                _currentCycleCount += 16;
                return;
            }

            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            var data = _io.ReadPort(portAddress);
            Save8BitRegisterValueToMemory(data, _hl.Word);
            Decrement8Bit(ref _bc.High);
            Increment16Bit(ref _hl);
            SetFlag(Z80StatusFlags.ZeroZ);
            SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddDoubleByteInstruction(0xED, 0xAA, 16, "IND", "Input and Decrement", _ =>
        {
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            var data = _io.ReadPort(portAddress);
            Save8BitRegisterValueToMemory(data, _hl.Word);
            Decrement8Bit(ref _bc.High);
            Decrement16Bit(ref _hl);
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            SetFlag(Z80StatusFlags.AddSubtractN);
        });
        AddDoubleByteInstruction(0xED, 0xBA, DynamicCycleHandling, "INDR", "Input, Decrement, Repeat", _ =>
        {
            if (_bc.High == 0)
            {
                // B was set to 0 before instruction was executed, so set to 256 bytes accordingly to documentation but no emulator does this
                //_bc.Word = 256;

                _currentCycleCount += 16;
                return;
            }

            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            var data = _io.ReadPort(portAddress);
            Save8BitRegisterValueToMemory(data, _hl.Word);
            Decrement8Bit(ref _bc.High);
            Decrement16Bit(ref _hl);
            SetFlag(Z80StatusFlags.ZeroZ);
            SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });


        AddStandardInstruction(0xD3, 11, "OUT (N),A", "Write I/O at n from A", _ => { WriteToIo(_af.High, GetNextByte(), _af.High); });
        AddDoubleByteInstruction(0xED, 0x79, 12, "OUT (C),A", "Write I/O at B/C from A", _ => { WriteToIo(_bc.High, _bc.Low, _af.High); });
        AddDoubleByteInstruction(0xED, 0x41, 12, "OUT (C),B", "Write I/O at B/C from B", _ => { WriteToIo(_bc.High, _bc.Low, _bc.High); });
        AddDoubleByteInstruction(0xED, 0x49, 12, "OUT (C),C", "Write I/O at B/C from C", _ => { WriteToIo(_bc.High, _bc.Low, _bc.Low); });
        AddDoubleByteInstruction(0xED, 0x51, 12, "OUT (C),D", "Write I/O at B/C from D", _ => { WriteToIo(_bc.High, _bc.Low, _de.High); });
        AddDoubleByteInstruction(0xED, 0x59, 12, "OUT (C),E", "Write I/O at B/C from E", _ => { WriteToIo(_bc.High, _bc.Low, _de.Low); });
        AddDoubleByteInstruction(0xED, 0x61, 12, "OUT (C),H", "Write I/O at B/C from H", _ => { WriteToIo(_bc.High, _bc.Low, _hl.High); });
        AddDoubleByteInstruction(0xED, 0x69, 12, "OUT (C),L", "Write I/O at B/C from L", _ => { WriteToIo(_bc.High, _bc.Low, _hl.Low); });

        AddDoubleByteInstruction(0xED, 0xA3, 16, "OUTI", "Output and Increment", _ =>
        {
            var data = GetValueFromMemoryByRegisterLocation(_hl);
            Decrement8Bit(ref _bc.High);
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            Increment16Bit(ref _hl);
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            SetFlag(Z80StatusFlags.AddSubtractN);
        });
        AddDoubleByteInstruction(0xED, 0xB3, DynamicCycleHandling, "OTIR", "Output, Increment, Repeat", _ =>
        {
            if (_bc.High == 0)
            {
                // B was set to 0 before instruction was executed, so set to 256 bytes accordingly to documentation but no emulator does this
                //_bc.Word = 256;

                _currentCycleCount += 16;
                return;
            }

            var data = GetValueFromMemoryByRegisterLocation(_hl);
            Decrement8Bit(ref _bc.High);
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            Increment16Bit(ref _hl);
            SetFlag(Z80StatusFlags.ZeroZ);
            SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddDoubleByteInstruction(0xED, 0xAB, 16, "OUTD", "Output and Decrement", _ =>
        {
            var data = GetValueFromMemoryByRegisterLocation(_hl);
            Decrement8Bit(ref _bc.High);
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            Decrement16Bit(ref _hl);
            SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            SetFlag(Z80StatusFlags.AddSubtractN);
        });
        AddDoubleByteInstruction(0xED, 0xBB, DynamicCycleHandling, "OTDR", "Output, Decrement, Repeat", _ =>
        {
            if (_bc.High == 0)
            {
                // B was set to 0 before instruction was executed, so set to 256 bytes accordingly to documentation but no emulator does this
                //_bc.Word = 256;

                _currentCycleCount += 16;
                return;
            }

            var data = GetValueFromMemoryByRegisterLocation(_hl);
            Decrement8Bit(ref _bc.High);
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            Decrement16Bit(ref _hl);
            SetFlag(Z80StatusFlags.ZeroZ);
            SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                SetProgramCounter((ushort)(_pc.Word - 2));
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });
    }

    private enum CbInstructionModes : byte
    {
        Normal = 0x00,
        DD = 0xDD,
        FD = 0xFD
    }

    private class Instruction
    {
        public Instruction(byte opCode, string name, string description, int cycles, Action<Instruction> handleMethod)
        {
            OpCode = opCode;
            Name = name;
            Description = description;
            ClockCycles = cycles;
            _handleMethod = handleMethod;
        }

        private readonly Action<Instruction> _handleMethod;

        public byte OpCode { get; }
        public string Name { get; }
        public string Description { get; }
        public int ClockCycles { get; }

        public void Execute()
        {
            _handleMethod(this);
        }
    }

    /// <summary>
    ///     Special CB instructions where they are DD/FD CB XX
    /// </summary>
    private class SpecialCbInstruction : Instruction
    {
        public byte DataByte { get; private set; }

        public SpecialCbInstruction(byte opCode, string name, string description, int cycles,
            Action<Instruction> handleMethod) : base(opCode, name, description, cycles, handleMethod) { }

        public void SetDataByte(byte data)
        {
            DataByte = data;
        }
    }
}