using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.Utilities;
using Kmse.Core.IO.Logging;

namespace Kmse.Core.IO.Vdp.Rendering
{
    public class VdpMode4DisplayModeRenderer : IVdpDisplayModeRenderer
    {
        private readonly IVdpRegisters _registers;
        private readonly IVdpRam _ram;
        private readonly IVdpDisplayUpdater _displayUpdater;
        private readonly IVdpVerticalCounter _verticalCounter;
        private readonly IVdpFlags _flags;
        private readonly IIoPortLogger _ioPortLogger;
        private Memory<byte> _pixels;
        private Memory<byte> _debugSpriteTablePixels;
        private Memory<byte> _debugSpriteTileMemoryPixels;

        public VdpMode4DisplayModeRenderer(IVdpRegisters registers, IVdpRam ram, IVdpDisplayUpdater displayUpdater, IVdpVerticalCounter verticalCounter, IVdpFlags flags, IIoPortLogger ioPortLogger)
        {
            _registers = registers;
            _ram = ram;
            _displayUpdater = displayUpdater;
            _verticalCounter = verticalCounter;
            _flags = flags;
            _ioPortLogger = ioPortLogger;
            // Mode 4 is always 256 x 192 pixels
            // Pixels are RGBA but stored as GBRA
            _pixels = new Memory<byte>(new byte[256 * 192 * 4]);
            _debugSpriteTablePixels = new Memory<byte>(new byte[256 * 192 * 4]);
            _debugSpriteTileMemoryPixels = new Memory<byte>(new byte[256 * 192 * 4]);
        }

        public void Reset()
        {
            ResetBuffer();
        }

        public void ResetBuffer()
        {
            var colours = GetColor(false, _registers.GetOverscanBackdropColour());
            ResetPixels(_pixels, colours.red, colours.green, colours.blue);
        }

        public void RenderLine()
        {
            RenderSprites();
            RenderBackground();
        }

        public void UpdateDisplay()
        {
            RenderAllSpritesInAddressTable();
            RenderAllTilesAndSpritesInMemory();
            _displayUpdater.DisplayFrame(_pixels.Span);
        }

        public void RenderAllSpritesInAddressTable()
        {
            if (!_displayUpdater.IsDebugSpriteTableEnabled())
            {
                return;
            }

            ResetPixels(_debugSpriteTablePixels, 0x00, 0x00, 0x00);

            var pixels = _debugSpriteTablePixels;
            //var spriteWidth = (byte)_registers.GetSpriteWidth();
            var spriteWidth = (byte)8;
            var spriteHeight = (byte)8;
            var baseAddress = _registers.GetSpriteAttributeTableBaseAddressOffset();

            var yOffset = 5;
            var spriteCounter = 0;
            var maxPerLine = 32;

            ResetPixels(_debugSpriteTileMemoryPixels, 0x00, 0x00, 0x00);

            for (var spriteNumber = 0; spriteNumber < 64; spriteNumber++)
            {
                var patternIndexAddress = baseAddress + 0x81 + (spriteNumber * 2);
                var patternIndex = _ram.ReadRawVideoRam((ushort)patternIndexAddress);
                var patternAddress = (ushort)(_registers.GetSpritePatternGeneratorBaseAddressOffset() + (patternIndex * 32));

                var xOffset = (spriteNumber % 32) * spriteWidth;

                // TODO: Handle 16 pixel sprite height?
                for (var y = 0; y < spriteHeight; y++)
                {
                    RenderSpritePatternLine(pixels, true, patternAddress, spriteWidth, xOffset, y, yOffset+y);
                }

                spriteCounter++;
                if (spriteCounter == maxPerLine)
                {
                    spriteCounter = 0;
                    yOffset += spriteHeight;
                }
            }
            _displayUpdater.DisplayDebugSpriteTable(_debugSpriteTablePixels.Span);
        }

