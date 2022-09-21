namespace Kmse.Core.Z80.Model;

/// <summary>
///     Special CB instructions where they are DD/FD CB XX
/// </summary>
public class SpecialCbInstruction : Instruction
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