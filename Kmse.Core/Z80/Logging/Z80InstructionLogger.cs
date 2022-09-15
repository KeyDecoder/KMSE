using System.Text;

namespace Kmse.Core.Z80.Logging;

public class Z80InstructionLogger : IZ80InstructionLogger
{
    private readonly ICpuLogger _cpuLogger;
    private readonly StringBuilder _currentData = new();
    private ushort _instructionMemoryAddressStart;
    private string _opCodeString;
    private string _description;
    private string _name;

    public Z80InstructionLogger(ICpuLogger cpuLogger)
    {
        _cpuLogger = cpuLogger;
    }

    /// <summary>
    /// Start a new log entry for an instruction given the provided base program counter address
    /// </summary>
    /// <param name="currentProgramCounter">Current program counter address/value</param>
    /// <returns><see cref="Z80InstructionLogger"/></returns>
    public IZ80InstructionLogger StartNewInstruction(ushort currentProgramCounter)
    {
        _instructionMemoryAddressStart = currentProgramCounter;
        _currentData.Clear();
        _opCodeString = string.Empty;
        _name = string.Empty;
        _description = string.Empty;
        return this;
    }

    /// <summary>
    /// Set the op code for this instruction
    /// </summary>
    /// <param name="opCodeHexString">Op code value formatted as Hex string</param>
    /// <param name="name">Name of instruction</param>
    /// <param name="description">Description for instruction</param>
    /// <returns><see cref="Z80InstructionLogger"/></returns>
    public IZ80InstructionLogger SetOpCode(string opCodeHexString, string name, string description)
    {
        _opCodeString = opCodeHexString;
        _name = name;
        _description = description;
        return this;
    }

    /// <summary>
    /// Add operation data for this instruction.  Any data is appended to existing data as a string
    /// </summary>
    /// <param name="data">Byte data to add</param>
    /// <returns><see cref="Z80InstructionLogger"/></returns>
    public IZ80InstructionLogger AddOperationData(byte data)
    {
        _currentData.Append(data.ToString("X2"));
        return this;
    }

    /// <summary>
    /// Add operation data for this instruction.  Any data is appended to existing data as a string
    /// </summary>
    /// <param name="data">unsigned short data to add</param>
    /// <returns><see cref="Z80InstructionLogger"/></returns>
    public IZ80InstructionLogger AddOperationData(ushort data)
    {
        _currentData.Append(data.ToString("X4"));
        return this;
    }

    /// <summary>
    /// Log the instruction to the CPU logger
    /// </summary>
    public void Log()
    {
        _cpuLogger.LogInstruction(_instructionMemoryAddressStart, _opCodeString, _name,
            _description, _currentData.ToString());
    }
}