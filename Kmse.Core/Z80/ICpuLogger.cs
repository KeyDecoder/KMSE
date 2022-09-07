namespace Kmse.Core.Z80;

public interface ICpuLogger
{
    void Debug(string message);
    void Error(string message);
    void LogInstruction(ushort baseAddress, string opCode, string operationName, string operationDescription, string data);
}