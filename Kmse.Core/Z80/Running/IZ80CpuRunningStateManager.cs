namespace Kmse.Core.Z80.Running;

public interface IZ80CpuRunningStateManager
{
    bool Halted { get; }
    void Reset();
    void Halt();

    void ResumeIfHalted();
}