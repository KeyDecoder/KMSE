using Kmse.Core.Z80.Support;

namespace Kmse.Core;

public interface IMasterSystemConsole
{
    Task<bool> LoadCartridge(string filename, CancellationToken cancellationToken);
    void PowerOn();
    void PowerOff();
    void Pause();
    void Unpause();
    bool IsRunning();
    bool IsPaused();
    void Run();
    CpuStatus GetCpuStatus();

    void TriggerPauseButton();
}