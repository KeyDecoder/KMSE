using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;

namespace Kmse.Core.IO.Vdp.Rendering.Sprites;

public class SpriteRenderInformation : ISpriteRenderInformation
{
    private readonly IVdpFlags _flags;
    private readonly IVdpRam _ram;
    private readonly IVdpRegisters _registers;
    private readonly IVdpVerticalCounter _verticalCounter;

    public SpriteRenderInformation(IVdpRegisters registers, IVdpRam ram, IVdpFlags flags,
        IVdpVerticalCounter verticalCounter)
    {
        _registers = registers;
        _ram = ram;
        _flags = flags;
        _verticalCounter = verticalCounter;
    }

    public IReadOnlyList<SpriteRenderData> GetSpritesToDraw()
    {
        var baseAddress = _registers.GetSpriteAttributeTableBaseAddressOffset();
        var spriteHeight = _registers.GetSpriteHeight();
        var spritesToDraw = new List<SpriteRenderData>();
        var videoMode = _registers.GetVideoMode();

        for (var spriteNumber = 0; spriteNumber < 64; spriteNumber++)
        {
            // Lookup the sprite attribute table to get addresses in memory for sprite information
            // Then load the sprite information from Video RAM

            var (spriteX, spriteY, patternIndex) = _ram.GetSpriteInformation(baseAddress, spriteNumber);

            // Stop rendering any more sprites if y co-ord is 208/0xD0 in any mode with 192 lines
            if (videoMode != VdpVideoMode.Mode4With224Lines && videoMode != VdpVideoMode.Mode4With240Lines &&
                spriteY == 0xD0)
            {
                break;
            }

            // Sprite y is always + 1
            // So if y is 0 then actually means scanline 1
            spriteY++;

            // Check if Sprites Y is in the current line
            // This includes the body pixels of the sprite
            if (_verticalCounter.RawCounter < spriteY || _verticalCounter.RawCounter >= spriteY + spriteHeight)
            {
                // Not on line, move to next
                continue;
            }

            // If more than 8 sprites, this is considered an sprite overflow
            // Set the flag and exit
            if (spritesToDraw.Count == 8)
            {
                _flags.SetFlag(VdpStatusFlags.SpriteOverflow);
                break;
            }

            // Note, we still need to add the sprites since even if screen blanked
            // since have to raise sprite overflow regardless of display or what the sprite is

            // Display is blanked, so skip since no need to draw
            if (!_registers.IsDisplayVisible())
            {
                continue;
            }

            if (_registers.ShiftSpritesLeftBy8Pixels())
            {
                spriteX -= 8;
            }

            var sprite = new SpriteRenderData
            {
                SpriteId = spriteNumber,
                X = spriteX,
                Y = spriteY,
                PatternIndex = patternIndex
            };
            spritesToDraw.Add(sprite);
        }

        return spritesToDraw;
    }
}