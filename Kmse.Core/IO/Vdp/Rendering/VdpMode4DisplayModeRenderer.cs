using Kmse.Core.IO.Vdp.Model;
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
            var colours = GetColor(_registers.GetOverscanBackdropColour());
            for (var i = 0; i < _pixels.Span.Length; i += 4)
            {
                _pixels.Span[i] = colours.blue;
                _pixels.Span[i + 1] = colours.green;
                _pixels.Span[i + 2] = colours.red;
                _pixels.Span[i + 3] = colours.alpha;
            }
        }

        public void RenderLine()
        {
            RenderSprites();
            RenderBackground();
        }

        public void UpdateDisplay()
        {            
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

                var patternAddress = _registers.GetSpritePatternGeneratorBaseAddressOffset() +
                                     (sprite.PatternIndex * 32);

                //Each pattern uses 32 bytes.The first four bytes are bitplanes 0 through 3
                //for line 0, the next four bytes are bitplanes 0 through 3 for line 1, etc.,
                //up to line 7.

                // Get offset of 4 bytes for this line
                var patternLineAddress = (ushort)(patternAddress + ((_verticalCounter.Counter - sprite.Y) * 4));
                var colourAddresses = new byte[spriteWidth];

                // This is not correct
                for (var i = 0; i < spriteWidth; i+=2)
                {
                    // Each byte is data for 2 pixels which can select up to 15 colours from palette
                    var data = _ram.ReadRawVideoRam(patternLineAddress);
                    colourAddresses[i] = (byte)(data & 0x0F);
                    colourAddresses[i+1] = (byte)((data & 0xF0) >> 4);

                    patternLineAddress++;
                }

                for (var i = 0; i < spriteWidth; i++)
                {
                    var xCoordinate = sprite.X + i;
                    if (xCoordinate >= 256)
                    {
                        // Sprite is partially off screen, so don't draw the rest of the line
                        break;
                    }

                    // Sprites always use the second palette only so we skip the first 16
                    if (colourAddresses[i] == 0)
                    {
                        // Address is 0 which means it is transparent, so don't write anything
                        continue;
                    }

                    var colourAddress = (ushort)(colourAddresses[i] + 16);
                    var colours = GetColor(colourAddress);

                    var index = GetPixelIndex(sprite.X + i, _verticalCounter.Counter);
                    _pixels.Span[index + 0] = colours.blue;
                    _pixels.Span[index + 1] = colours.green;
                    _pixels.Span[index + 2] = colours.red;
                    _pixels.Span[index + 3] = colours.alpha;
                }
            }
        }

        private void RenderBackground()
        {

        }

        private (byte blue, byte green, byte red, byte alpha) GetColor(ushort colourAddress)
        {
            var color = _ram.ReadRawColourRam(colourAddress);
            var alpha = (byte)0xFF;

            // --BBGGRR
            var red = (byte)(color & 0x03);
            var green = (byte)((color >> 2) & 0x03);
            var blue = (byte)((color >> 4) & 0x03);

            return (ConvertPaletteToColourByte(blue), ConvertPaletteToColourByte(green),
                ConvertPaletteToColourByte(red), alpha);
        }

        private byte ConvertPaletteToColourByte(byte value)
        {
            return value switch
            {
                0 => 0,
                1 => 85,
                2 => 170,
                3 => 255,
                _ => throw new InvalidOperationException($"Colour value {value:X2} is not supported")
            };
        }
    }
}
