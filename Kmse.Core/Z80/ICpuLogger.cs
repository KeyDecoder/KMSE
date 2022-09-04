﻿namespace Kmse.Core.Z80;

public interface ICpuLogger
{
    void LogDebug(string message);
    void LogMemoryRead(ushort address, byte data);
    void LogInstruction(ushort baseAddress, byte opCode, string operation, string data);
}