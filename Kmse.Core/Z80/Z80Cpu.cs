using Kmse.Core.IO;
using Kmse.Core.Memory;

namespace Kmse.Core.Z80;

public class Z80Cpu : IZ80Cpu
{
    private readonly ICpuLogger _cpuLogger;
    private int _currentCycleCount;
    private IMasterSystemIoManager _io;
    private IMasterSystemMemory _memory;

    public Z80Cpu(ICpuLogger cpuLogger)
    {
        _cpuLogger = cpuLogger;
    }

    public void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io)
    {
        _memory = memory;
        _io = io;
    }

    public void Reset()
    {
        _currentCycleCount = 0;
    }

    public int ExecuteNextCycle()
    {
        // NOP
        _currentCycleCount = 4;

        return _currentCycleCount;
    }
}