        public void RenderAllTilesAndSpritesInMemory()
        {
            if (!_displayUpdater.IsDebugSpriteTileMemoryEnabled())
            {
                return;
            }

            var pixels = _debugSpriteTileMemoryPixels;
            var spriteWidth = (byte)_registers.GetSpriteWidth();
            var yOffset = 5;
            var spriteCounter = 0;
            var maxPerLine = 32;
            var start = 0;
            var end = start + 288;

            ResetPixels(_debugSpriteTileMemoryPixels, 0x00, 0x00, 0x00);

            for (var spriteNumber = start; spriteNumber < end; spriteNumber++)
            {
                var patternAddress = (ushort)(spriteNumber * 32);
                var xOffset = (spriteNumber % 32) * 8;

                for (var y = 0; y < 8; y++)
                {
                    // Since we are rendering all the tiles and sprites, we use the tile palette
                    RenderSpritePatternLine(pixels, false, patternAddress, spriteWidth, xOffset, y, yOffset + y);
                }

                spriteCounter++;
                if (spriteCounter == maxPerLine)
                {
                    spriteCounter = 0;
                    yOffset += 8;
                }
            }
            _displayUpdater.DisplayDebugSpriteTileMemory(_debugSpriteTileMemoryPixels.Span);
        }

        private void RenderSpritePatternLine(Memory<byte> frame, bool spritePalette, ushort patternAddress, byte spriteWidth, int xStart, int spriteYOffset, int yline)
        {
            // Get offset of 4 bytes for this line
            var patternLineAddress = (ushort)(patternAddress + (spriteYOffset * 4));
            var colourAddresses = new byte[spriteWidth];

            // Is this 8 bytes for zoomed/large?
            var lineData = new byte[4];
            lineData[0] = _ram.ReadRawVideoRam(patternLineAddress++);
            lineData[1] = _ram.ReadRawVideoRam(patternLineAddress++);
            lineData[2] = _ram.ReadRawVideoRam(patternLineAddress++);
            lineData[3] = _ram.ReadRawVideoRam(patternLineAddress);

            //Each pattern uses 32 bytes.The first four bytes are bitplanes 0 through 3
            //for line 0, the next four bytes are bitplanes 0 through 3 for line 1, etc.,
            //up to line 7.

            var pixelOffset = 0;
            for (var i = 7; i >= 0; i--)
            {
                var bit = i;
                Bitwise.SetIf(ref colourAddresses[pixelOffset], 0, () => Bitwise.IsSet(lineData[0], bit));
                Bitwise.SetIf(ref colourAddresses[pixelOffset], 1, () => Bitwise.IsSet(lineData[1], bit));
                Bitwise.SetIf(ref colourAddresses[pixelOffset], 2, () => Bitwise.IsSet(lineData[2], bit));
                Bitwise.SetIf(ref colourAddresses[pixelOffset], 3, () => Bitwise.IsSet(lineData[3], bit));
                pixelOffset++;
            }

            for (var i = 0; i < spriteWidth; i++)
            {
                var xCoordinate = xStart + i;
                if (xCoordinate >= 256)
                {
                    // Sprite is partially off screen, so don't draw the rest of the line
                    break;
                }

                // Sprites always use the second palette only so we skip the first 16
                var colourAddress = (ushort)(colourAddresses[i]);
                if (colourAddress == 0)
                {
                    // Address is 0 which means it is transparent, so don't write anything
                    continue;
                }

                var colours = GetColor(spritePalette, colourAddress);

                var index = GetPixelIndex(xCoordinate, yline);
                frame.Span[index + 0] = colours.blue;
                frame.Span[index + 1] = colours.green;
                frame.Span[index + 2] = colours.red;
                frame.Span[index + 3] = colours.alpha;
            }
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
            var largeSprites = _registers.IsSprites8By16() || _registers.IsSprites16By16();

            for (var spriteNumber = 0; spriteNumber < 64; spriteNumber++)
            {
                // Lookup the sprite attribute table to get addresses in memory for sprite information
                // Then load the sprite information from Video RAM

                var yAddress = baseAddress + (spriteNumber & 0x3F);
                var xAddress = baseAddress + 0x80 + spriteNumber * 2;
                var patternAddress = baseAddress + 0x81 + spriteNumber * 2;

                if (largeSprites)
                {
                    // TODO: How to handler large sprites?
                }

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
            var spriteWidth = (byte)_registers.GetSpriteWidth();
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

                var patternAddress = (ushort)(_registers.GetSpritePatternGeneratorBaseAddressOffset() + (sprite.PatternIndex * 32));

                var spriteYOffset = _verticalCounter.Counter - sprite.Y;
                RenderSpritePatternLine(_pixels, true, patternAddress, spriteWidth, sprite.X, spriteYOffset, _verticalCounter.Counter);

                ////Each pattern uses 32 bytes.The first four bytes are bitplanes 0 through 3
                ////for line 0, the next four bytes are bitplanes 0 through 3 for line 1, etc.,
                ////up to line 7.

                //// Get offset of 4 bytes for this line
                //var patternLineAddress = (ushort)(patternAddress + ((_verticalCounter.Counter - sprite.Y) * 4));
                //var colourAddresses = new byte[spriteWidth];

                //// Is this 8 bytes for zoomed/large?
                //var lineData = new byte[4];
                //lineData[0] = _ram.ReadRawVideoRam(patternLineAddress++);
                //lineData[1] = _ram.ReadRawVideoRam(patternLineAddress++);
                //lineData[2] = _ram.ReadRawVideoRam(patternLineAddress++);
                //lineData[3] = _ram.ReadRawVideoRam(patternLineAddress);

                //var pixelOffset = 0;
                //for (var i = 7; i >= 0; i--)
                //{
                //    var bit = i;
                //    Bitwise.SetIf(ref colourAddresses[pixelOffset], 0, () => Bitwise.IsSet(lineData[0], bit));
                //    Bitwise.SetIf(ref colourAddresses[pixelOffset], 1, () => Bitwise.IsSet(lineData[1], bit));
                //    Bitwise.SetIf(ref colourAddresses[pixelOffset], 2, () => Bitwise.IsSet(lineData[2], bit));
                //    Bitwise.SetIf(ref colourAddresses[pixelOffset], 3, () => Bitwise.IsSet(lineData[3], bit));
                //    pixelOffset++;
                //}

                //int zoomShift = (_registers.IsSprites16By16() ? 1 : 0);
                //for (int pixel = 0; pixel < spriteWidth; pixel++)
                //{
                //    int c = (((lineData[0] >> (7 - (pixel >> zoomShift))) & 0x1) << 0);
                //    c |= (((lineData[1] >> (7 - (pixel >> zoomShift))) & 0x1) << 1);
                //    c |= (((lineData[2] >> (7 - (pixel >> zoomShift))) & 0x1) << 2);
                //    c |= (((lineData[3] >> (7 - (pixel >> zoomShift))) & 0x1) << 3);
                //    if (colourAddresses[pixel] != c)
                //    {
                //        Console.WriteLine("bad");
                //    }
                //    //colourAddresses[pixel] = (byte)c;
                //}

                //for (var i = 0; i < spriteWidth; i++)
                //{
                //    var xCoordinate = sprite.X + i;
                //    if (xCoordinate >= 256)
                //    {
                //        // Sprite is partially off screen, so don't draw the rest of the line
                //        break;
                //    }

                //    // Sprites always use the second palette only so we skip the first 16
                //    if (colourAddresses[i] == 0)
                //    {
                //        // Address is 0 which means it is transparent, so don't write anything
                //        continue;
                //    }

                //    var colourAddress = (ushort)(colourAddresses[i] + 16);
                //    var colours = GetColor(colourAddress);

                //    var index = GetPixelIndex(sprite.X + i, _verticalCounter.Counter);
                //    _pixels.Span[index + 0] = colours.blue;
                //    _pixels.Span[index + 1] = colours.green;
                //    _pixels.Span[index + 2] = colours.red;
                //    _pixels.Span[index + 3] = colours.alpha;
                //}
            }
        }

        private void RenderBackground()
        {

        }

        private void ResetPixels(Memory<byte> pixels, byte red, byte green, byte blue)
        {
            for (var i = 0; i < _pixels.Span.Length; i += 4)
            {
                pixels.Span[i] = blue;
                pixels.Span[i + 1] = green;
                pixels.Span[i + 2] = red;
                pixels.Span[i + 3] = 0xFF;
            }
        }

        private (byte blue, byte green, byte red, byte alpha) GetColor(bool secondPalette, ushort colourAddress)
        {
            if (secondPalette)
            {
                colourAddress += 16;
            }
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
