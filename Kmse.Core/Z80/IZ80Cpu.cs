using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80;

public interface IZ80Cpu
{
    CpuStatus GetStatus();
    void Reset();
    int ExecuteNextCycle();
}