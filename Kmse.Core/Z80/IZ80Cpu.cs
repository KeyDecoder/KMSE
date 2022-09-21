using Kmse.Core.Z80.Model;

namespace Kmse.Core.Z80;

public interface IZ80Cpu
{
    CpuStatus GetStatus();
    void Reset();
    int ExecuteNextCycle();
}