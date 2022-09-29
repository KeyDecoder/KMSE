using Kmse.Core.IO.Vdp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;

namespace Kmse.Core.IO.Vdp.Rendering
{
    public class VdpMode4DisplayModeRenderer : IVdpDisplayModeRenderer
    {
        private readonly IVdpRegisters _registers;
        private readonly IVdpRam _ram;
        private readonly IVdpDisplayUpdater _displayUpdater;
        private readonly IVdpVerticalCounter _verticalCounter;
        private readonly IVdpFlags _flags;
        private Memory<byte> _pixels;

        public VdpMode4DisplayModeRenderer(IVdpRegisters registers, IVdpRam ram, IVdpDisplayUpdater displayUpdater, IVdpVerticalCounter verticalCounter, IVdpFlags flags)
        {
            _registers = registers;
            _ram = ram;
            _displayUpdater = displayUpdater;
            _verticalCounter = verticalCounter;
            _flags = flags;
            // Mode 4 is always 256 x 192 pixels
            // Pixels are RGBA but stored as GBRA
            _pixels = new Memory<byte>(new byte[256 * 192 * 4]);
        }

        public void Reset()
        {
            ResetBuffer();
        }

        public void ResetBuffer()
        {
            // TODO: Reset with backdrop color
            _pixels.Span.Fill(0xFF);
        }

        public void RenderLine()
        {
            RenderSprites();
            RenderBackground();
        }

        public void UpdateDisplay()
        {
            // TODO: If mode changes, pass this in so display can change resolution
            _displayUpdater.UpdateDisplay(_pixels.Span);
        }

        private int GetPixelIndex(int x, int y)
        {
            // BGRA8 format
            // Each pixel takes up 4 bytes and always row at a time
            // So...
            // index 0 is x position 1 blue, index 1 is x position 1, y position 0 green, index 2 is x position 1, y position 0 red, index 3 is x position 1, y position 0 alpha
            // index 1 is x position 2 blue, index 1 is x position 1, y position 0 green, index 2 is x position 1, y position 0 red, index 3 is x position 1, y position 0 alpha
            return x * 4 + y * 256 * 4;
        }

        private class SpriteToDraw
        {
            public int SpriteId { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int PatternIndex { get; init; }
        }

        private IList<SpriteToDraw> GetSpritesToDraw()
        {
            var baseAddress = _registers.GetSpriteAttributeTableBaseAddressOffset();
            var spriteHeight = _registers.GetSpriteHeight();
            var spritesToDraw = new List<SpriteToDraw>();

            for (var spriteNumber = 0; spriteNumber < 64; spriteNumber++)
            {
                // Lookup the sprite attribute table to get addresses in memory for sprite information
                // Then load the sprite information from Video RAM

                var patternAddress = baseAddress + 0x81 + ((spriteNumber & 0x3F) << 1);
                var xAddress = baseAddress + 0x80 + ((spriteNumber & 0x3F) << 1);
                var yAddress = baseAddress + (spriteNumber & 0x3F);

                var spriteX = _ram.ReadRawVideoRam((ushort)xAddress);
                var spriteY = _ram.ReadRawVideoRam((ushort)yAddress);
                var patternIndex = _ram.ReadRawVideoRam((ushort)patternAddress);

                // Stop rendering any more sprites if y co-ord is 208 in mode 4 / 192 line mode
                if (spriteY == 0xD0)
                {
                    break;
                }

                // Sprite y is always + 1
                // So if y is 0 then actually means scanline 1
                spriteY++;

                // Check if Sprites Y is in the current line
                // This includes the 8x8 pixels of the sprite
                if (_verticalCounter.Counter < spriteY || _verticalCounter.Counter > spriteY + spriteHeight)
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

                var sprite = new SpriteToDraw
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

        private void RenderSprites()
        {
            var spriteWidth = _registers.GetSpriteWidth();
            var sprites = GetSpritesToDraw();

            // Now we have a list of sprites to draw, draw them all, but draw them in reverse order
            //The inter-sprite priority is defined by the order of the sprites in the
            // internal buffer.An opaque pixel from a lower-entry sprite is displayed
            // over any opaque pixel from a higher-entry sprite.

            //For example: Out of the eight possible sprites, 5 are used.
            //Sprites 1,2,3 have transparent pixels.Sprites 4 and 5 have opaque pixels.
            //Only the pixel from sprite 4 will be shown since it comes before sprite 5.

            if (sprites.Count == 0)
            {
                return;
            }

            for (var spriteNumber = sprites.Count - 1; spriteNumber >= 0; spriteNumber--)
            {
                var sprite = sprites[spriteNumber];

                // TODO: Get sprite pattern and draw it properly
                // Just draw a blob for now
                for (var i = 0; i < spriteWidth; i++)
                {
                    var index = GetPixelIndex(sprite.X + i, _verticalCounter.Counter);
                    _pixels.Span[index + 0] = 0x00;
                    _pixels.Span[index + 1] = 0x00;
                    _pixels.Span[index + 2] = 0x00;
                    _pixels.Span[index + 3] = 0xFF;
                }
            }
        }

        private void RenderBackground()
        {

        }
    }
}
