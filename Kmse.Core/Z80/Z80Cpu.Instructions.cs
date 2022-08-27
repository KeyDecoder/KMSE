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
        AddStandardInstruction(0xE9, 4, "JP (HL)", "Unconditional Jump", _ => { });
        AddStandardInstruction(0x10, DynamicCycleHandling, "DJNZ $+2", "Decrement, Jump if Non-Zero", _ => { });
        AddDoubleByteInstruction(0xDD, 0xE9, 8, "JP (IX)", "Unconditional Jump", _ => { });
        AddDoubleByteInstruction(0xFD, 0xE9, 8, "JP (IY)", "Unconditional Jump", _ => { });
        AddStandardInstruction(0x18, 12, "JR $N+2", "Relative Jump", _ => { });
        AddStandardInstruction(0x38, DynamicCycleHandling, "JR C,$N+2", "Cond. Relative Jump", _ => { });
        AddStandardInstruction(0x30, DynamicCycleHandling, "JR NC,$N+2", "Cond. Relative Jump", _ => { });
        AddStandardInstruction(0x28, DynamicCycleHandling, "JR Z,$N+2", "Cond. Relative Jump", _ => { });
        AddStandardInstruction(0x20, DynamicCycleHandling, "JR NZ,$N+2", "Cond. Relative Jump", _ => { });

        AddStandardInstruction(0xC3, 10, "JP $NN", "Unconditional Jump", _ => { });
        AddStandardInstruction(0xDA, 10, "JP C,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xD2, 10, "JP NC,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xFA, 10, "JP M,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xF2, 10, "JP P,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xCA, 10, "JP Z,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xC2, 10, "JP NZ,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xEA, 10, "JP PE,$NN", "Conditional Jump", _ => { });
        AddStandardInstruction(0xE2, 10, "JP PO,$NN", "Conditional Jump", _ => { });

        AddStandardInstruction(0xCD, 17, "CALL NN", "Unconditional Call", _ => { });
        AddStandardInstruction(0xDC, DynamicCycleHandling, "CALL C,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xD4, DynamicCycleHandling, "CALL NC,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xFC, DynamicCycleHandling, "CALL M,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xF4, DynamicCycleHandling, "CALL P,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xCC, DynamicCycleHandling, "CALL Z,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xC4, DynamicCycleHandling, "CALL NZ,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xEC, DynamicCycleHandling, "CALL PE,NN", "Conditional Call", _ => { });
        AddStandardInstruction(0xE4, DynamicCycleHandling, "CALL PO,NN", "Conditional Call", _ => { });

        AddStandardInstruction(0xC9, 10, "RET", "Return", _ => { });
        AddStandardInstruction(0xD8, DynamicCycleHandling, "RET C", "Conditional Return", _ => { });
        AddStandardInstruction(0xD0, DynamicCycleHandling, "RET NC", "", _ => { });
        AddStandardInstruction(0xF8, DynamicCycleHandling, "RET M", "", _ => { });
        AddStandardInstruction(0xF0, DynamicCycleHandling, "RET P", "", _ => { });
        AddStandardInstruction(0xC8, DynamicCycleHandling, "RET Z", "", _ => { });
        AddStandardInstruction(0xC0, DynamicCycleHandling, "RET NZ", "", _ => { });
        AddStandardInstruction(0xE8, DynamicCycleHandling, "RET PE", "", _ => { });
        AddStandardInstruction(0xE0, DynamicCycleHandling, "RET PO", "", _ => { });

        AddStandardInstruction(0xC7, 11, "RST 0", "Restart", _ => { });
        AddStandardInstruction(0xCF, 11, "RST 08H", "", _ => { });
        AddStandardInstruction(0xD7, 11, "RST 10H", "", _ => { });
        AddStandardInstruction(0xDF, 11, "RST 18H", "", _ => { });
        AddStandardInstruction(0xE7, 11, "RST 20H", "", _ => { });
        AddStandardInstruction(0xEF, 11, "RST 28H", "", _ => { });
        AddStandardInstruction(0xF7, 11, "RST 30H", "", _ => { });
        AddStandardInstruction(0xFF, 11, "RST 38H", "", _ => { });
        
        AddDoubleByteInstruction(0xED, 0x4D, 14, "RETI", "Return from Interrupt", _ => { });
        AddDoubleByteInstruction(0xED, 0x45, 14, "RETN", "Return from NMI", _ => { });
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

        AddStandardInstruction(0x3C, 4, "INC A", "Increment (8-bit)", _ => { });
        AddStandardInstruction(0x4, 4, "INC B", "", _ => { });
        AddStandardInstruction(0x0C, 4, "INC C", "", _ => { });
        AddStandardInstruction(0x14, 4, "INC D", "", _ => { });
        AddStandardInstruction(0x1C, 4, "INC E", "", _ => { });
        AddStandardInstruction(0x24, 4, "INC H", "", _ => { });
        AddStandardInstruction(0x2C, 4, "INC L", "", _ => { });
        AddStandardInstruction(0x3, 6, "INC BC", "Increment (16-bit)", _ => { });
        AddStandardInstruction(0x13, 6, "INC DE", "", _ => { });
        AddStandardInstruction(0x23, 6, "INC HL", "", _ => { });
        AddStandardInstruction(0x33, 6, "INC SP", "", _ => { });
        AddStandardInstruction(0x34, 11, "INC (HL)", "Increment (indirect)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x34, 23, "INC (IX+d)", "Increment", _ => { });
        AddDoubleByteInstruction(0xFD, 0x34, 23, "INC (IY+d)", "Increment", _ => { });

        AddStandardInstruction(0x3D, 4, "DEC A", "Decrement (8-bit)", _ => { });
        AddStandardInstruction(0x5, 4, "DEC B", "", _ => { });
        AddStandardInstruction(0x0D, 4, "DEC C", "", _ => { });
        AddStandardInstruction(0x15, 4, "DEC D", "", _ => { });
        AddStandardInstruction(0x1D, 4, "DEC E", "", _ => { });
        AddStandardInstruction(0x25, 4, "DEC H", "", _ => { });
        AddStandardInstruction(0x35, 11, "DEC (HL)", "", _ => { });
        AddStandardInstruction(0x0B, 6, "DEC BC	Decrement (16-bit)", "", _ => { });
        AddStandardInstruction(0x1B, 6, "DEC DE", "", _ => { });
        AddStandardInstruction(0x2B, 6, "DEC HL", "", _ => { });
        AddStandardInstruction(0x3B, 6, "DEC SP", "", _ => { });
        AddStandardInstruction(0x2D, 4, "DEC L", "Decrement", _ => { });
        AddDoubleByteInstruction(0xDD, 0x2B, 10, "DEC IX", "Decrement", _ => { });
        AddDoubleByteInstruction(0xFD, 0x2B, 10, "DEC IY", "Decrement", _ => { });
        AddDoubleByteInstruction(0xDD, 0x35, 23, "DEC (IX+d)", "Decrement", _ => { });
        AddDoubleByteInstruction(0xFD, 0x35, 23, "DEC (IY+d)", "Decrement", _ => { });

        AddStandardInstruction(0x3F, 4, "CCF", "Complement Carry Flag", _ => { });
        AddStandardInstruction(0x27, 4, "DAA", "Decimal Adjust Accumulator", _ => { });
        AddStandardInstruction(0X2F, 4, "CPL", "Complement", _ => { });
        AddStandardInstruction(0x37, 4, "SCF", "Set Carry Flag", _ => { });
        AddDoubleByteInstruction(0xED, 0x44, 8, "NEG", "Negate", _ => { });
    }

    private void PopulateBitSetResetAndTestGroupInstructions()
    {
        AddDoubleByteInstructionWithMask(0xCB, 0x80, 0x3F, 8, "RES b,r", "Reset bit", _ => { });
        AddDoubleByteInstruction(0xCB, 0x40, 8, "BIT b,r", "Test Bit", _ => { });
        AddDoubleByteInstruction(0xCB, 0x46, 12, "BIT b,(HL)", "Test Bit", _ => { });
        AddDoubleByteInstructionWithMask(0xCB, 0xC0, 0x3F, 8, "SET b,r", "Set bit", _ => { });

        AddSpecialCbInstructionWithMask(0xDD, 0xC6, 7, 23, "SET b,(IX+d)", "Set Bit", _ => { });
        AddSpecialCbInstructionWithMask(0xFD, 0xC6, 7, 23, "SET b,(IY+d)", "Set Bit", _ => { });
        AddSpecialCbInstructionWithMask(0xDD, 0x46, 7, 20, "BIT b,(IX+d)", "Test Bit", _ => { });
        AddSpecialCbInstructionWithMask(0xFD, 0x46, 7, 20, "BIT b,(IY+d)", "Test Bit", _ => { });
        AddSpecialCbInstructionWithMask(0xDD, 0x86, 7, 23, "RES b,(IX+d)", "Reset bit", _ => { });
        AddSpecialCbInstructionWithMask(0xFD, 0x86, 7, 23, "RES b,(IY+d)", "Reset bit", _ => { });
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
        AddStandardInstructionWithMask(0x78, 7, 4, "LD A,r", "Load (8-bit)", _ => { });
        AddStandardInstruction(0x0A, 7, "LD A,(BC)", "", _ => { });
        AddStandardInstruction(0x1A, 7, "LD A,(DE)", "", _ => { });
        AddStandardInstructionWithMask(0x40, 7, 4, "LD B,r", "", _ => { });
        AddStandardInstructionWithMask(0x48, 7, 4, "LD C,r", "", _ => { });
        AddStandardInstructionWithMask(0x50, 7, 4, "LD D,r", "", _ => { });
        AddStandardInstructionWithMask(0x58, 7, 4, "LD E,r", "", _ => { });
        AddStandardInstructionWithMask(0x60, 7, 4, "LD H,r", "", _ => { });
        AddStandardInstructionWithMask(0x68, 7, 4, "LD L,r", "", _ => { });
        AddStandardInstruction(0xF9, 6, "LD SP,HL", "", _ => { });
        AddStandardInstructionWithMask(0x70, 5, 7, "LD (HL),r", "Load (Indirect)", _ => { });
        AddStandardInstruction(0x77, 7, "LD (HL),A", "", _ => { });
        AddStandardInstruction(0x2, 7, "LD (BC),A", "", _ => { });
        AddStandardInstruction(0x12, 7, "LD (DE),A", "", _ => { });
        AddDoubleByteInstruction(0xED, 0x47, 9, "LD I,A", "Load*", _ => { });
        AddDoubleByteInstruction(0xED, 0x4F, 9, "LD R,A", "Load", _ => { });
        AddDoubleByteInstruction(0xED, 0x57, 9, "LD A,I", "Load*", _ => { });
        AddDoubleByteInstruction(0xED, 0x5F, 9, "LD A,R", "Load", _ => { });
        AddStandardInstruction(0x3E, 7, "LD A,N", "Load", _ => { });
        AddStandardInstruction(0x06, 7, "LD B,N", "Load", _ => { });
        AddStandardInstruction(0x0E, 7, "LD C,N", "Load", _ => { });
        AddStandardInstruction(0x16, 7, "LD D,N", "Load", _ => { });
        AddStandardInstruction(0x1E, 7, "LD E,N", "Load", _ => { });
        AddStandardInstruction(0x26, 7, "LD H,N", "Load", _ => { });
        AddStandardInstruction(0x2E, 7, "LD L,N", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0xF9, 10, "LD SP,IX", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0xF9, 10, "LD SP,IY", "Load", _ => { });
        AddStandardInstruction(0x36, 10, "LD (HL),N", "Load", _ => { });
        AddStandardInstruction(0x3A, 13, "LD A,(NN)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x7E, 19, "LD A,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x7E, 19, "LD A,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x46, 19, "LD B,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x46, 19, "LD B,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x4E, 19, "LD C,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x4E, 19, "LD C,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x56, 19, "LD D,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x56, 19, "LD D,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x5E, 19, "LD E,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x5E, 19, "LD E,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x66, 19, "LD H,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x66, 19, "LD H,(IY+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xDD, 0x6E, 19, "LD L,(IX+d)", "Load", _ => { });
        AddDoubleByteInstruction(0xFD, 0x6E, 19, "LD L,(IY+d)", "Load", _ => { });
        AddStandardInstruction(0x01, 10, "LD BC,NN", "Load", _ => { });
        AddStandardInstruction(0x11, 10, "LD DE,NN", "Load", _ => { });
        AddStandardInstruction(0x2A, 16, "LD HL,(NN)", "Load", _ => { });
        AddStandardInstruction(0x21, 10, "LD HL,NN", "Load", _ => { });
        AddStandardInstruction(0x31, 10, "LD SP,NN", "Load", _ => { });
        AddStandardInstruction(0x32, 13, "LD (NN),A", "Load", _ => { });
        AddStandardInstruction(0x22, 16, "LD (NN),HL", "Load", _ => { });
        AddDoubleByteInstructionWithMask(0xDD, 0x70, 7, 19, "LD (IX+d),r", "Load", _ => { });
        AddDoubleByteInstructionWithMask(0xFD, 0x70, 7, 19, "LD (IY+d),r", "Load", _ => { });
        AddDoubleByteInstruction(0xED, 0x4B, 20, "LD BC, (NN)", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xED, 0x5B, 20, "LD DE, (NN)", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xED, 0x7B, 20, "LD SP, (NN)", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x2A, 20, "LD IX, (NN)", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x21, 14, "LD IX, NN", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xFD, 0x2A, 20, "LD IY, (NN)", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xFD, 0x21, 14, "LD IY, NN", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xED, 0x43, 20, "LD(NN), BC", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xED, 0x53, 20, "LD(NN), DE", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x22, 20, "LD(NN), IX", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xFD, 0x22, 20, "LD(NN), IY", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xED, 0x73, 20, "LD(NN), SP", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xDD, 0x36, 19, "LD(IX + d), N", "Load(16 - bit)", _ => { });
        AddDoubleByteInstruction(0xFD, 0x36, 19, "LD(IY + d), N", "Load(16 - bit)", _ => { });

        AddDoubleByteInstruction(0xED, 0xA8, 16, "LDD", "Load and Decrement", _ => { });
        AddDoubleByteInstruction(0xED, 0xB8, DynamicCycleHandling, "LDDR", "Load, Decrement, Repeat", _ => { });
        AddDoubleByteInstruction(0xED, 0xA0, 16, "LDI", "Load and Increment", _ => { });
        AddDoubleByteInstruction(0xED, 0xB0, DynamicCycleHandling, "LDIR", "Load, Increment, Repeat", _ => { });
        
        AddStandardInstruction(0xF1, 10, "POP AF	Pop", "", _ => { });
        AddStandardInstruction(0xC1, 10, "POP BC", "", _ => { });
        AddStandardInstruction(0xD1, 10, "POP DE", "", _ => { });
        AddStandardInstruction(0xE1, 10, "POP HL", "", _ => { });
        AddDoubleByteInstruction(0xDD, 0xE1, 14, "POP IX", "Pop", _ => { });
        AddDoubleByteInstruction(0xFD, 0xE1, 14, "POP IY", "Pop", _ => { });

        AddStandardInstruction(0xF5, 11, "PUSH AF", "Push", _ => { });
        AddStandardInstruction(0xC5, 11, "PUSH BC", "Push", _ => { });
        AddStandardInstruction(0xD5, 11, "PUSH DE", "Push", _ => { });
        AddStandardInstruction(0xE5, 11, "PUSH HL", "Push", _ => { });
        AddDoubleByteInstruction(0xDD, 0xE5, 15, "PUSH IX", "Push", _ => { });
        AddDoubleByteInstruction(0xFD, 0xE5, 15, "PUSH IY", "Push", _ => { });
    }

    private void PopulateExchangeBlockTransferAndSearchInstructions()
    {
        AddStandardInstruction(0xE3, 19, "EX (SP),HL", "Exchange", _ => { });
        AddStandardInstruction(0x8, 4, "EX AF,AF'", "", _ => { });
        AddStandardInstruction(0xEB, 4, "EX DE,HL", "", _ => { });
        AddStandardInstruction(0xD9, 4, "EXX", "Exchange", _ => { });
        AddDoubleByteInstruction(0xDD, 0xE3, 23, "EX (SP),IX", "Exchange", _ => { });
        AddDoubleByteInstruction(0xFD, 0xE3, 23, "EX (SP),IY", "Exchange", _ => { });
    }

    private void PopulateInputOutputInstructions()
    {
        AddStandardInstruction(0xDB, 11, "IN A,(N)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x70, 12, "IN (C)", "Input*", _ => { });
        AddDoubleByteInstruction(0xED, 0x78, 12, "IN A,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x40, 12, "IN B,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x48, 12, "IN C,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x50, 12, "IN D,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x58, 12, "IN E,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x60, 12, "IN H,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xED, 0x68, 12, "IN L,(C)", "Input", _ => { });
        AddDoubleByteInstruction(0xDD, 0x23, 10, "INC IX", "Increment", _ => { });
        AddDoubleByteInstruction(0xFD, 0x23, 10, "INC IY", "Increment", _ => { });
        AddDoubleByteInstruction(0xED, 0xAA, 16, "IND", "Input and Decrement", _ => { });
        AddDoubleByteInstruction(0xED, 0xBA, DynamicCycleHandling, "INDR", "Input, Decrement, Repeat", _ => { });
        AddDoubleByteInstruction(0xED, 0xA2, 16, "INI", "Input and Increment", _ => { });
        AddDoubleByteInstruction(0xED, 0xB2, 21 / 16, "INIR", "Input, Increment, Repeat", _ => { });

        AddStandardInstruction(0xD3, 11, "OUT (N),A", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x71, 12, "OUT (C),0", "Output*", _ => { });
        AddDoubleByteInstruction(0xED, 0x79, 12, "OUT (C),A", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x41, 12, "OUT (C),B", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x49, 12, "OUT (C),C", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x51, 12, "OUT (C),D", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x59, 12, "OUT (C),E", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x61, 12, "OUT (C),H", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0x69, 12, "OUT (C),L", "Output", _ => { });
        AddDoubleByteInstruction(0xED, 0xAB, 16, "OUTD", "Output and Decrement", _ => { });
        AddDoubleByteInstruction(0xED, 0xBB, DynamicCycleHandling, "OTDR", "Output, Decrement, Repeat", _ => { });
        AddDoubleByteInstruction(0xED, 0xA3, 16, "OUTI", "Output and Increment", _ => { });
        AddDoubleByteInstruction(0xED, 0xB3, DynamicCycleHandling, "OTIR", "Output, Increment, Repeat", _ => { });
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