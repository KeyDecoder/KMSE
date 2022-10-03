using Kmse.Core.IO.Vdp.Rendering;
using Kmse.TestUi;

namespace Kmse.TestUI.Render;

public class WinFormsDisplayUpdater : IVdpDisplayUpdater
{
    private readonly frmMain _form;

    public WinFormsDisplayUpdater(frmMain form)
    {
        _form = form;
    }

    public void DisplayFrame(ReadOnlySpan<byte> frame)
    {
        _form.DrawMainFrame(frame);
    }

    public void DisplayDebugSpriteTable(ReadOnlySpan<byte> frame)
    {
        _form.DrawSpriteDebugFrame(frame);
    }

    public void DisplayDebugSpriteTileMemory(ReadOnlySpan<byte> frame)
    {
        _form.DrawTileMemoryDebugFrame(frame);
    }

    public bool IsDebugSpriteTableEnabled()
    {
        return _form.IsSpriteDebugDisplayEnabled();
    }

    public bool IsDebugSpriteTileMemoryEnabled()
    {
        return _form.IsTileMemoryDebugDisplayEnabled();
    }
}