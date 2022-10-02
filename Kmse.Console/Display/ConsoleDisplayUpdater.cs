using Kmse.Core.IO.Vdp.Rendering;

namespace Kmse.Console.Display;

public class ConsoleDisplayUpdater : IVdpDisplayUpdater
{
    public void DisplayFrame(Span<byte> frame) { }

    public void DisplayDebugSpriteTable(Span<byte> frame) { }

    public void DisplayDebugSpriteTileMemory(Span<byte> frame) { }

    public bool IsDebugSpriteTableEnabled()
    {
        return false;
    }

    public bool IsDebugSpriteTileMemoryEnabled()
    {
        return false;
    }
}