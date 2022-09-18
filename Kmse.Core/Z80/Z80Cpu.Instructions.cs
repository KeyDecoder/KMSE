﻿using Kmse.Core.Z80.Support;

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
            case 0xCB: _cbInstructions.Add(opCode, new Instruction(prefix, opCode, name, description, cycles, handleFunc)); break;
            case 0xDD: _ddInstructions.Add(opCode, new Instruction(prefix, opCode, name, description, cycles, handleFunc)); break;
            case 0xED: _edInstructions.Add(opCode, new Instruction(prefix, opCode, name, description, cycles, handleFunc)); break;
            case 0xFD: _fdInstructions.Add(opCode, new Instruction(prefix, opCode, name, description, cycles, handleFunc)); break;
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
            case 0xDD: _specialDdcbInstructions.Add(opCode, new SpecialCbInstruction(prefix, opCode, name, description, cycles, handleFunc)); break;
            case 0xFD: _specialFdcbInstructions.Add(opCode, new SpecialCbInstruction(prefix, opCode, name, description, cycles, handleFunc)); break;
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
        AddStandardInstruction(0x76, 4, "HALT", "Halt", (_) => { Halt(); });

        AddStandardInstruction(0xF3, 4, "DI", "Disable Interrupts", (_) => { _interruptFlipFlop1 = false; _interruptFlipFlop2 = false; });
        AddStandardInstruction(0xFB, 4, "EI", "Enable Interrupts", (_) => { _interruptFlipFlop1 = true; _interruptFlipFlop2 = true; });
        AddDoubleByteInstruction(0xED, 0x46, 8, "IM 0", "Set Maskable Interupt to Mode 0", (_) => { _interruptMode = 0; });
        AddDoubleByteInstruction(0xED, 0x56, 8, "IM 1", "Set Maskable Interupt to Mode 1", (_) => { _interruptMode = 1; });
        AddDoubleByteInstruction(0xED, 0x5E, 8, "IM 2", "Set Maskable Interupt to Mode 2", (_) => { _interruptMode = 2; });
    }

    private void PopulateJumpCallAndReturnOperations()
    {
        AddStandardInstruction(0xE9, 4, "JP (HL)", "Unconditional Jump", _ => { _pc.Set(_hl); });
        AddDoubleByteInstruction(0xDD, 0xE9, 8, "JP (IX)", "Unconditional Jump", _ => { _pc.Set(_ix); });
        AddDoubleByteInstruction(0xFD, 0xE9, 8, "JP (IY)", "Unconditional Jump", _ => { _pc.Set(_iy); });
        AddStandardInstruction(0xC3, 10, "JP $NN", "Unconditional Jump", _ =>  { _pc.Set(_pc.GetNextTwoDataBytes()); });

        AddStandardInstruction(0xDA, 10, "JP C,$NN", "Conditional Jump If Carry Set", _ => { _pc.Jump16BitIfFlagCondition(Z80StatusFlags.CarryC, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xD2, 10, "JP NC,$NN", "Conditional Jump If Carry Not Set", _ => { _pc.Jump16BitIfNotFlagCondition(Z80StatusFlags.CarryC, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xFA, 10, "JP M,$NN", "Conditional Jump If Negative", _ => { _pc.Jump16BitIfFlagCondition(Z80StatusFlags.SignS, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xF2, 10, "JP P,$NN", "Conditional Jump If Positive", _ => { _pc.Jump16BitIfNotFlagCondition(Z80StatusFlags.SignS, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xCA, 10, "JP Z,$NN", "Conditional Jump if Zero", _ => { _pc.Jump16BitIfFlagCondition(Z80StatusFlags.ZeroZ, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xC2, 10, "JP NZ,$NN", "Conditional Jump If Not Zero", _ => { _pc.Jump16BitIfNotFlagCondition(Z80StatusFlags.ZeroZ, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xEA, 10, "JP PE,$NN", "Conditional Jump If Parity Even", _ => { _pc.Jump16BitIfFlagCondition(Z80StatusFlags.ParityOverflowPV, _pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xE2, 10, "JP PO,$NN", "Conditional Jump If Parity Odd", _ => { _pc.Jump16BitIfNotFlagCondition(Z80StatusFlags.ParityOverflowPV, _pc.GetNextTwoDataBytes()); });

        AddStandardInstruction(0x18, 12, "JR $N+2", "Relative Jump By Offset", _ => { _pc.JumpByOffset(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x38, DynamicCycleHandling, "JR C,$N+2", "Cond. Relative Jump", _ => { _currentCycleCount += _pc.JumpByOffsetIfFlag(Z80StatusFlags.CarryC, _pc.GetNextDataByte()) ? 12 : 7; });
        AddStandardInstruction(0x30, DynamicCycleHandling, "JR NC,$N+2", "Cond. Relative Jump", _ => { _currentCycleCount += _pc.JumpByOffsetIfNotFlag(Z80StatusFlags.CarryC, _pc.GetNextDataByte()) ? 12 : 7; });
        AddStandardInstruction(0x28, DynamicCycleHandling, "JR Z,$N+2", "Cond. Relative Jump", _ => { _currentCycleCount += _pc.JumpByOffsetIfFlag(Z80StatusFlags.ZeroZ, _pc.GetNextDataByte()) ? 12 : 7; });
        AddStandardInstruction(0x20, DynamicCycleHandling, "JR NZ,$N+2", "Cond. Relative Jump", _ => { _currentCycleCount += _pc.JumpByOffsetIfNotFlag(Z80StatusFlags.ZeroZ, _pc.GetNextDataByte()) ? 12 : 7; });

        AddStandardInstruction(0x10, DynamicCycleHandling, "DJNZ $+2", "Decrement, Jump if Non-Zero", _ =>
        {
            _b.Decrement();
            var offset = _pc.GetNextDataByte();
            if (_bc.High != 0)
            {
                _pc.JumpByOffset(offset);
                _currentCycleCount += 13;
            }

            // Not jumping, continue to next instruction
            _currentCycleCount += 8;
        });

        AddStandardInstruction(0xCD, 17, "CALL NN", "Unconditional Call", _ => { _pc.SetAndSaveExisting(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0xDC, DynamicCycleHandling, "CALL C,NN", "Conditional Call If Carry Set", _ => { _currentCycleCount += _pc.CallIfFlagCondition(Z80StatusFlags.CarryC, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xD4, DynamicCycleHandling, "CALL NC,NN", "Conditional Call If Carry Not Set", _ => { _currentCycleCount += _pc.CallIfNotFlagCondition(Z80StatusFlags.CarryC, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xFC, DynamicCycleHandling, "CALL M,NN", "Conditional Call If Negative", _ => { _currentCycleCount += _pc.CallIfFlagCondition(Z80StatusFlags.SignS, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xF4, DynamicCycleHandling, "CALL P,NN", "Conditional Call If Negative", _ => { _currentCycleCount += _pc.CallIfNotFlagCondition(Z80StatusFlags.SignS, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xCC, DynamicCycleHandling, "CALL Z,NN", "Conditional Call If Zero", _ => { _currentCycleCount += _pc.CallIfFlagCondition(Z80StatusFlags.ZeroZ, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xC4, DynamicCycleHandling, "CALL NZ,NN", "Conditional Call If Not Zero", _ => { _currentCycleCount += _pc.CallIfNotFlagCondition(Z80StatusFlags.ZeroZ, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xEC, DynamicCycleHandling, "CALL PE,NN", "Conditional Call If Parity Even", _ => { _currentCycleCount += _pc.CallIfFlagCondition(Z80StatusFlags.ParityOverflowPV, _pc.GetNextTwoDataBytes()) ? 17 : 10; });
        AddStandardInstruction(0xE4, DynamicCycleHandling, "CALL PO,NN", "Conditional Call If Parity Odd", _ => { _currentCycleCount += _pc.CallIfNotFlagCondition(Z80StatusFlags.ParityOverflowPV, _pc.GetNextTwoDataBytes()) ? 17 : 10; });

        AddStandardInstruction(0xC9, 10, "RET", "Return", _ => { _pc.SetFromStack(); });
        AddStandardInstruction(0xD8, DynamicCycleHandling, "RET C", "Conditional Return If Carry Set", _ => { _currentCycleCount += _pc.ReturnIfFlag(Z80StatusFlags.CarryC) ? 11 : 5; });
        AddStandardInstruction(0xD0, DynamicCycleHandling, "RET NC", "Conditional Return If Carry Not Set", _ => { _currentCycleCount += _pc.ReturnIfNotFlag(Z80StatusFlags.CarryC) ? 11 : 5; });
        AddStandardInstruction(0xF8, DynamicCycleHandling, "RET M", "Conditional Return If Negative", _ => { _currentCycleCount += _pc.ReturnIfFlag(Z80StatusFlags.SignS) ? 11 : 5; });
        AddStandardInstruction(0xF0, DynamicCycleHandling, "RET P", "Conditional Return If Positive", _ => { _currentCycleCount += _pc.ReturnIfNotFlag(Z80StatusFlags.SignS) ? 11 : 5; });
        AddStandardInstruction(0xC8, DynamicCycleHandling, "RET Z", "Conditional Return If Zero", _ => { _currentCycleCount += _pc.ReturnIfFlag(Z80StatusFlags.ZeroZ) ? 11 : 5; });
        AddStandardInstruction(0xC0, DynamicCycleHandling, "RET NZ", "Conditional Return If Not Zero", _ => { _currentCycleCount += _pc.ReturnIfNotFlag(Z80StatusFlags.ZeroZ) ? 11 : 5; });
        AddStandardInstruction(0xE8, DynamicCycleHandling, "RET PE", "Conditional Return If Parity Even", _ => { _currentCycleCount += _pc.ReturnIfFlag(Z80StatusFlags.ParityOverflowPV) ? 11 : 5; });
        AddStandardInstruction(0xE0, DynamicCycleHandling, "RET PO", "Conditional Return If Parity Odd", _ => { _currentCycleCount += _pc.ReturnIfNotFlag(Z80StatusFlags.ParityOverflowPV) ? 11 : 5; });

        AddStandardInstruction(0xC7, 11, "RST 0", "Restart at 0h", _ => { _pc.SetAndSaveExisting(0x00); });
        AddStandardInstruction(0xCF, 11, "RST 08H", "Restart at 08h", _ => {  _pc.SetAndSaveExisting(0x08); });
        AddStandardInstruction(0xD7, 11, "RST 10H", "Restart at 10h", _ => {  _pc.SetAndSaveExisting(0x10); });
        AddStandardInstruction(0xDF, 11, "RST 18H", "Restart at 18h", _ => {  _pc.SetAndSaveExisting(0x18); });
        AddStandardInstruction(0xE7, 11, "RST 20H", "Restart at 20h", _ => {  _pc.SetAndSaveExisting(0x20); });
        AddStandardInstruction(0xEF, 11, "RST 28H", "Restart at 28h", _ => {  _pc.SetAndSaveExisting(0x28); });
        AddStandardInstruction(0xF7, 11, "RST 30H", "Restart at 30h", _ => {  _pc.SetAndSaveExisting(0x30); });
        AddStandardInstruction(0xFF, 11, "RST 38H", "Restart at 38h", _ => { _pc.SetAndSaveExisting(0x38); });
        
        AddDoubleByteInstruction(0xED, 0x4D, 14, "RETI", "Return from Interrupt", _ => { _pc.SetFromStack(); _io.ClearMaskableInterrupt(); });
        AddDoubleByteInstruction(0xED, 0x45, 14, "RETN", "Return from NMI", _ => { _pc.SetFromStack(); _interruptFlipFlop1 = _interruptFlipFlop2; });
    }

    private void PopulateArthmeticAndLogicalInstructions()
    {
        AddStandardInstruction(0x87, 4, "ADD A, A", "Add A to A", _ => { AddValueTo8BitRegister(_af.High, ref _af.High); });
        AddStandardInstruction(0x80, 4, "ADD A, B", "Add B to A", _ => { AddValueTo8BitRegister(_bc.High, ref _af.High); });
        AddStandardInstruction(0x81, 4, "ADD A, C", "Add C to A", _ => { AddValueTo8BitRegister(_bc.Low, ref _af.High); });
        AddStandardInstruction(0x82, 4, "ADD A, D", "Add D to A", _ => { AddValueTo8BitRegister(_de.High, ref _af.High); });
        AddStandardInstruction(0x83, 4, "ADD A, E", "Add E to A", _ => { AddValueTo8BitRegister(_de.Low, ref _af.High); });
        AddStandardInstruction(0x84, 4, "ADD A, H", "Add H to A", _ => { AddValueTo8BitRegister(_hl.High, ref _af.High); });
        AddStandardInstruction(0x85, 4, "ADD A, L", "Add L to A", _ => { AddValueTo8BitRegister(_hl.Low, ref _af.High); });
        AddStandardInstruction(0xC6, 7, "ADD A, N", "Add", _ => { AddValueTo8BitRegister(_pc.GetNextDataByte(), ref _af.High);});

        AddStandardInstruction(0x09, 11, "ADD HL,BC", "Add BC to HL", _ => { Add16BitRegisterTo16BitRegister(_bc, ref _hl); });
        AddStandardInstruction(0x19, 11, "ADD HL,DE", "Add DE to HL", _ => { Add16BitRegisterTo16BitRegister(_de, ref _hl); });
        AddStandardInstruction(0x29, 11, "ADD HL,HL", "Add HL to HL", _ => { Add16BitRegisterTo16BitRegister(_hl, ref _hl); });
        AddStandardInstruction(0x39, 11, "ADD HL,SP", "Add SP to HL", _ => { Add16BitRegisterTo16BitRegister(_stack.AsRegister(), ref _hl); });
        AddDoubleByteInstruction(0xDD, 0x19, 15, "ADD IX,DE", "Add DE to IX", _ => { Add16BitRegisterTo16BitRegister(_de, ref _ix); });
        AddDoubleByteInstruction(0xDD, 0x29, 15, "ADD IX,IX", "Add IX to IX", _ => { Add16BitRegisterTo16BitRegister(_ix, ref _ix); });
        AddDoubleByteInstruction(0xDD, 0x39, 15, "ADD IX,SP", "Add SP to IX", _ => { Add16BitRegisterTo16BitRegister(_stack.AsRegister(), ref _ix); });
        AddDoubleByteInstruction(0xDD, 0x09, 15, "ADD IX,BC", "Add BC to IX", _ => { Add16BitRegisterTo16BitRegister(_bc, ref _ix); });

        AddDoubleByteInstruction(0xFD, 0x09, 15, "ADD IY,BC", "Add BC to IY", _ => { Add16BitRegisterTo16BitRegister(_bc, ref _iy); });
        AddDoubleByteInstruction(0xFD, 0x19, 15, "ADD IY,DE", "Add DE to IY", _ => { Add16BitRegisterTo16BitRegister(_de, ref _iy); });
        AddDoubleByteInstruction(0xFD, 0x29, 15, "ADD IY,IY", "Add IY to IY", _ => { Add16BitRegisterTo16BitRegister(_iy, ref _iy); });
        AddDoubleByteInstruction(0xFD, 0x39, 15, "ADD IY,SP", "Add SP to IY", _ => { Add16BitRegisterTo16BitRegister(_stack.AsRegister(), ref _iy); });

        AddStandardInstruction(0x86, 4, "ADD A,(HL)", "Add Data at memory location from HL to A", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_hl, 0, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x86, 19, "ADD A,(IX+d)", "Add Data at memory location from IX + d to A", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_ix, _pc.GetNextDataByte(), ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x86, 19, "ADD A,(IY+d)", "Add Data at memory location from IY + d to A", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_iy, _pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0x8F, 4, "ADC A, A", "Add A to A with Carry", _ => { AddValueTo8BitRegister(_af.High, ref _af.High, true); });
        AddStandardInstruction(0x88, 4, "ADC B, B", "Add B to A with Carry", _ => { AddValueTo8BitRegister(_bc.High, ref _af.High, true); });
        AddStandardInstruction(0x89, 4, "ADC C, C", "Add C to A with Carry", _ => { AddValueTo8BitRegister(_bc.Low, ref _af.High, true); });
        AddStandardInstruction(0x8A, 4, "ADC D, D", "Add D to A with Carry", _ => { AddValueTo8BitRegister(_de.High, ref _af.High, true); });
        AddStandardInstruction(0x8B, 4, "ADC E, E", "Add E to A with Carry", _ => { AddValueTo8BitRegister(_de.Low, ref _af.High, true); });
        AddStandardInstruction(0x8C, 4, "ADC H, H", "Add H to A with Carry", _ => { AddValueTo8BitRegister(_hl.High, ref _af.High, true); });
        AddStandardInstruction(0x8D, 4, "ADC L, L", "Add L to A with Carry", _ => { AddValueTo8BitRegister(_hl.Low, ref _af.High, true); });
        AddStandardInstruction(0xCE, 7, "ADC A, N", "Add n to A with Carry", _ => { AddValueTo8BitRegister(_pc.GetNextDataByte(), ref _af.High, true); });

        AddStandardInstruction(0x8E, 4, "ADC (HL)", "Add (HL) to A with Carry", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_hl, 0, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0x8E, 19, "ADC A,(IX+d)", "Add (IX+d) to A with Carry", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_ix, _pc.GetNextDataByte(), ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x8E, 19, "ADC A,(IY+d)", "Add (IX+d) to A with Carry", _ => { AddValueAtRegisterMemoryLocationTo8BitRegister(_iy, _pc.GetNextDataByte(), ref _af.High, true); });
        
        AddDoubleByteInstruction(0xED, 0x4A, 15, "ADC HL,BC", "Add BC to HL with Carry", _ => { Add16BitRegisterTo16BitRegister(_bc, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x5A, 15, "ADC HL,DE", "Add DE to HL with Carry", _ => { Add16BitRegisterTo16BitRegister(_de, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x6A, 15, "ADC HL,HL", "Add HL to HL with Carry", _ => { Add16BitRegisterTo16BitRegister(_hl, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x7A, 15, "ADC HL,SP", "Add SP to HL with Carry", _ => { Add16BitRegisterTo16BitRegister(_stack.AsRegister(), ref _hl, true); });

        AddStandardInstruction(0x97, 4, "SUB A, A", "Subtract A from A", _ => { SubtractValueFrom8BitRegister(_af.High, ref _af.High); });
        AddStandardInstruction(0x90, 4, "SUB A, B", "Subtract B from A", _ => { SubtractValueFrom8BitRegister(_bc.High, ref _af.High); });
        AddStandardInstruction(0x91, 4, "SUB A, C", "Subtract C from A", _ => { SubtractValueFrom8BitRegister(_bc.Low, ref _af.High); });
        AddStandardInstruction(0x92, 4, "SUB A, D", "Subtract D from A", _ => { SubtractValueFrom8BitRegister(_de.High, ref _af.High); });
        AddStandardInstruction(0x93, 4, "SUB A, E", "Subtract E from A", _ => { SubtractValueFrom8BitRegister(_de.Low, ref _af.High); });
        AddStandardInstruction(0x94, 4, "SUB A, H", "Subtract H from A", _ => { SubtractValueFrom8BitRegister(_hl.High, ref _af.High); });
        AddStandardInstruction(0x95, 4, "SUB A, L", "Subtract L from A", _ => { SubtractValueFrom8BitRegister(_hl.Low, ref _af.High); });
        AddStandardInstruction(0xD6, 7, "SUB A, N", "Subtract n from A", _ => { SubtractValueFrom8BitRegister(_pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0x96, 7, "SUB (HL)", "Subtract Data at memory location HL from A", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_hl, 0, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x96, 19, "SUB (IX+d)", "Subtract Data at memory location IX + d from A", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_ix, _pc.GetNextDataByte(), ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x96, 19, "SUB (IY+d)", "Subtract Data at memory location IY + d from A", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_iy, _pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0x9F, 4, "SBC A, A", "Subtract A from A with Carry", _ => { SubtractValueFrom8BitRegister(_af.High, ref _af.High, true); });
        AddStandardInstruction(0x98, 4, "SBC A, B", "Subtract B from A with Carry", _ => { SubtractValueFrom8BitRegister(_bc.High, ref _af.High, true); });
        AddStandardInstruction(0x99, 4, "SBC A, C", "Subtract C from A with Carry", _ => { SubtractValueFrom8BitRegister(_bc.Low, ref _af.High, true); });
        AddStandardInstruction(0x9A, 4, "SBC A, D", "Subtract D from A with Carry", _ => { SubtractValueFrom8BitRegister(_de.High, ref _af.High, true); });
        AddStandardInstruction(0x9B, 4, "SBC A, E", "Subtract E from A with Carry", _ => { SubtractValueFrom8BitRegister(_de.Low, ref _af.High, true); });
        AddStandardInstruction(0x9C, 4, "SBC A, H", "Subtract H from A with Carry", _ => { SubtractValueFrom8BitRegister(_hl.High, ref _af.High, true); });
        AddStandardInstruction(0x9D, 4, "SBC A, L", "Subtract L from A with Carry", _ => { SubtractValueFrom8BitRegister(_hl.Low, ref _af.High, true); });
        AddStandardInstruction(0xDE, 7, "SBC A,N", "Subtract N from A with Carry", _ => { SubtractValueFrom8BitRegister(_pc.GetNextDataByte(), ref _af.High, true); });

        AddStandardInstruction(0x9E, 4, "SBC (HL)", "Subtract (HL) from A with Carry", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_hl, 0, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0x9E, 19, "SBC A,(IX+d)", "Subtract (IX+d) from A with Carry", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_ix, _pc.GetNextDataByte(), ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x9E, 19, "SBC A,(IY+d)", "Subtract (Iy+d) from A with Carry", _ => { SubtractValueAtRegisterMemoryLocationFrom8BitRegister(_iy, _pc.GetNextDataByte(), ref _af.High, true); });

        AddDoubleByteInstruction(0xED, 0x42, 15, "SBC HL,BC", "Subtract with Carry", _ => { Sub16BitRegisterFrom16BitRegister(_bc, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x52, 15, "SBC HL,DE", "Subtract with Carry", _ => { Sub16BitRegisterFrom16BitRegister(_de, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x62, 15, "SBC HL,HL", "Subtract with Carry", _ => { Sub16BitRegisterFrom16BitRegister(_hl, ref _hl, true); });
        AddDoubleByteInstruction(0xED, 0x72, 15, "SBC HL,SP", "Subtract with Carry", _ => { Sub16BitRegisterFrom16BitRegister(_stack.AsRegister(), ref _hl, true); });

        AddStandardInstruction(0xBF, 4, "CP A", "Compare A to A", _ => { Compare8Bit(_af.High, _af.High); });
        AddStandardInstruction(0xB8, 4, "CP B", "Compare B to A", _ => { Compare8Bit(_bc.High, _af.High); });
        AddStandardInstruction(0xB9, 4, "CP C", "Compare C to A", _ => { Compare8Bit(_bc.Low, _af.High); });
        AddStandardInstruction(0xBA, 4, "CP D", "Compare D to A", _ => { Compare8Bit(_de.High, _af.High); });
        AddStandardInstruction(0xBB, 4, "CP E", "Compare E to A", _ => { Compare8Bit(_de.Low, _af.High); });
        AddStandardInstruction(0xBC, 4, "CP H", "Compare H to A", _ => { Compare8Bit(_hl.High, _af.High); });
        AddStandardInstruction(0xBD, 4, "CP L", "Compare L to A", _ => { Compare8Bit(_hl.Low, _af.High); });
        AddStandardInstruction(0xBE, 7, "CP (HL)", "Compare Data at memory location in (HL) to A", _ => { Compare8BitToMemoryLocationFrom16BitRegister(_hl, 0, _af.High); });

        AddStandardInstruction(0xFE, 7, "CP N", "Compare value N to A", _ => { Compare8Bit(_pc.GetNextDataByte(), _af.High); });
        AddDoubleByteInstruction(0xDD, 0xBE, 19, "CP (IX+d)", "Compare Data at memory location in (IX + d) to A", _ => { Compare8BitToMemoryLocationFrom16BitRegister(_ix, _pc.GetNextDataByte(), _af.High); });
        AddDoubleByteInstruction(0xFD, 0xBE, 19, "CP (IY+d)", "Compare Data at memory location in (IY + d) to A", _ => { Compare8BitToMemoryLocationFrom16BitRegister(_iy, _pc.GetNextDataByte(), _af.High); });

        AddDoubleByteInstruction(0xED, 0xA1, 16, "CPI", "Compare and Increment", _ => { CompareIncrement(); });
        AddDoubleByteInstruction(0xED, 0xB1, DynamicCycleHandling, "CPIR", "Compare, Increment, Repeat", _ =>
        {
            //if (_bc.Word == 0)
            //{
            //    // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
            //    //_bc.Word = 64 * 1024;

            //    _currentCycleCount += 16;
            //    return;
            //}

            CompareIncrement();

            if (_bc.Value != 0 && !_flags.IsFlagSet(Z80StatusFlags.ZeroZ))
            {
                // If BC not zero and A != (HL), set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                var currentPc = _pc.Value;
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddDoubleByteInstruction(0xED, 0xA9, 16, "CPD", "Compare and Decrement", _ => { CompareDecrement(); });
        AddDoubleByteInstruction(0xED, 0xB9, 21 / 16, "CPDR", "Compare, Decrement, Repeat", _ =>
        {
            //if (_bc.Word == 0)
            //{
            //    // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
            //    //_bc.Word = 64 * 1024;

            //    _currentCycleCount += 16;
            //    return;
            //}

            CompareDecrement();

            if (_bc.Value != 0 && !_flags.IsFlagSet(Z80StatusFlags.ZeroZ))
            {
                // If BC not zero and A != (HL), set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddStandardInstruction(0xA7, 4, "AND A", "AND A with A and Store in A", _ => { And8Bit(_af.High, _af.High, ref _af.High); });
        AddStandardInstruction(0xA0, 4, "AND B", "AND A with B and Store in A", _ => { And8Bit(_af.High, _bc.High, ref _af.High); });
        AddStandardInstruction(0xA1, 4, "AND C", "AND A with C and Store in A", _ => { And8Bit(_af.High, _bc.Low, ref _af.High); });
        AddStandardInstruction(0xA2, 4, "AND D", "AND A with D and Store in A", _ => { And8Bit(_af.High, _de.High, ref _af.High); });
        AddStandardInstruction(0xA3, 4, "AND E", "AND A with E and Store in A", _ => { And8Bit(_af.High, _de.Low, ref _af.High); });
        AddStandardInstruction(0xA4, 4, "AND H", "AND A with H and Store in A", _ => { And8Bit(_af.High, _hl.High, ref _af.High); });
        AddStandardInstruction(0xA5, 4, "AND L", "AND A with L and Store in A", _ => { And8Bit(_af.High, _hl.Low, ref _af.High); });

        AddStandardInstruction(0xE6, 7, "AND N", "AND A with N and Store in A", _ => { And8Bit(_af.High, _pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0xA6, 7, "AND (HL)", "AND A with data in memory location in (HL) and Store in A", _ => { And8BitToMemoryLocationFrom16BitRegister(_hl, 0, _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xA6, 19, "AND (IX+d)", "AND A with data in memory location in (IX+d) and Store in A", _ => { And8BitToMemoryLocationFrom16BitRegister(_ix, _pc.GetNextDataByte(), _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xA6, 19, "AND (IY+d)", "AND A with data in memory location in (IX+y) and Store in A", _ => { And8BitToMemoryLocationFrom16BitRegister(_iy, _pc.GetNextDataByte(), _af.High, ref _af.High); });

        AddStandardInstruction(0xB7, 4, "OR A", "OR A with A and Store in A", _ => { Or8Bit(_af.High, _af.High, ref _af.High); });
        AddStandardInstruction(0xB0, 4, "OR B", "OR A with B and Store in A", _ => { Or8Bit(_af.High, _bc.High, ref _af.High); });
        AddStandardInstruction(0xB1, 4, "OR C", "OR A with C and Store in A", _ => { Or8Bit(_af.High, _bc.Low, ref _af.High); });
        AddStandardInstruction(0xB2, 4, "OR D", "OR A with D and Store in A", _ => { Or8Bit(_af.High, _de.High, ref _af.High); });
        AddStandardInstruction(0xB3, 4, "OR E", "OR A with E and Store in A", _ => { Or8Bit(_af.High, _de.Low, ref _af.High); });
        AddStandardInstruction(0xB4, 4, "OR H", "OR A with H and Store in A", _ => { Or8Bit(_af.High, _hl.High, ref _af.High); });
        AddStandardInstruction(0xB5, 4, "OR L", "OR A with L and Store in A", _ => { Or8Bit(_af.High, _hl.Low, ref _af.High); });
        AddStandardInstruction(0xF6, 7, "OR N", "Or A with N and Store in A", _ => { Or8Bit(_af.High, _pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0xB6, 7, "OR (HL)", "OR A with data in memory location in (HL) and Store in A", _ => { Or8BitToMemoryLocationFrom16BitRegister(_hl, 0, _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xB6, 19, "OR (IX+d)", "OR A with data in memory location in (IX+d) and Store in A", _ => { Or8BitToMemoryLocationFrom16BitRegister(_ix, _pc.GetNextDataByte(), _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xB6, 19, "OR (IY+d)", "OR A with data in memory location in (IY+d) and Store in A", _ => { Or8BitToMemoryLocationFrom16BitRegister(_iy, _pc.GetNextDataByte(), _af.High, ref _af.High); });

        AddStandardInstruction(0xAF, 4, "XOR A", "XOR A with A and Store in A", _ => { Xor8Bit(_af.High, _af.High, ref _af.High); });
        AddStandardInstruction(0xA8, 4, "XOR B", "XOR A with B and Store in A", _ => { Xor8Bit(_af.High, _bc.High, ref _af.High); });
        AddStandardInstruction(0xA9, 4, "XOR C", "XOR A with C and Store in A", _ => { Xor8Bit(_af.High, _bc.Low, ref _af.High); });
        AddStandardInstruction(0xAA, 4, "XOR D", "XOR A with D and Store in A", _ => { Xor8Bit(_af.High, _de.High, ref _af.High); });
        AddStandardInstruction(0xAB, 4, "XOR E", "XOR A with E and Store in A", _ => { Xor8Bit(_af.High, _de.Low, ref _af.High); });
        AddStandardInstruction(0xAC, 4, "XOR H", "XOR A with H and Store in A", _ => { Xor8Bit(_af.High, _hl.High, ref _af.High); });
        AddStandardInstruction(0xAD, 4, "XOR L", "XOR A with L and Store in A", _ => { Xor8Bit(_af.High, _hl.Low, ref _af.High); });
        AddStandardInstruction(0xEE, 7, "XOR N", "XOr A with N and Store in A", _ => { Xor8Bit(_af.High, _pc.GetNextDataByte(), ref _af.High); });

        AddStandardInstruction(0xAE, 7, "XOR (HL)", "XOR A with data in memory location in (HL) and Store in A", _ => { Xor8BitToMemoryLocationFrom16BitRegister(_hl, 0, _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xAE, 19, "XOR (IX+d)", "XOR A with data in memory location in (IX+d) and Store in A", _ => { Xor8BitToMemoryLocationFrom16BitRegister(_ix, _pc.GetNextDataByte(), _af.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xAE, 19, "XOR (IY+d)", "XOR A with data in memory location in (IY+d) and Store in A", _ => { Xor8BitToMemoryLocationFrom16BitRegister(_iy, _pc.GetNextDataByte(), _af.High, ref _af.High); });

        AddStandardInstruction(0x3C, 4, "INC A", "Increment A", _ => { Increment8Bit(ref _af.High); });
        AddStandardInstruction(0x04, 4, "INC B", "Increment B", _ => { Increment8Bit(ref _bc.High); });
        AddStandardInstruction(0x0C, 4, "INC C", "Increment C", _ => { Increment8Bit(ref _bc.Low); });
        AddStandardInstruction(0x14, 4, "INC D", "Increment D", _ => { Increment8Bit(ref _de.High); });
        AddStandardInstruction(0x1C, 4, "INC E", "Increment E", _ => { Increment8Bit(ref _de.Low); });
        AddStandardInstruction(0x24, 4, "INC H", "Increment H", _ => { Increment8Bit(ref _hl.High); });
        AddStandardInstruction(0x2C, 4, "INC L", "Increment L", _ => { Increment8Bit(ref _hl.Low); });

        AddStandardInstruction(0x3, 6, "INC BC", "Increment BC", _ => { Increment16Bit(ref _bc); });
        AddStandardInstruction(0x13, 6, "INC DE", "Increment DE", _ => { Increment16Bit(ref _de); });
        AddStandardInstruction(0x23, 6, "INC HL", "Increment HL", _ => { Increment16Bit(ref _hl); });
        AddStandardInstruction(0x33, 6, "INC SP", "Increment SP", _ => { _stack.IncrementStackPointer(); });
        AddDoubleByteInstruction(0xDD, 0x23, 10, "INC IX", "Increment", _ => { Increment16Bit(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0x23, 10, "INC IY", "Increment", _ => { Increment16Bit(ref _iy); });

        AddStandardInstruction(0x34, 11, "INC (HL)", "Increment (indirect)", _ => { IncrementMemory(_hl, 0); });
        AddDoubleByteInstruction(0xDD, 0x34, 23, "INC (IX+d)", "Increment", _ => { IncrementMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x34, 23, "INC (IY+d)", "Increment", _ => { IncrementMemory(_iy, _pc.GetNextDataByte()); });

        AddStandardInstruction(0x3D, 4, "DEC A", "Decrement A", _ => { Decrement8Bit(ref _af.High); });
        AddStandardInstruction(0x05, 4, "DEC B", "Decrement B", _ => { Decrement8Bit(ref _bc.High); });
        AddStandardInstruction(0x0D, 4, "DEC C", "Decrement C", _ => { Decrement8Bit(ref _bc.Low); });
        AddStandardInstruction(0x15, 4, "DEC D", "Decrement D", _ => { Decrement8Bit(ref _de.High); });
        AddStandardInstruction(0x1D, 4, "DEC E", "Decrement E", _ => { Decrement8Bit(ref _de.Low); });
        AddStandardInstruction(0x25, 4, "DEC H", "Decrement H", _ => { Decrement8Bit(ref _hl.High); });
        AddStandardInstruction(0x2D, 4, "DEC L", "Decrement L", _ => { Decrement8Bit(ref _hl.Low); });

        AddStandardInstruction(0x0B, 6, "DEC BC", "Decrement BC", _ => { Decrement16Bit(ref _bc); });
        AddStandardInstruction(0x1B, 6, "DEC DE", "Decrement DE", _ => { Decrement16Bit(ref _de); });
        AddStandardInstruction(0x2B, 6, "DEC HL", "Decrement HL", _ => { Decrement16Bit(ref _hl); });
        AddStandardInstruction(0x3B, 6, "DEC SP", "Decrement SP", _ => { _stack.DecrementStackPointer(); });
        AddDoubleByteInstruction(0xDD, 0x2B, 10, "DEC IX", "Decrement IX", _ => { Decrement16Bit(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0x2B, 10, "DEC IY", "Decrement IY", _ => { Decrement16Bit(ref _iy); });

        AddStandardInstruction(0x35, 11, "DEC (HL)", "", _ => { DecrementMemory(_hl); });
        AddDoubleByteInstruction(0xDD, 0x35, 23, "DEC (IX+d)", "Decrement", _ => { DecrementMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x35, 23, "DEC (IY+d)", "Decrement", _ => { DecrementMemory(_iy, _pc.GetNextDataByte()); });

        AddStandardInstruction(0x37, 4, "SCF", "Set Carry Flag", _ =>
        {
            _flags.ClearFlag(Z80StatusFlags.HalfCarryH); 
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN); 
            _flags.SetFlag(Z80StatusFlags.CarryC);
        });
        AddStandardInstruction(0x3F, 4, "CCF", "Complement Carry Flag", _ =>
        {
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
            _flags.SetClearFlagConditional(Z80StatusFlags.HalfCarryH, _flags.IsFlagSet(Z80StatusFlags.CarryC));
            _flags.InvertFlag(Z80StatusFlags.CarryC);
        });
        AddStandardInstruction(0x27, 4, "DAA", "Decimal Adjust Accumulator", _ => { DecimalAdjustAccumulator();  });
        AddStandardInstruction(0X2F, 4, "CPL", "Complement", _ => { InvertAccumulatorRegister(); });
        AddDoubleByteInstruction(0xED, 0x44, 8, "NEG", "Negate", _ => { NegateAccumulatorRegister(); });

        // Undocumented instructions
        AddDoubleByteInstruction(0xDD, 0x84, 4, "ADD A, IXH", "Add IXH to A", _ => { AddValueTo8BitRegister(_ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x85, 4, "ADD A, IXL", "Add IXL to A", _ => { AddValueTo8BitRegister(_ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x84, 4, "ADD A, IYH", "Add IYH to A", _ => { AddValueTo8BitRegister(_iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x85, 4, "ADD A, IYL", "Add IYL to A", _ => { AddValueTo8BitRegister(_iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x8C, 4, "ADC A, IXH", "Add IXH to A with Carry", _ => { AddValueTo8BitRegister(_ix.High, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0x8D, 4, "ADC A, IXL", "Add IXL to A with Carry", _ => { AddValueTo8BitRegister(_ix.Low, ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x8C, 4, "ADC A, IYH", "Add IYH to A with Carry", _ => { AddValueTo8BitRegister(_iy.High, ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x8D, 4, "ADC A, IYL", "Add IYL to A with Carry", _ => { AddValueTo8BitRegister(_iy.Low, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0x94, 4, "SUB A, IXH", "Subtract IXH from A", _ => { SubtractValueFrom8BitRegister(_ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x95, 4, "SUB A, IXH", "Subtract IXL from A", _ => { SubtractValueFrom8BitRegister(_ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x94, 4, "SUB A, IYH", "Subtract IYH from A", _ => { SubtractValueFrom8BitRegister(_iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x95, 4, "SUB A, IYH", "Subtract IYL from A", _ => { SubtractValueFrom8BitRegister(_iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x9C, 4, "SBC A, IXH", "Subtract IXH from A with Carry", _ => { SubtractValueFrom8BitRegister(_ix.High, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0x9D, 4, "SBC A, IXL", "Subtract IXL from A with Carry", _ => { SubtractValueFrom8BitRegister(_ix.Low, ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x9C, 4, "SBC A, IYH", "Subtract IYH from A with Carry", _ => { SubtractValueFrom8BitRegister(_iy.High, ref _af.High, true); });
        AddDoubleByteInstruction(0xFD, 0x9D, 4, "SBC A, IYL", "Subtract IYL from A with Carry", _ => { SubtractValueFrom8BitRegister(_iy.Low, ref _af.High, true); });
        AddDoubleByteInstruction(0xDD, 0xA4, 4, "AND IXH", "AND A with IXH and Store in A", _ => { And8Bit(_af.High, _ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xA5, 4, "AND IXL", "AND A with IXL and Store in A", _ => { And8Bit(_af.High, _ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xA4, 4, "AND IYH", "AND A with IYH and Store in A", _ => { And8Bit(_af.High, _iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xA5, 4, "AND IYL", "AND A with IYL and Store in A", _ => { And8Bit(_af.High, _iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xAC, 4, "XOR IXH", "XOR A with IXH and Store in A", _ => { Xor8Bit(_af.High, _ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xAD, 4, "XOR IXL", "XOR A with IXL and Store in A", _ => { Xor8Bit(_af.High, _ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xAC, 4, "XOR IYH", "XOR A with IYH and Store in A", _ => { Xor8Bit(_af.High, _iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xAD, 4, "XOR IYL", "XOR A with IYL and Store in A", _ => { Xor8Bit(_af.High, _iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xB4, 4, "OR IXH", "OR A with IXH and Store in A", _ => { Or8Bit(_af.High, _ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xB5, 4, "OR IXL", "OR A with IXL and Store in A", _ => { Or8Bit(_af.High, _ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xB4, 4, "OR IYH", "OR A with IYH and Store in A", _ => { Or8Bit(_af.High, _iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0xB5, 4, "OR IYL", "OR A with IYL and Store in A", _ => { Or8Bit(_af.High, _iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0xBC, 4, "CP IXH", "Compare IXH to A", _ => { Compare8Bit(_ix.High, _af.High); });
        AddDoubleByteInstruction(0xDD, 0xBD, 4, "CP IXL", "Compare IXL to A", _ => { Compare8Bit(_ix.Low, _af.High); });
        AddDoubleByteInstruction(0xFD, 0xBC, 4, "CP IYH", "Compare IYH to A", _ => { Compare8Bit(_iy.High, _af.High); });
        AddDoubleByteInstruction(0xFD, 0xBD, 4, "CP IYL", "Compare IYL to A", _ => { Compare8Bit(_iy.Low, _af.High); });

        AddDoubleByteInstruction(0xDD, 0x24, 4, "INC IXH", "Increment IHX", _ => { Increment8Bit(ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x2C, 4, "INC IXL", "Increment IHL", _ => { Increment8Bit(ref _ix.Low); });
        AddDoubleByteInstruction(0xFD, 0x24, 4, "INC IYH", "Increment IYX", _ => { Increment8Bit(ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x2C, 4, "INC IYL", "Increment IYL", _ => { Increment8Bit(ref _iy.Low); });

        AddDoubleByteInstruction(0xDD, 0x25, 4, "DEC IXH", "Decrement IHX", _ => { Decrement8Bit(ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x2D, 4, "DEC IXL", "Decrement IHL", _ => { Decrement8Bit(ref _ix.Low); });
        AddDoubleByteInstruction(0xFD, 0x25, 4, "DEC IYH", "Decrement IYX", _ => { Decrement8Bit(ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x2D, 4, "DEC IYL", "Decrement IYL", _ => { Decrement8Bit(ref _iy.Low); });
    }

    private void PopulateBitSetResetAndTestGroupInstructions()
    {
        AddDoubleByteInstructionWithMask(0xCB, 0x80, 0x3F, 8, "RES b,r", "Reset bit in register", i => { ResetBitByOpCode(i.OpCode); });
        AddSpecialCbInstruction(0xDD, 0x86, 23, "RES 0,(IX+d)", "Reset bit 0 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x8E, 23, "RES 1,(IX+d)", "Reset bit 1 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x96, 23, "RES 2,(IX+d)", "Reset bit 2 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x9E, 23, "RES 3,(IX+d)", "Reset bit 3 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xA6, 23, "RES 4,(IX+d)", "Reset bit 4 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xAE, 23, "RES 5,(IX+d)", "Reset bit 5 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xB6, 23, "RES 6,(IX+d)", "Reset bit 6 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xBE, 23, "RES 7,(IX+d)", "Reset bit 7 in (IX+d)", i => { ResetBitByRegisterLocation(_ix, 7, ((SpecialCbInstruction)i).DataByte); });

        AddSpecialCbInstruction(0xFD, 0x86, 23, "RES 0,(IY+d)", "Reset bit 0 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x8E, 23, "RES 1,(IY+d)", "Reset bit 1 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x96, 23, "RES 2,(IY+d)", "Reset bit 2 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x9E, 23, "RES 3,(IY+d)", "Reset bit 3 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xA6, 23, "RES 4,(IY+d)", "Reset bit 4 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xAE, 23, "RES 5,(IY+d)", "Reset bit 5 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xB6, 23, "RES 6,(IY+d)", "Reset bit 6 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xBE, 23, "RES 7,(IY+d)", "Reset bit 7 in (IY+d)", i => { ResetBitByRegisterLocation(_iy, 7, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstructionWithMask(0xCB, 0x40, 0x3F, 8, "BIT b,r", "Test Bit", i => { TestBitByOpCode(i.OpCode); });

        AddSpecialCbInstruction(0xDD, 0x46, 23, "BIT 0,(IX+d)", "Test bit 0 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x4E, 23, "BIT 1,(IX+d)", "Test bit 1 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x56, 23, "BIT 2,(IX+d)", "Test bit 2 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x5E, 23, "BIT 3,(IX+d)", "Test bit 3 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x66, 23, "BIT 4,(IX+d)", "Test bit 4 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x6E, 23, "BIT 5,(IX+d)", "Test bit 5 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x76, 23, "BIT 6,(IX+d)", "Test bit 6 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0x7E, 23, "BIT 7,(IX+d)", "Test bit 7 in (IX+d)", i => { TestBitByRegisterLocation(_ix, 7, ((SpecialCbInstruction)i).DataByte); });
                                                                                               
        AddSpecialCbInstruction(0xFD, 0x46, 23, "BIT 0,(IY+d)", "Test bit 0 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x4E, 23, "BIT 1,(IY+d)", "Test bit 1 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x56, 23, "BIT 2,(IY+d)", "Test bit 2 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x5E, 23, "BIT 3,(IY+d)", "Test bit 3 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x66, 23, "BIT 4,(IY+d)", "Test bit 4 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x6E, 23, "BIT 5,(IY+d)", "Test bit 5 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x76, 23, "BIT 6,(IY+d)", "Test bit 6 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x7E, 23, "BIT 7,(IY+d)", "Test bit 7 in (IY+d)", i => { TestBitByRegisterLocation(_iy, 7, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstructionWithMask(0xCB, 0xC0, 0x3F, 8, "SET b,r", "Set bit b in Register r", i => { SetBitByOpCode(i.OpCode); });

        AddSpecialCbInstruction(0xDD, 0xC6, 23, "SET 0,(IX+d)", "Set bit 0 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xCE, 23, "SET 1,(IX+d)", "Set bit 1 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xD6, 23, "SET 2,(IX+d)", "Set bit 2 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xDE, 23, "SET 3,(IX+d)", "Set bit 3 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xE6, 23, "SET 4,(IX+d)", "Set bit 4 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xEE, 23, "SET 5,(IX+d)", "Set bit 5 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xF6, 23, "SET 6,(IX+d)", "Set bit 6 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xDD, 0xFE, 23, "SET 7,(IX+d)", "Set bit 7 in (IX+d)", i => { SetBitByRegisterLocation(_ix, 7, ((SpecialCbInstruction)i).DataByte); });

        AddSpecialCbInstruction(0xFD, 0xC6, 23, "SET 0,(IY+d)", "Set bit 0 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 0, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xCE, 23, "SET 1,(IY+d)", "Set bit 1 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 1, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xD6, 23, "SET 2,(IY+d)", "Set bit 2 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 2, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xDE, 23, "SET 3,(IY+d)", "Set bit 3 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 3, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xE6, 23, "SET 4,(IY+d)", "Set bit 4 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 4, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xEE, 23, "SET 5,(IY+d)", "Set bit 5 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 5, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xF6, 23, "SET 6,(IY+d)", "Set bit 6 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 6, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0xFE, 23, "SET 7,(IY+d)", "Set bit 7 in (IY+d)", i => { SetBitByRegisterLocation(_iy, 7, ((SpecialCbInstruction)i).DataByte); });
    }

    private void PopulateRotateAndShiftInstructions()
    {
        AddStandardInstruction(0x17, 4, "RLA", "Rotate Left Accumulator", _ => { RotateLeftAccumulator();});
        AddStandardInstruction(0x07, 4, "RLCA", "Rotate Left Circular Accumulator", _ => { RotateLeftCircularAccumulator(); });
        AddStandardInstruction(0x1F, 4, "RRA", "Rotate Right Accumulator", _ => { RotateRightAccumulator(); });
        AddStandardInstruction(0x0F, 4, "RRCA", "Rotate Right Circular Accumulator", _ => { RotateRightCircularAccumulator(); });

        AddDoubleByteInstruction(0xCB, 0x17, 8, "RL A", "Rotate Left A 1 bit", _ => { RotateLeft(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x10, 8, "RL B", "Rotate Left B 1 bit", _ => { RotateLeft(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x11, 8, "RL C", "Rotate Left C 1 bit", _ => { RotateLeft(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x12, 8, "RL D", "Rotate Left D 1 bit", _ => { RotateLeft(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x13, 8, "RL E", "Rotate Left E 1 bit", _ => { RotateLeft(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x14, 8, "RL H", "Rotate Left H 1 bit", _ => { RotateLeft(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x15, 8, "RL L", "Rotate Left L 1 bit", _ => { RotateLeft(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x16, 8, "RL (HL)", "Rotate Left (HL) 1 bit", _ => { RotateLeft16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x16, 23, "RL (IX+d)", "Rotate Left (IX+d) 1 bit", i => { RotateLeft16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x16, 23, "RL (IY+d)", "Rotate Left (IY+d) 1 bit", i => { RotateLeft16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x07, 8, "RLC A", "Rotate Left Circular A 1 bit", _ => { RotateLeftCircular(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x00, 8, "RLC B", "Rotate Left Circular B 1 bit", _ => { RotateLeftCircular(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x01, 8, "RLC C", "Rotate Left Circular C 1 bit", _ => { RotateLeftCircular(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x02, 8, "RLC D", "Rotate Left Circular D 1 bit", _ => { RotateLeftCircular(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x03, 8, "RLC E", "Rotate Left Circular E 1 bit", _ => { RotateLeftCircular(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x04, 8, "RLC H", "Rotate Left Circular H 1 bit", _ => { RotateLeftCircular(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x05, 8, "RLC L", "Rotate Left Circular L 1 bit", _ => { RotateLeftCircular(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x06, 8, "RLC (HL)", "Rotate Left Circular (HL) 1 bit", _ => { RotateLeftCircular16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x06, 23, "RLC (IX+d)", "Rotate Left Circular (IX+d) 1 bit", i => { RotateLeftCircular16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x06, 23, "RLC (IY+d)", "Rotate Left Circular (IY+d) 1 bit", i => { RotateLeftCircular16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x1F, 8, "RR A", "Rotate Right A 1 bit", _ => { RotateRight(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x18, 8, "RR B", "Rotate Right B 1 bit", _ => { RotateRight(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x19, 8, "RR C", "Rotate Right C 1 bit", _ => { RotateRight(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x1A, 8, "RR D", "Rotate Right D 1 bit", _ => { RotateRight(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x1B, 8, "RR E", "Rotate Right E 1 bit", _ => { RotateRight(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x1C, 8, "RR H", "Rotate Right H 1 bit", _ => { RotateRight(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x1D, 8, "RR L", "Rotate Right L 1 bit", _ => { RotateRight(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x1E, 8, "RR (HL)", "Rotate Right (HL) 1 bit", _ => { RotateRight16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x1E, 23, "RR (IX+d)", "Rotate Right (IX+d) 1 bit", i => { RotateRight16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x1E, 23, "RR (IY+d)", "Rotate Right (IY+d) 1 bit", i => { RotateRight16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x0F, 8, "RRC A", "Rotate Right Circular A 1 bit", _ => { RotateRightCircular(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x08, 8, "RRC B", "Rotate Right Circular B 1 bit", _ => { RotateRightCircular(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x09, 8, "RRC C", "Rotate Right Circular C 1 bit", _ => { RotateRightCircular(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x0A, 8, "RRC D", "Rotate Right Circular D 1 bit", _ => { RotateRightCircular(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x0B, 8, "RRC E", "Rotate Right Circular E 1 bit", _ => { RotateRightCircular(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x0C, 8, "RRC H", "Rotate Right Circular H 1 bit", _ => { RotateRightCircular(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x0D, 8, "RRC L", "Rotate Right Circular L 1 bit", _ => { RotateRightCircular(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x0E, 8, "RRC (HL)", "Rotate Right Circular (HL) 1 bit", _ => { RotateRightCircular16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x0E, 23, "RRC (IX+d)", "Rotate Right Circular (IX+d) 1 bit", i => { RotateRightCircular16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x0E, 23, "RRC (IY+d)", "Rotate Right Circular (IY+d) 1 bit", i => { RotateRightCircular16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x27, 8, "SLA A", "Shift Left Arithmetic A 1 bit", _ => { ShiftLeftArithmetic(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x20, 8, "SLA B", "Shift Left Arithmetic B 1 bit", _ => { ShiftLeftArithmetic(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x21, 8, "SLA C", "Shift Left Arithmetic C 1 bit", _ => { ShiftLeftArithmetic(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x22, 8, "SLA D", "Shift Left Arithmetic D 1 bit", _ => { ShiftLeftArithmetic(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x23, 8, "SLA E", "Shift Left Arithmetic E 1 bit", _ => { ShiftLeftArithmetic(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x24, 8, "SLA H", "Shift Left Arithmetic H 1 bit", _ => { ShiftLeftArithmetic(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x25, 8, "SLA L", "Shift Left Arithmetic L 1 bit", _ => { ShiftLeftArithmetic(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x26, 8, "SLA (HL)", "Shift Left Arithmetic (HL) 1 bit", _ => { ShiftLeftArithmetic16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x26, 23, "SLA (IX+d)", "Shift Left Arithmetic (IX+d) 1 bit", i => { ShiftLeftArithmetic16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x26, 23, "SLA (IY+d)", "Shift Left Arithmetic (IY+d) 1 bit", i => { ShiftLeftArithmetic16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x2F, 8, "SRA A", "Shift Right Arithmetic A 1 bit", _ => { ShiftRightArithmetic(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x28, 8, "SRA B", "Shift Right Arithmetic B 1 bit", _ => { ShiftRightArithmetic(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x29, 8, "SRA C", "Shift Right Arithmetic C 1 bit", _ => { ShiftRightArithmetic(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x2A, 8, "SRA D", "Shift Right Arithmetic D 1 bit", _ => { ShiftRightArithmetic(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x2B, 8, "SRA E", "Shift Right Arithmetic E 1 bit", _ => { ShiftRightArithmetic(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x2C, 8, "SRA H", "Shift Right Arithmetic H 1 bit", _ => { ShiftRightArithmetic(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x2D, 8, "SRA L", "Shift Right Arithmetic L 1 bit", _ => { ShiftRightArithmetic(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x2E, 8, "SRA (HL)", "Shift Right Arithmetic (HL) 1 bit", _ => { ShiftRightArithmetic16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x2E, 23, "SRA (IX+d)", "Shift Right Arithmetic (IX+d) 1 bit", i => { ShiftRightArithmetic16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x2E, 23, "SRA (IY+d)", "Shift Right Arithmetic (IY+d) 1 bit", i => { ShiftRightArithmetic16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        // NOTE: SLL is an undocumented instruction
        AddDoubleByteInstruction(0xCB, 0x37, 8, "SLL A", "Shift Left LogicalA 1 bit", _ => { ShiftLeftLogical(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x30, 8, "SLL B", "Shift Left LogicalB 1 bit", _ => { ShiftLeftLogical(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x31, 8, "SLL C", "Shift Left LogicalC 1 bit", _ => { ShiftLeftLogical(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x32, 8, "SLL D", "Shift Left LogicalD 1 bit", _ => { ShiftLeftLogical(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x33, 8, "SLL E", "Shift Left LogicalE 1 bit", _ => { ShiftLeftLogical(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x34, 8, "SLL H", "Shift Left LogicalH 1 bit", _ => { ShiftLeftLogical(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x35, 8, "SLL L", "Shift Left LogicalL 1 bit", _ => { ShiftLeftLogical(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x36, 8, "SLL (HL)", "Shift Left Logical (HL) 1 bit", _ => { ShiftLeftLogical16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x36, 23, "SLL (IX+d)", "Shift Left Logical (IX+d) 1 bit", i => { ShiftLeftLogical16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x36, 23, "SLL (IY+d)", "Shift Left Logical (IY+d) 1 bit", i => { ShiftLeftLogical16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xCB, 0x3F, 8, "SRL A", "Shift Right Logical A 1 bit", _ => { ShiftRightLogical(ref _af.High); });
        AddDoubleByteInstruction(0xCB, 0x38, 8, "SRL B", "Shift Right Logical B 1 bit", _ => { ShiftRightLogical(ref _bc.High); });
        AddDoubleByteInstruction(0xCB, 0x39, 8, "SRL C", "Shift Right Logical C 1 bit", _ => { ShiftRightLogical(ref _bc.Low); });
        AddDoubleByteInstruction(0xCB, 0x3A, 8, "SRL D", "Shift Right Logical D 1 bit", _ => { ShiftRightLogical(ref _de.High); });
        AddDoubleByteInstruction(0xCB, 0x3B, 8, "SRL E", "Shift Right Logical E 1 bit", _ => { ShiftRightLogical(ref _de.Low); });
        AddDoubleByteInstruction(0xCB, 0x3C, 8, "SRL H", "Shift Right Logical H 1 bit", _ => { ShiftRightLogical(ref _hl.High); });
        AddDoubleByteInstruction(0xCB, 0x3D, 8, "SRL L", "Shift Right Logical L 1 bit", _ => { ShiftRightLogical(ref _hl.Low); });
        AddDoubleByteInstruction(0xCB, 0x3E, 8, "SRL (HL)", "Shift Right Logical (HL) 1 bit", _ => { ShiftRightLogical16BitRegisterMemoryLocation(_hl, 0); });
        AddSpecialCbInstruction(0xDD, 0x3E, 23, "SRL (IX+d)", "Shift Right Logical (IX+d) 1 bit", i => { ShiftRightLogical16BitRegisterMemoryLocation(_ix, ((SpecialCbInstruction)i).DataByte); });
        AddSpecialCbInstruction(0xFD, 0x3E, 23, "SRL (IY+d)", "Shift Right Logical (IY+d) 1 bit", i => { ShiftRightLogical16BitRegisterMemoryLocation(_iy, ((SpecialCbInstruction)i).DataByte); });

        AddDoubleByteInstruction(0xED, 0x6F, 18, "RLD", "Rotate Left 4 bits", _ => { RotateLeftDigit(); });
        AddDoubleByteInstruction(0xED, 0x67, 18, "RRD", "Rotate Right 4 bits", _ => { RotateRightDigit(); });
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
        AddStandardInstructionWithMask(0x70, 5, 7, "LD (HL),r", "Load 8 bit register into (HL)", i => { LoadRR(i.OpCode); });
        // Since 0x76 is a seperate instruction (halt) we can't do a mask using all lower 3 bits so skip 0x76 and add 0x77 manually
        AddStandardInstruction(0x77, 7, "LD (HL),A", "Load A into (HL)", i => { LoadRR(i.OpCode); });

        AddStandardInstruction(0x0A, 7, "LD A,(BC)", "Load A from data in memory location at BC", _ => { _accumulator.SetFromDataInMemory(_bc); });
        AddStandardInstruction(0x1A, 7, "LD A,(DE)", "Load A from data in memory location at DE", _ => { _accumulator.SetFromDataInMemory(_de); });
        AddStandardInstruction(0x2, 7, "LD (BC),A", "Load A to memory at BC", _ => { _accumulator.SaveToMemory(_bc); });
        AddStandardInstruction(0x12, 7, "LD (DE),A", "Load A to memory at DE", _ => { _accumulator.SaveToMemory(_de); });

        AddDoubleByteInstruction(0xDD, 0x77, 19, "LD (IX+d),A", "Load A into memory location at IX+D", _ => { _accumulator.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x70, 19, "LD (IX+d),B", "Load B into memory location at IX+D", _ => { _b.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x71, 19, "LD (IX+d),C", "Load C into memory location at IX+D", _ => { _c.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x72, 19, "LD (IX+d),D", "Load D into memory location at IX+D", _ => { _d.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x73, 19, "LD (IX+d),E", "Load E into memory location at IX+D", _ => { _e.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x74, 19, "LD (IX+d),H", "Load H into memory location at IX+D", _ => { _h.SaveToMemory(_ix, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x75, 19, "LD (IX+d),L", "Load L into memory location at IX+D", _ => { _l.SaveToMemory(_ix, _pc.GetNextDataByte()); });

        AddDoubleByteInstruction(0xFD, 0x77, 19, "LD (IY+d),A", "Load A into memory location at IY+D", _ => { _accumulator.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x70, 19, "LD (IY+d),B", "Load B into memory location at IY+D", _ => { _b.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x71, 19, "LD (IY+d),C", "Load C into memory location at IY+D", _ => { _c.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x72, 19, "LD (IY+d),D", "Load D into memory location at IY+D", _ => { _d.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x73, 19, "LD (IY+d),E", "Load E into memory location at IY+D", _ => { _e.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x74, 19, "LD (IY+d),H", "Load H into memory location at IY+D", _ => { _h.SaveToMemory(_iy, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x75, 19, "LD (IY+d),L", "Load L into memory location at IY+D", _ => { _l.SaveToMemory(_iy, _pc.GetNextDataByte()); });

        AddStandardInstruction(0xF9, 6, "LD SP,HL", "Load HL into SP", _ => { _stack.Set(_hl); });
        AddDoubleByteInstruction(0xDD, 0xF9, 10, "LD SP,IX", "Load IX into SP", _ => { _stack.Set(_ix); });
        AddDoubleByteInstruction(0xFD, 0xF9, 10, "LD SP,IY", "Load IY into SP", _ => { _stack.Set(_iy); });

        AddDoubleByteInstruction(0xED, 0x47, 9, "LD I,A", "Load A into I", _ => { _iRegister.Set(_accumulator); });
        AddDoubleByteInstruction(0xED, 0x4F, 9, "LD R,A", "Load A into R", _ => { _rRegister.Set(_accumulator); });
        AddDoubleByteInstruction(0xED, 0x57, 9, "LD A,I", "Load I into A", _ => { _accumulator.SetFromInterruptRegister(_iRegister, _interruptFlipFlop2); });
        AddDoubleByteInstruction(0xED, 0x5F, 9, "LD A,R", "Load R into A", _ => { _accumulator.SetFromMemoryRefreshRegister(_rRegister, _interruptFlipFlop2); });
        AddStandardInstruction(0x3E, 7, "LD A,N", "Load n into A", _ => { _accumulator.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x06, 7, "LD B,N", "Load n into B", _ => { _b.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x0E, 7, "LD C,N", "Load n into C", _ => { _c.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x16, 7, "LD D,N", "Load n into D", _ => { _d.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x1E, 7, "LD E,N", "Load n into E", _ => { _e.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x26, 7, "LD H,N", "Load n into H", _ => { _h.Set(_pc.GetNextDataByte()); });
        AddStandardInstruction(0x2E, 7, "LD L,N", "Load n into L", _ => { _l.Set(_pc.GetNextDataByte()); });

        // These are undocumented but zxdoc still runs them
        // https://iot.onl/asm/z80/opcodes/undocumented/
        // DD & FD
        // Officially the 0xDD and 0xFD prefixes cause any instruction that references(HL) to instead work against the IX &IY registers with a displacement, 0xDD for IX and 0xFD for IY.
        // The undocumented instructions allows for instructions that refer to just H or L can also be used to access the upper or lower 8 - bit components of IX and IY themselves.

        // These first ones are copies of existing instructions but with a DD/FD prefix
        AddDoubleByteInstructionWithMask(0xDD, 0x78, 3, 4, "LD A,r", "Load 8-bit register into A", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xDD, 0x40, 3, 4, "LD B,r", "Load 8-bit register into B", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xDD, 0x48, 3, 4, "LD C,r", "Load 8-bit register into C", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xDD, 0x50, 3, 4, "LD D,r", "Load 8-bit register into D", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xDD, 0x58, 3, 4, "LD E,r", "Load 8-bit register into E", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xFD, 0x78, 3, 4, "LD A,r", "Load 8-bit register into A", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xFD, 0x40, 3, 4, "LD B,r", "Load 8-bit register into B", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xFD, 0x48, 3, 4, "LD C,r", "Load 8-bit register into C", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xFD, 0x50, 3, 4, "LD D,r", "Load 8-bit register into D", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstructionWithMask(0xFD, 0x58, 3, 4, "LD E,r", "Load 8-bit register into E", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xDD, 0x7F, 4, "LD A,A", "Load 8-bit register into A", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xDD, 0x47, 4, "LD A,B", "Load 8-bit register into B", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xDD, 0x4F, 4, "LD A,C", "Load 8-bit register into C", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xDD, 0x57, 4, "LD A,D", "Load 8-bit register into D", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xDD, 0x5F, 4, "LD A,E", "Load 8-bit register into E", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xFD, 0x7F, 4, "LD A,A", "Load 8-bit register into A", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xFD, 0x47, 4, "LD A,B", "Load 8-bit register into B", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xFD, 0x4F, 4, "LD A,C", "Load 8-bit register into C", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xFD, 0x57, 4, "LD A,D", "Load 8-bit register into D", i => { LoadRR(i.OpCode); });
        AddDoubleByteInstruction(0xFD, 0x5F, 4, "LD A,E", "Load 8-bit register into E", i => { LoadRR(i.OpCode); });
        
        // These are undocumented instructions which operate on the IX/IY high and low nibbles
        AddDoubleByteInstruction(0xDD, 0x26, 7, "LD IXH,N", "Load n into IX high", _ => { LoadValueInto8BitRegister(ref _ix.High, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x2E, 7, "LD IXL,N", "Load n into IX low", _ => { LoadValueInto8BitRegister(ref _ix.Low, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x26, 7, "LD IYH,N", "Load n into IX high", _ => { LoadValueInto8BitRegister(ref _iy.High, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x2E, 7, "LD IYL,N", "Load n into IX low", _ => { LoadValueInto8BitRegister(ref _iy.Low, _pc.GetNextDataByte()); });

        AddDoubleByteInstruction(0xDD, 0x7C, 9, "LD A, IXH", "Load IXH into A", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x44, 9, "LD B, IXH", "Load IXH into B", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _bc.High); });
        AddDoubleByteInstruction(0xDD, 0x4C, 9, "LD C, IXH", "Load IXH into C", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _bc.Low); });
        AddDoubleByteInstruction(0xDD, 0x54, 9, "LD D, IXH", "Load IXH into D", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _de.High); });
        AddDoubleByteInstruction(0xDD, 0x5C, 9, "LD E, IXH", "Load IXH into E", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _de.Low); });
        AddDoubleByteInstruction(0xDD, 0x7D, 9, "LD A, IXL", "Load IXL into A", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _af.High); });
        AddDoubleByteInstruction(0xDD, 0x45, 9, "LD B, IXL", "Load IXL into B", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _bc.High); });
        AddDoubleByteInstruction(0xDD, 0x4D, 9, "LD C, IXL", "Load IXL into C", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _bc.Low); });
        AddDoubleByteInstruction(0xDD, 0x55, 9, "LD D, IXL", "Load IXL into D", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _de.High); });
        AddDoubleByteInstruction(0xDD, 0x5D, 9, "LD E, IXL", "Load IXL into E", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _de.Low); });

        AddDoubleByteInstruction(0xFD, 0x7C, 9, "LD A, IYH", "Load IYH into A", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x44, 9, "LD B, IYH", "Load IYH into B", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _bc.High); });
        AddDoubleByteInstruction(0xFD, 0x4C, 9, "LD C, IYH", "Load IYH into C", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _bc.Low); });
        AddDoubleByteInstruction(0xFD, 0x54, 9, "LD D, IYH", "Load IYH into D", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _de.High); });
        AddDoubleByteInstruction(0xFD, 0x5C, 9, "LD E, IYH", "Load IYH into E", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _de.Low); });
        AddDoubleByteInstruction(0xFD, 0x7D, 9, "LD A, IYL", "Load IYL into A", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _af.High); });
        AddDoubleByteInstruction(0xFD, 0x45, 9, "LD B, IYL", "Load IYL into B", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _bc.High); });
        AddDoubleByteInstruction(0xFD, 0x4D, 9, "LD C, IYL", "Load IYL into C", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _bc.Low); });
        AddDoubleByteInstruction(0xFD, 0x55, 9, "LD D, IYL", "Load IYL into D", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _de.High); });
        AddDoubleByteInstruction(0xFD, 0x5D, 9, "LD E, IYL", "Load IYL into E", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _de.Low); });

        AddDoubleByteInstruction(0xDD, 0x67, 9, "LD IXH, A", "Load A into IXH", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x60, 9, "LD IXH, B", "Load B into IXH", _ => { Load8BitRegisterFrom8BitRegister(_bc.High, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x61, 9, "LD IXH, C", "Load C into IXH", _ => { Load8BitRegisterFrom8BitRegister(_bc.Low, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x62, 9, "LD IXH, D", "Load D into IXH", _ => { Load8BitRegisterFrom8BitRegister(_de.High, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x63, 9, "LD IXH, E", "Load E into IXH", _ => { Load8BitRegisterFrom8BitRegister(_de.Low, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x64, 9, "LD IXH, IXH", "Load IXH into IXH", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _ix.High); });
        AddDoubleByteInstruction(0xDD, 0x65, 9, "LD IXH, IXL", "Load IXL into IXH", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _ix.High); });

        AddDoubleByteInstruction(0xDD, 0x6F, 9, "LD IXL, A", "Load A into IXL", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x68, 9, "LD IXL, B", "Load B into IXL", _ => { Load8BitRegisterFrom8BitRegister(_bc.High, ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x69, 9, "LD IXL, C", "Load C into IXL", _ => { Load8BitRegisterFrom8BitRegister(_bc.Low , ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x6A, 9, "LD IXL, D", "Load D into IXL", _ => { Load8BitRegisterFrom8BitRegister(_de.High, ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x6B, 9, "LD IXL, E", "Load E into IXL", _ => { Load8BitRegisterFrom8BitRegister(_de.Low , ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x6C, 9, "LD IXL, IXH", "Load IXH into IXL", _ => { Load8BitRegisterFrom8BitRegister(_ix.High, ref _ix.Low); });
        AddDoubleByteInstruction(0xDD, 0x6D, 9, "LD IXL, IXL", "Load IXL into IXL", _ => { Load8BitRegisterFrom8BitRegister(_ix.Low, ref _ix.Low); });

        AddDoubleByteInstruction(0xFD, 0x67, 9, "LD IYH, A", "Load A into IYH", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x60, 9, "LD IYH, B", "Load B into IYH", _ => { Load8BitRegisterFrom8BitRegister(_bc.High, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x61, 9, "LD IYH, C", "Load C into IYH", _ => { Load8BitRegisterFrom8BitRegister(_bc.Low, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x62, 9, "LD IYH, D", "Load D into IYH", _ => { Load8BitRegisterFrom8BitRegister(_de.High, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x63, 9, "LD IYH, E", "Load E into IYH", _ => { Load8BitRegisterFrom8BitRegister(_de.Low, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x64, 9, "LD IYH, IYH", "Load IYH into IYH", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _iy.High); });
        AddDoubleByteInstruction(0xFD, 0x65, 9, "LD IYH, IYL", "Load IYL into IYH", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _iy.High); });

        AddDoubleByteInstruction(0xFD, 0x6F, 9, "LD IYL, A", "Load A into IYL", _ => { Load8BitRegisterFrom8BitRegister(_af.High, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x68, 9, "LD IYL, B", "Load B into IYL", _ => { Load8BitRegisterFrom8BitRegister(_bc.High, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x69, 9, "LD IYL, C", "Load C into IYL", _ => { Load8BitRegisterFrom8BitRegister(_bc.Low, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x6A, 9, "LD IYL, D", "Load D into IYL", _ => { Load8BitRegisterFrom8BitRegister(_de.High, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x6B, 9, "LD IYL, E", "Load E into IYL", _ => { Load8BitRegisterFrom8BitRegister(_de.Low, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x6C, 9, "LD IYL, IYH", "Load IYH into IYL", _ => { Load8BitRegisterFrom8BitRegister(_iy.High, ref _iy.Low); });
        AddDoubleByteInstruction(0xFD, 0x6D, 9, "LD IYL, IYL", "Load IYL into IYL", _ => { Load8BitRegisterFrom8BitRegister(_iy.Low, ref _iy.Low); });
        // end undocumented instructions

        AddDoubleByteInstruction(0xDD, 0x7E, 19, "LD A,(IX+d)", "Load memory at IX + d into A", _ => { _accumulator.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x46, 19, "LD B,(IX+d)", "Load memory at IX + d into B", _ => { _b.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x4E, 19, "LD C,(IX+d)", "Load memory at IX + d into C", _ => { _c.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x56, 19, "LD D,(IX+d)", "Load memory at IX + d into D", _ => { _d.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x5E, 19, "LD E,(IX+d)", "Load memory at IX + d into E", _ => { _e.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x66, 19, "LD H,(IX+d)", "Load memory at IX + d into H", _ => { _h.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x6E, 19, "LD L,(IX+d)", "Load memory at IX + d into L", _ => { _l.SetFromDataInMemory(_ix.Value, _pc.GetNextDataByte()); });

        AddDoubleByteInstruction(0xFD, 0x7E, 19, "LD A,(IY+d)", "Load memory at IY + d into A", _ => { _accumulator.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte());});
        AddDoubleByteInstruction(0xFD, 0x46, 19, "LD B,(IY+d)", "Load memory at IY + d into B", _ => { _b.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte());});
        AddDoubleByteInstruction(0xFD, 0x4E, 19, "LD C,(IY+d)", "Load memory at IY + d into C", _ => { _c.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x56, 19, "LD D,(IY+d)", "Load memory at IY + d into D", _ => { _d.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x5E, 19, "LD E,(IY+d)", "Load memory at IY + d into E", _ => { _e.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x66, 19, "LD H,(IY+d)", "Load memory at IY + d into H", _ => { _h.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xFD, 0x6E, 19, "LD L,(IY+d)", "Load memory at IY + d into L", _ => { _l.SetFromDataInMemory(_iy.Value, _pc.GetNextDataByte()); });

        AddStandardInstruction(0x36, 10, "LD (HL),N", "Load value n into memory at HL", _ => { WriteToMemory(_hl, _pc.GetNextDataByte()); });
        AddDoubleByteInstruction(0xDD, 0x36, 19, "LD(IX + d), N", "Load value n into location at IX + d", _ =>  {  var offset = _pc.GetNextDataByte();  var value = _pc.GetNextDataByte(); WriteToMemory(_ix, value, offset); });
        AddDoubleByteInstruction(0xFD, 0x36, 19, "LD(IY + d), N", "Load value n into location at IY + d", _ => { var offset = _pc.GetNextDataByte(); var value = _pc.GetNextDataByte(); WriteToMemory(_iy, value, offset); });

        AddStandardInstruction(0x3A, 13, "LD A,(NN)", "Load value at memory location NN into A", _ => { _accumulator.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0x2A, 16, "LD HL,(NN)", "Load value at memory location NN into HL", _ => { _hl.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x4B, 20, "LD BC, (NN)", "Load value at memory location NN into BC", _ => { _bc.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x5B, 20, "LD DE, (NN)", "Load value at memory location NN into DE", _ => { _de.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x7B, 20, "LD SP, (NN)", "Load value at memory location NN into SP", _ => { _stack.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xDD, 0x2A, 20, "LD IX, (NN)", "Load value at memory location NN into IX", _ => { _ix.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xFD, 0x2A, 20, "LD IY, (NN)", "Load value at memory location NN into IY", _ => { _iy.SetFromDataInMemory(_pc.GetNextTwoDataBytes()); });

        AddStandardInstruction(0x01, 10, "LD BC,NN", "Load nn value into BC", _ => { _bc.Set(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0x11, 10, "LD DE,NN", "Load nn value into DE", _ => { _de.Set(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0x21, 10, "LD HL,NN", "Load nn value into HL", _ => { _hl.Set(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0x31, 10, "LD SP,NN", "Load nn value into SP", _ => { _stack.Set(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xDD, 0x21, 14, "LD IX, NN", "Load nn value into IX", _ => { _ix.Set(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xFD, 0x21, 14, "LD IY, NN", "Load nn value into IY", _ => { _iy.Set(_pc.GetNextTwoDataBytes()); });

        AddStandardInstruction(0x32, 13, "LD (NN),A", "Load A into memory location NN", _ => { _accumulator.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddStandardInstruction(0x22, 16, "LD (NN),HL", "Load HL into memory location NN", _ => { _hl.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x43, 20, "LD(NN), BC", "Load BC into memory location NN", _ => { _bc.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x53, 20, "LD(NN), DE", "Load DE into memory location NN", _ => { _de.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xDD, 0x22, 20, "LD(NN), IX", "Load IX into memory location NN", _ => { _ix.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xFD, 0x22, 20, "LD(NN), IY", "Load IY into memory location NN", _ => { _iy.SaveToMemory(_pc.GetNextTwoDataBytes()); });
        AddDoubleByteInstruction(0xED, 0x73, 20, "LD(NN), SP", "Load SP into memory location NN", _ => { _stack.SaveToMemory(_pc.GetNextTwoDataBytes()); });

        AddDoubleByteInstruction(0xED, 0xA0, 16, "LDI", "Load and Increment", _ =>
        {
            CopyMemory(_hl, _de);
            Increment16Bit(ref _hl);
            Increment16Bit(ref _de);
            Decrement16Bit(ref _bc);
            _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Value != 0);
        });
        AddDoubleByteInstruction(0xED, 0xB0, DynamicCycleHandling, "LDIR", "Load, Increment, Repeat", _ =>
        {
            if (_bc.Value == 0)
            {
                // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
                //_bc.Word = 64 * 1024;

                _currentCycleCount += 16;
                return;
            }

            CopyMemory(_hl, _de);
            Increment16Bit(ref _hl);
            Increment16Bit(ref _de);
            Decrement16Bit(ref _bc);
            _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Value != 0);

            if (_bc.Value != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });
        AddDoubleByteInstruction(0xED, 0xA8, 16, "LDD", "Load and Decrement", _ =>
        {
            CopyMemory(_hl, _de);
            Decrement16Bit(ref _hl);
            Decrement16Bit(ref _de);
            Decrement16Bit(ref _bc);
            _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
            _flags.SetClearFlagConditional(Z80StatusFlags.ParityOverflowPV, _bc.Value != 0);
        });
        AddDoubleByteInstruction(0xED, 0xB8, DynamicCycleHandling, "LDDR", "Load, Decrement, Repeat", _ =>
        {
            if (_bc.Value == 0)
            {
                // BC was set to 0 before instruction was executed, so set to 64kb accordingly to documentation but no emulator does this
                //_bc.Word = 64 * 1024;

                _currentCycleCount += 16;
                return;
            }

            CopyMemory(_hl, _de);
            Decrement16Bit(ref _hl);
            Decrement16Bit(ref _de);
            Decrement16Bit(ref _bc);
            _flags.ClearFlag(Z80StatusFlags.HalfCarryH);
            _flags.ClearFlag(Z80StatusFlags.AddSubtractN);
            // This is not a typo, for some reason in LDDR the PV flag is reset unlike the other LDx instructions
            _flags.ClearFlag(Z80StatusFlags.ParityOverflowPV);

            if (_bc.Value != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddStandardInstruction(0xF5, 11, "PUSH AF", "Push AF", _ => { _stack.PushRegisterToStack(_af); });
        AddStandardInstruction(0xC5, 11, "PUSH BC", "Push BC", _ => { _stack.PushRegisterToStack(_bc); });
        AddStandardInstruction(0xD5, 11, "PUSH DE", "Push DE", _ => { _stack.PushRegisterToStack(_de); });
        AddStandardInstruction(0xE5, 11, "PUSH HL", "Push HL", _ => { _stack.PushRegisterToStack(_hl); });
        AddDoubleByteInstruction(0xDD, 0xE5, 15, "PUSH IX", "Push IX", _ => { _stack.PushRegisterToStack(_ix); });
        AddDoubleByteInstruction(0xFD, 0xE5, 15, "PUSH IY", "Push IY", _ => { _stack.PushRegisterToStack(_iy); });

        AddStandardInstruction(0xF1, 10, "POP AF", "Pop AF from Stack", _ => { _stack.PopRegisterFromStack(ref _af); });
        AddStandardInstruction(0xC1, 10, "POP BC", "Pop BC from Stack", _ => { _stack.PopRegisterFromStack(ref _bc); });
        AddStandardInstruction(0xD1, 10, "POP DE", "Pop DE from Stack", _ => { _stack.PopRegisterFromStack(ref _de); });
        AddStandardInstruction(0xE1, 10, "POP HL", "Pop HL from Stack", _ => { _stack.PopRegisterFromStack(ref _hl); });
        AddDoubleByteInstruction(0xDD, 0xE1, 14, "POP IX", "Pop IX from Stack", _ => { _stack.PopRegisterFromStack(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0xE1, 14, "POP IY", "Pop IY from Stack", _ => { _stack.PopRegisterFromStack(ref _iy); });
    }

    private void PopulateExchangeBlockTransferAndSearchInstructions()
    {
        AddStandardInstruction(0xE3, 19, "EX (SP),HL", "Exchange HL with Data from Memory Address in SP", _ => { _stack.SwapRegisterWithDataAtStackPointerAddress(ref _hl); });
        AddStandardInstruction(0x8, 4, "EX AF,AF'", "Exchange AF and AF Shadow", _ => { SwapRegisters(ref _af, ref _afShadow); });
        AddStandardInstruction(0xEB, 4, "EX DE,HL", "Exchange DE and HL", _ => { _hl.SwapWithDeRegister(_de); });
        AddStandardInstruction(0xD9, 4, "EXX", "Exchange BC, DE, HL with Shadow Registers", _ => { SwapRegisters(ref _bc, ref _bcShadow); SwapRegisters(ref _de, ref _deShadow); SwapRegisters(ref _hl, ref _hlShadow); });
        AddDoubleByteInstruction(0xDD, 0xE3, 23, "EX (SP),IX", "Exchange IX with Data from Memory Address in SP", _ => { _stack.SwapRegisterWithDataAtStackPointerAddress(ref _ix); });
        AddDoubleByteInstruction(0xFD, 0xE3, 23, "EX (SP),IY", "Exchange IY with Data from Memory Address in SP", _ => { _stack.SwapRegisterWithDataAtStackPointerAddress(ref _iy); });
    }

    private void PopulateInputOutputInstructions()
    {
        AddStandardInstruction(0xDB, 11, "IN A,(N)", "Read I/O at N into A", _ => { _accumulator.Set(_ioManagement.Read(_af.High, _pc.GetNextDataByte(), false)); });
        AddDoubleByteInstruction(0xED, 0x70, 12, "IN (C)", "Read I/O at B/C But Only Set Flags", _ => { _ioManagement.Read(_bc.High, _bc.Low, true); });
        AddDoubleByteInstruction(0xED, 0x78, 12, "IN A,(C)", "Read I/O at B/C into A with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _accumulator); });
        AddDoubleByteInstruction(0xED, 0x40, 12, "IN B,(C)", "Read I/O at B/C into B with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _b); });
        AddDoubleByteInstruction(0xED, 0x48, 12, "IN C,(C)", "Read I/O at B/C into C with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _c); });
        AddDoubleByteInstruction(0xED, 0x50, 12, "IN D,(C)", "Read I/O at B/C into D with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _d); });
        AddDoubleByteInstruction(0xED, 0x58, 12, "IN E,(C)", "Read I/O at B/C into E with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _e); });
        AddDoubleByteInstruction(0xED, 0x60, 12, "IN H,(C)", "Read I/O at B/C into H with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _h); });
        AddDoubleByteInstruction(0xED, 0x68, 12, "IN L,(C)", "Read I/O at B/C into L with flags", _ => { _ioManagement.ReadAndSetRegister(_bc, _l); });
        
        AddDoubleByteInstruction(0xED, 0xA2, 16, "INI", "Input and Increment", _ =>
        {
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            var data = _io.ReadPort(portAddress);
            _memoryManagement.WriteToMemory(_hl, data);
            _b.Decrement();
            _hl.Increment();
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
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
            _memoryManagement.WriteToMemory(_hl, data);
            _b.Decrement();
            _hl.Increment();
            _flags.SetFlag(Z80StatusFlags.ZeroZ);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
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
            _memoryManagement.WriteToMemory(_hl, data);
            _b.Decrement();
            _hl.Decrement();
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
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
            _memoryManagement.WriteToMemory(_hl, data);
            _b.Decrement();
            _hl.Decrement();
            _flags.SetFlag(Z80StatusFlags.ZeroZ);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });


        AddStandardInstruction(0xD3, 11, "OUT (N),A", "Write I/O at n from A", _ => { _ioManagement.Write(_af.High, _pc.GetNextDataByte(), _accumulator); });
        AddDoubleByteInstruction(0xED, 0x79, 12, "OUT (C),A", "Write I/O at B/C from A", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _accumulator); });
        AddDoubleByteInstruction(0xED, 0x41, 12, "OUT (C),B", "Write I/O at B/C from B", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _b); });
        AddDoubleByteInstruction(0xED, 0x49, 12, "OUT (C),C", "Write I/O at B/C from C", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _c); });
        AddDoubleByteInstruction(0xED, 0x51, 12, "OUT (C),D", "Write I/O at B/C from D", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _d); });
        AddDoubleByteInstruction(0xED, 0x59, 12, "OUT (C),E", "Write I/O at B/C from E", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _e); });
        AddDoubleByteInstruction(0xED, 0x61, 12, "OUT (C),H", "Write I/O at B/C from H", _ => {  _ioManagement.Write(_b.Value, _bc.Low, _h); });
        AddDoubleByteInstruction(0xED, 0x69, 12, "OUT (C),L", "Write I/O at B/C from L", _ => { _ioManagement.Write(_b.Value, _bc.Low, _l); });

        AddDoubleByteInstruction(0xED, 0xA3, 16, "OUTI", "Output and Increment", _ =>
        {
            var data = _memoryManagement.ReadFromMemory(_hl);
            _b.Decrement();
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            _hl.Increment();
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
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

            var data = _memoryManagement.ReadFromMemory(_hl);
            _b.Decrement();
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            _hl.Increment();
            _flags.SetFlag(Z80StatusFlags.ZeroZ);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
                _currentCycleCount += 21;
            }
            else
            {
                _currentCycleCount += 16;
            }
        });

        AddDoubleByteInstruction(0xED, 0xAB, 16, "OUTD", "Output and Decrement", _ =>
        {
            var data = _memoryManagement.ReadFromMemory(_hl);
            _b.Decrement();
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

             _hl.Decrement();
            _flags.SetClearFlagConditional(Z80StatusFlags.ZeroZ, _bc.High == 0);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);
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

            var data = _memoryManagement.ReadFromMemory(_hl);
            _b.Decrement();
            var portAddress = (ushort)((_bc.High << 8) + _bc.Low);
            _io.WritePort(portAddress, data);

            _hl.Decrement();
            _flags.SetFlag(Z80StatusFlags.ZeroZ);
            _flags.SetFlag(Z80StatusFlags.AddSubtractN);

            if (_bc.High != 0)
            {
                // If not zero, set PC back by 2 so instruction is repeated
                // Note that this is not a loop here since we still need to process interrupts
                // hence running instruction again rather than doing a loop here
                _pc.MoveProgramCounterBackward(2);
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
            PrefixOpCode = 0x00;
            OpCode = opCode;
            Name = name;
            Description = description;
            ClockCycles = cycles;
            _handleMethod = handleMethod;
        }

        public Instruction(byte prefixOpCode, byte opCode, string name, string description, int cycles, Action<Instruction> handleMethod)
        {
            PrefixOpCode = prefixOpCode;
            OpCode = opCode;
            Name = name;
            Description = description;
            ClockCycles = cycles;
            _handleMethod = handleMethod;
        }

        private readonly Action<Instruction> _handleMethod;

        public byte PrefixOpCode { get; }
        public byte OpCode { get; }
        public string Name { get; }
        public string Description { get; }
        public int ClockCycles { get; }

        public void Execute()
        {
            _handleMethod(this);
        }

        public virtual string GetOpCode()
        {
            return PrefixOpCode != 0x00 ? $"{PrefixOpCode:X2} {OpCode:X2}" : $"{OpCode:X2}";
        }
    }

    /// <summary>
    ///     Special CB instructions where they are DD/FD CB XX
    /// </summary>
    private class SpecialCbInstruction : Instruction
    {
        public byte DataByte { get; private set; }

        public SpecialCbInstruction(byte prefixOpCode, byte opCode, string name, string description, int cycles,
            Action<Instruction> handleMethod) : base(prefixOpCode, opCode, name, description, cycles, handleMethod) { }

        public void SetDataByte(byte data)
        {
            DataByte = data;
        }

        public override string GetOpCode()
        {
            return $"{PrefixOpCode:X2} CB {OpCode:X2}";
        }
    }
}