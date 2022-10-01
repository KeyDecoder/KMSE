namespace Kmse.Core.IO.Vdp.Rendering;

public interface IVdpDisplayModeRenderer
{
    void Reset();
    void ResetBuffer();
    void RenderLine();
    void UpdateDisplay();
    void RenderAllSpritesInAddressTable();
    void RenderAllTilesAndSpritesInMemory();
}