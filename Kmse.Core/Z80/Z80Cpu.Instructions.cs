namespace Kmse.Core.Z80;

/// <summary>
/// Generate the list of instructions and how to handle them
/// Created using this amazing page which summaries all the instructions - https://www.smspower.org/Development/InstructionSet
/// </summary>
public partial class Z80Cpu
{
    private void AddGenericInstructionWithMask(byte opCode, byte mask, int cycles, string name, string description, Action<Instruction> handleFunc)
    {
        // These op codes do the same thing but generally have some information in the op code itself, but we can handle them with the same function
        // An example being ADD A, r where 80 is base r is low 3 bits (mask is 7) so op codes are 0x80 - 0x87 which all do the same add, just different registers
        for (var i = opCode; i <= opCode + mask; i++)
        {
            _genericInstructions.Add(i, new Instruction(i, name, description, cycles, handleFunc));
        }
    }

    private void AddGenericInstruction(byte opCode, int cycles, string name, string description, Action<Instruction> handleFunc)
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
        }
    }

    private void PopulateInstructions()
    {
        PopulateCpuControlOperations();
        PopulateJumpCallAndReturnOperations();
        PopulateArthmeticAndLogicalInstructions();
        PopulateBitManipulationInstructions();
        PopulateRotateAndShiftInstructions();
        PopulateLoadAndExchangeInstructions();
        PopulateExchangeBlockTransferAndSearchInstructions();
        PopulateInputOutputInstructions();
    }

    private void PopulateCpuControlOperations()
    {
        AddGenericInstruction(0x00, 4, "NOP", "No Operation", (_) => { });
        AddGenericInstruction(0x76, 4, "HALT", "Halt", (_) => { _halted = true; });

        AddGenericInstruction(0xF3, 4, "DI", "Disable Interrupts", (_) => { _interruptFlipFlop1 = false; _interruptFlipFlop2 = false; });
        AddGenericInstruction(0xFB, 4, "EI", "Enable Interrupts", (_) => { _interruptFlipFlop1 = true; _interruptFlipFlop2 = true; });
        AddDoubleByteInstruction(0xED, 0x46, 8, "IM 0", "Set Maskable Interupt to Mode 0", (_) => { _interruptMode = 0; });
        AddDoubleByteInstruction(0xED, 0x56, 8, "IM 1", "Set Maskable Interupt to Mode 1", (_) => { _interruptMode = 1; });
        AddDoubleByteInstruction(0xED, 0x5E, 8, "IM 2", "Set Maskable Interupt to Mode 2", (_) => { _interruptMode = 2; });
    }

    private void PopulateJumpCallAndReturnOperations()
    {
        AddGenericInstruction(0xE9, 4, "JP (HL)", "Unconditional Jump", _ => { });
        AddGenericInstruction(0xC9, 10, "RET", "Return", _ => { });
        AddGenericInstruction(0xD8, -1, "RET C", "Conditional Return", _ => { });
        AddGenericInstruction(0xD0, -1, "RET NC", "", _ => { });
        AddGenericInstruction(0xF8, -1, "RET M", "", _ => { });
        AddGenericInstruction(0xF0, -1, "RET P", "", _ => { });
        AddGenericInstruction(0xC8, -1, "RET Z", "", _ => { });
        AddGenericInstruction(0xC0, -1, "RET NZ", "", _ => { });
        AddGenericInstruction(0xE8, -1, "RET PE", "", _ => { });
        AddGenericInstruction(0xE0, -1, "RET PO", "", _ => { });

        AddGenericInstruction(0xC7, 11, "RST 0", "Restart", _ => { });
        AddGenericInstruction(0xCF, 11, "RST 08H", "", _ => { });
        AddGenericInstruction(0xD7, 11, "RST 10H", "", _ => { });
        AddGenericInstruction(0xDF, 11, "RST 18H", "", _ => { });
        AddGenericInstruction(0xE7, 11, "RST 20H", "", _ => { });
        AddGenericInstruction(0xEF, 11, "RST 28H", "", _ => { });
        AddGenericInstruction(0xF7, 11, "RST 30H", "", _ => { });
        AddGenericInstruction(0xFF, 11, "RST 38H", "", _ => { });
    }

    private void PopulateArthmeticAndLogicalInstructions()
    {
        AddGenericInstruction(0xCE, 7, "ADC A, N", "Add with Carry", _ => { });

        AddGenericInstructionWithMask(0x88, 7, 4, "ADC A, r", "Add with Carry", _ => { });
        AddGenericInstructionWithMask(0x80, 7, 4, "ADD A,r", "Add (8-bit)", _ => { });
        AddGenericInstruction(0x09, 11, "ADD HL,BC", "Add (16-bit)", _ => { });
        AddGenericInstruction(0x19, 11, "ADD HL,DE", "", _ => { });
        AddGenericInstruction(0x29, 11, "ADD HL,HL", "", _ => { });
        AddGenericInstruction(0x39, 11, "ADD HL,SP", "", _ => { });
        AddGenericInstructionWithMask(0xA0, 7, 4, "AND r", "Logical AND", _ => { });

        AddGenericInstructionWithMask(0x98, 7, 4, "SBC r", "Subtract with Carry", _ => { });
        AddGenericInstructionWithMask(0x90, 7, 4, "SUB r", "Subtract", _ => { });
        AddGenericInstruction(0xD6, 7, "SUB N", "Subtract", _ => { });

        AddGenericInstructionWithMask(0xB8, 7, 4, "CP r", "Compare", _ => { });
        AddGenericInstruction(0xFE, 7, "CP N", "Compare", _ => { });

        AddGenericInstruction(0xE6, 7, "AND N", "And", _ => { });

        AddGenericInstructionWithMask(0xB0, 7, 4, "OR r", "Logical inclusive OR", _ => { });
        AddGenericInstructionWithMask(0xA8, 7, 4, "XOR r", "Logical Exclusive OR", _ => { });
        AddGenericInstruction(0xEE, 7, "XOR N", "Xor", _ => { });

        AddGenericInstruction(0x3C, 4, "INC A", "Increment (8-bit)", _ => { });
        AddGenericInstruction(0x4, 4, "INC B", "", _ => { });
        AddGenericInstruction(0x0C, 4, "INC C", "", _ => { });
        AddGenericInstruction(0x14, 4, "INC D", "", _ => { });
        AddGenericInstruction(0x1C, 4, "INC E", "", _ => { });
        AddGenericInstruction(0x24, 4, "INC H", "", _ => { });
        AddGenericInstruction(0x2C, 4, "INC L", "", _ => { });
        AddGenericInstruction(0x3, 6, "INC BC", "Increment (16-bit)", _ => { });
        AddGenericInstruction(0x13, 6, "INC DE", "", _ => { });
        AddGenericInstruction(0x23, 6, "INC HL", "", _ => { });
        AddGenericInstruction(0x33, 6, "INC SP", "", _ => { });
        AddGenericInstruction(0x34, 11, "INC (HL)", "Increment (indirect)", _ => { });

        AddGenericInstruction(0x3D, 4, "DEC A", "Decrement (8-bit)", _ => { });
        AddGenericInstruction(0x5, 4, "DEC B", "", _ => { });
        AddGenericInstruction(0x0D, 4, "DEC C", "", _ => { });
        AddGenericInstruction(0x15, 4, "DEC D", "", _ => { });
        AddGenericInstruction(0x1D, 4, "DEC E", "", _ => { });
        AddGenericInstruction(0x25, 4, "DEC H", "", _ => { });
        AddGenericInstruction(0x35, 11, "DEC (HL)", "", _ => { });
        AddGenericInstruction(0x0B, 6, "DEC BC	Decrement (16-bit)", "", _ => { });
        AddGenericInstruction(0x1B, 6, "DEC DE", "", _ => { });
        AddGenericInstruction(0x2B, 6, "DEC HL", "", _ => { });
        AddGenericInstruction(0x3B, 6, "DEC SP", "", _ => { });
        AddGenericInstruction(0x2D, 4, "DEC L", "Decrement", _ => { });

        AddGenericInstruction(0x3F, 4, "CCF", "Complement Carry Flag", _ => { });
        AddGenericInstruction(0x27, 4, "DAA", "Decimal Adjust Accumulator", _ => { });
        AddGenericInstruction(0X2F, 4, "CPL", "Complement", _ => { });
        AddGenericInstruction(0x37, 4, "SCF", "Set Carry Flag", _ => { });
    }

    private void PopulateBitManipulationInstructions()
    {

    }

    private void PopulateRotateAndShiftInstructions()
    {
        AddGenericInstruction(0x17, 4, "RLA", "Rotate Left Accumulator", _ => { });
        AddGenericInstruction(0x7, 4, "RLCA", "Rotate Left Circular Accumulator", _ => { });
        AddGenericInstruction(0x1F, 4, "RRA", "Rotate Right Accumulator", _ => { });
        AddGenericInstruction(0x0F, 4, "RRCA", "Rotate Right Circular Accumulator", _ => { });
    }

    private void PopulateLoadAndExchangeInstructions()
    {
        AddGenericInstructionWithMask(0x78, 7, 4, "LD A,r", "Load (8-bit)", _ => { });
        AddGenericInstruction(0x0A, 7, "LD A,(BC)", "", _ => { });
        AddGenericInstruction(0x1A, 7, "LD A,(DE)", "", _ => { });
        AddGenericInstructionWithMask(0x40, 7, 4, "LD B,r", "", _ => { });
        AddGenericInstructionWithMask(0x48, 7, 4, "LD C,r", "", _ => { });
        AddGenericInstructionWithMask(0x50, 7, 4, "LD D,r", "", _ => { });
        AddGenericInstructionWithMask(0x58, 7, 4, "LD E,r", "", _ => { });
        AddGenericInstructionWithMask(0x60, 7, 4, "LD H,r", "", _ => { });
        AddGenericInstructionWithMask(0x68, 7, 4, "LD L,r", "", _ => { });
        AddGenericInstruction(0xF9, 6, "LD SP,HL", "", _ => { });
        AddGenericInstructionWithMask(0x70, 5, 7, "LD (HL),r", "Load (Indirect)", _ => { });
        AddGenericInstruction(0x77, 7, "LD (HL),A", "", _ => { });

        AddGenericInstruction(0x2, 7, "LD (BC),A", "", _ => { });
        AddGenericInstruction(0x12, 7, "LD (DE),A", "", _ => { });

        AddGenericInstruction(0xF1, 10, "POP AF	Pop", "", _ => { });
        AddGenericInstruction(0xC1, 10, "POP BC", "", _ => { });
        AddGenericInstruction(0xD1, 10, "POP DE", "", _ => { });
        AddGenericInstruction(0xE1, 10, "POP HL", "", _ => { });
        AddGenericInstruction(0xF5, 11, "PUSH AF", "Push", _ => { });
        AddGenericInstruction(0xC5, 11, "PUSH BC", "Push", _ => { });
        AddGenericInstruction(0xD5, 11, "PUSH DE", "Push", _ => { });
        AddGenericInstruction(0xE5, 11, "PUSH HL", "Push", _ => { });
    }

    private void PopulateExchangeBlockTransferAndSearchInstructions()
    {
        AddGenericInstruction(0xE3, 19, "EX (SP),HL", "Exchange", _ => { });
        AddGenericInstruction(0x8, 4, "EX AF,AF'", "", _ => { });
        AddGenericInstruction(0xEB, 4, "EX DE,HL", "", _ => { });
        AddGenericInstruction(0xD9, 4, "EXX", "Exchange", _ => { });
    }

    private void PopulateInputOutputInstructions()
    {
        // IN, INI, INIR, IND, INDR, OUT, OUTDR
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
}