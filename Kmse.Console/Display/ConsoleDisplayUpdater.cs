using Kmse.Core.IO.Vdp.Rendering;

namespace Kmse.Console.Display;

public class ConsoleDisplayUpdater : IVdpDisplayUpdater
{
    public void DisplayFrame(ReadOnlySpan<byte> frame) { }

    public void DisplayDebugSpriteTable(ReadOnlySpan<byte> frame) { }

    public void DisplayDebugSpriteTileMemory(ReadOnlySpan<byte> frame) { }

    public bool IsDebugSpriteTableEnabled()
    {
        return false;
    }

    public bool IsDebugSpriteTileMemoryEnabled()
    {
        return false;
    }
}