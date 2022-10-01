namespace Kmse.Core.IO.Vdp.Rendering;

public interface IVdpDisplayUpdater
{
    // TODO: If mode changes, pass the size through update display so display can change resolution
    void DisplayFrame(Span<byte> frame);
    void DisplayDebugSpriteTable(Span<byte> frame);
    void DisplayDebugSpriteTileMemory(Span<byte> frame);
    bool IsDebugSpriteTableEnabled();
    bool IsDebugSpriteTileMemoryEnabled();
}