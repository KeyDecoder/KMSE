using Kmse.Core.IO;
using Kmse.Core.Memory;

namespace Kmse.Core.Z80;

public interface IZ80Cpu
{
    void Initialize(IMasterSystemMemory memory, IMasterSystemIoManager io);
    void Reset();
    int ExecuteNextCycle();
}