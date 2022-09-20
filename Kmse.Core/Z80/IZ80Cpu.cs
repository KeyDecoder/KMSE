using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.Support;

namespace Kmse.Core.Z80;

public interface IZ80Cpu
{
    void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io);
    IZ80InterruptManagement GetInterruptManagementInterface();
    CpuStatus GetStatus();
    void Reset();
    int ExecuteNextCycle();
}