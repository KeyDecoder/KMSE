namespace Kmse.Core.Z80.Model;

public class Instruction
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