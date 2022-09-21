using System;

namespace Kmse.Core.Z80.Instructions;

public interface IZ80CpuCycleCounter
{
    int CurrentCycleCount { get; }
    void Reset();
    void Increment(int value);

}