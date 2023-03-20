namespace Kmse.Core.IO.Vdp.Rendering.Sprites;

public interface ISpriteRenderInformation
{
    IReadOnlyList<SpriteRenderData> GetSpritesToDraw();
}