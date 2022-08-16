namespace Kmse.Core;

public interface IMasterSystemConsole
{
    Task<bool> LoadCartridge(string filename, CancellationToken cancellationToken);
    void PowerOn();
    void PowerOff();
    void Pause();
    void Unpause();
    void Run();
}