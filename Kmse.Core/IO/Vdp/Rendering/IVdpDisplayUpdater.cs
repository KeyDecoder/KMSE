namespace Kmse.Core.IO.Vdp.Rendering;

public interface IVdpDisplayUpdater
{
    // TODO: If mode changes, pass the size through update display so display can change resolution
    void DisplayFrame(ReadOnlySpan<byte> frame);
    void DisplayDebugSpriteTable(ReadOnlySpan<byte> frame);
    void DisplayDebugSpriteTileMemory(ReadOnlySpan<byte> frame);
    bool IsDebugSpriteTableEnabled();
    bool IsDebugSpriteTileMemoryEnabled();
}