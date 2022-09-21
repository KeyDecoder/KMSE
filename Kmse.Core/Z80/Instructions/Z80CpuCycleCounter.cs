namespace Kmse.Core.Z80.Instructions;

public class Z80CpuCycleCounter : IZ80CpuCycleCounter
{
    public int CurrentCycleCount { get; private set; }

    public void Reset()
    {
        CurrentCycleCount = 0;
    }

    public void Increment(int value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException("Cannot increment cycle count by negative value");
        }
        CurrentCycleCount += value;
    }
}