using Kmse.Core.Z80.Logging;

namespace Kmse.Core.Z80.Running;

public class Z80CpuRunningStateManager : IZ80CpuRunningStateManager
{
    private readonly ICpuLogger _cpuLogger;

    public Z80CpuRunningStateManager(ICpuLogger cpuLogger)
    {
        _cpuLogger = cpuLogger;
    }

    public bool Halted { get; private set; }

    public void Reset()
    {
        Halted = false;
    }

    public void Halt()
    {
        _cpuLogger.Debug("Halting CPU");
        Halted = true;
    }

    public void ResumeIfHalted()
    {
        if (Halted)
        {
            _cpuLogger.Debug("Resuming CPU");
        }

        Halted = false;
    }
}