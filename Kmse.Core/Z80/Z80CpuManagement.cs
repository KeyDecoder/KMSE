using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Running;

namespace Kmse.Core.Z80;

public class Z80CpuManagement
{
    public IZ80CpuInputOutputManager IoManagement { get; set; }
    public IZ80CpuMemoryManagement MemoryManagement { get; set; }
    public IZ80InterruptManagement InterruptManagement { get; set; }
    public IZ80CpuCycleCounter CycleCounter { get; set; }
    public IZ80CpuRunningStateManager RunningStateManager { get; set; }
}