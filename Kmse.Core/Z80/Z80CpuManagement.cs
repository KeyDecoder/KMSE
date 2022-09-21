using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Memory;

namespace Kmse.Core.Z80;

public class Z80CpuManagement
{
    public IZ80CpuInputOutput IoManagement { get; set; }
    public IZ80CpuMemoryManagement MemoryManagement { get; set; }
    public IZ80InterruptManagement InterruptManagement { get; set; }
    public IZ80CpuCycleCounter CycleCounter { get; set; }
}