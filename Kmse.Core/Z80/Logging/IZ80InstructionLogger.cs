namespace Kmse.Core.Z80.Logging;

public interface IZ80InstructionLogger
{
    IZ80InstructionLogger StartNewInstruction(ushort currentProgramCounter);
    IZ80InstructionLogger SetOpCode(string opCodeHexString, string name, string description);
    IZ80InstructionLogger AddOperationData(byte data);
    IZ80InstructionLogger AddOperationData(ushort data);
    void Log();
}