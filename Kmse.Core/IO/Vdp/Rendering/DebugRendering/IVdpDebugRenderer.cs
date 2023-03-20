namespace Kmse.Core.IO.Vdp.Rendering.DebugRendering;

public interface IVdpDebugRenderer
{
    void RenderAllSpritesInAddressTable();
    void RenderAllTilesAndSpritesInMemory();
}