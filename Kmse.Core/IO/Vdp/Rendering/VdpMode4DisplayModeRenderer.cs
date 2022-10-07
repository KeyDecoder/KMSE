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
        private Memory<PixelStatus> _pixelInformation;

        private enum PixelStatus
        {
            Transparent,
            BackgroundLowPriority,
            BackgroundHighPriority,
            Sprite
        }

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
            _pixelInformation = new Memory<PixelStatus>(new PixelStatus[256 * 192]);
        }

        public void Reset()
        {
            ResetBuffer();
        }

        public void ResetBuffer()
        {
            var colours = GetColor(false, _registers.GetOverscanBackdropColour());
            ResetPixels(_pixels, colours.red, colours.green, colours.blue);
            _pixelInformation.Span.Fill(PixelStatus.Transparent);
        }

        public void RenderLine()
        {
            RenderBackground();
            RenderSprites();
        }

        public void UpdateDisplay()
        {
            RenderAllSpritesInAddressTable();
            RenderAllTilesAndSpritesInMemory();
            _displayUpdater.DisplayFrame(_pixels.Span);
        }

        private void RenderBackground()
        {
            var nameTableBaseAddress = _registers.GetNameTableBaseAddressOffset();
            var startingColumn = _registers.GetBackgroundXStartingColumn();
            var fineScrollValue = _registers.GetBackgroundXFineScrollValue();
            var yScroll = _registers.GetBackgroundYScroll();
            var maskColumn0 = _registers.MaskColumn0WithOverscanColor();
            var column0Mask = _registers.GetOverscanBackdropColour();

            // Background is 32 8x8 tiles across each line
            // For each line, we draw the line of each tile

            if (_verticalCounter.RawCounter > 192)
            {
                // Gone below active display
                return;
            }

            // Tile index depends on the tile line we are on x 32
            var tileRow = ((byte)Math.Floor((double)_verticalCounter.RawCounter / 8));

            if (_registers.IsHorizontalScrollingEnabledForRows0To15() && tileRow < 2)
            {
                // Scrolling is disabled for rows 0 - 15 or top two sets of tiles, so set starting column and fine scroll value to 0
                fineScrollValue = 0;
                startingColumn = 0;
            }

            var tileIndex = (tileRow * 32) + (32 - startingColumn);
            for (var column = 0; column < 32; column++)
            {
                // Tile index in name table starts at start column and wraps at 32
                var address = (ushort)(nameTableBaseAddress + (tileIndex * 2));
                var firstByte = _ram.ReadRawVideoRam(address);
                address++;
                var secondByte = _ram.ReadRawVideoRam(address);
                var tileInformation = Bitwise.ToUnsigned16BitValue(secondByte, firstByte);

                //MSB LSB
                //---pcvhnnnnnnnnn

                //    - = Unused.Some games use these bits as flags for collision and damage
                //zones. (such as Wonderboy in Monster Land, Zillion 2)
                //p = Priority flag.When set, sprites will be displayed underneath the background pattern in question.
                //    c = Palette select.
                //    v = Vertical flip flag.
                //    h = Horizontal flip flag.
                //    n = Pattern index, any one of 512 patterns in VRAM can be selected.
                var priority = Bitwise.IsSet(tileInformation, 12);
                var secondPalette = Bitwise.IsSet(tileInformation, 11);
                var verticalFlip = Bitwise.IsSet(tileInformation, 10);
                var horizontalFlip = Bitwise.IsSet(tileInformation, 9);
                var patternIndex = (byte)(tileInformation & 0xFF);
                var patternAddress = (ushort)(patternIndex * 32);

                var xOffset = (column * 8) + fineScrollValue;
                // Wrap around if goes off the edge due to fine scroll
                xOffset %= 256;
                var yOffset = _verticalCounter.RawCounter;
                var tileYOffset = _verticalCounter.RawCounter - tileRow * 8;

                if (xOffset >= 0)
                {
                    RenderTilePatternLine(_pixels, secondPalette, patternAddress, 8, xOffset, tileYOffset, yOffset,
                        false, priority, true);
                }

                if (maskColumn0 && column == 0)
                {
                    // Mask first column out and leave as background colour
                    // Note that we override what was there since some backgrounds will draw partially as they scroll horizontally 
                    // and this hides that
                    var colours = GetColor(secondPalette, column0Mask);
                    for (var i = 0; i < 8; i++)
                    {
                        var index = GetPixelIndex(i, _verticalCounter.RawCounter);
                        _pixels.Span[index + 0] = colours.blue;
                        _pixels.Span[index + 1] = colours.green;
                        _pixels.Span[index + 2] = colours.red;
                        _pixels.Span[index + 3] = colours.alpha;
                    }

                    // Mark as high priority background since sprites cannot display over this
                    SetPixelStatus(xOffset, yOffset, PixelStatus.BackgroundHighPriority);
                    continue;
                }

                tileIndex++;
            }
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

                var spriteYOffset = _verticalCounter.RawCounter - sprite.Y;
                RenderTilePatternLine(_pixels, true, patternAddress, spriteWidth, sprite.X, spriteYOffset, _verticalCounter.RawCounter, true, false, true);
            }
        }

        public void RenderAllSpritesInAddressTable()
        {
            if (!_displayUpdater.IsDebugSpriteTableEnabled())
            {
                return;
            }

            ResetPixels(_debugSpriteTablePixels, 0x00, 0x00, 0x00);

            // Since we are dumping all the sprites out, we don't adjust for size or zoom, just dump out all the 8x8 sprite tiles
            const byte spriteWidth = 8;
            const byte spriteHeight = 8;
            const int maxPerLine = 32;

            var baseAddress = _registers.GetSpriteAttributeTableBaseAddressOffset();
            var yOffset = 5;
            var spriteCounter = 0;

            ResetPixels(_debugSpriteTileMemoryPixels, 0x00, 0x00, 0x00);

            for (var spriteNumber = 0; spriteNumber < 64; spriteNumber++)
            {
                var patternIndexAddress = baseAddress + 0x81 + (spriteNumber * 2);
                var patternIndex = _ram.ReadRawVideoRam((ushort)patternIndexAddress);
                var patternAddress = (ushort)(_registers.GetSpritePatternGeneratorBaseAddressOffset() + (patternIndex * 32));

                var xOffset = (spriteNumber % 32) * spriteWidth;

                for (var y = 0; y < spriteHeight; y++)
                {
                    RenderTilePatternLine(_debugSpriteTablePixels, true, patternAddress, spriteWidth, xOffset, y, yOffset+y, true, false, false);
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

            // Since we are dumping all the tiles out, we don't adjust for size or zoom, just dump out all the 8x8 tiles
            const byte spriteWidth = 8;
            const int maxPerLine = 32;
            const int start = 0;
            const int end = start + 288;
            var yOffset = 5;
            var spriteCounter = 0;

            ResetPixels(_debugSpriteTileMemoryPixels, 0x00, 0x00, 0x00);

            for (var spriteNumber = start; spriteNumber < end; spriteNumber++)
            {
                var patternAddress = (ushort)(spriteNumber * 32);
                var xOffset = (spriteNumber % 32) * 8;

                for (var y = 0; y < 8; y++)
                {
                    // Since we are rendering all the tiles and sprites, we use the tile palette
                    RenderTilePatternLine(_debugSpriteTileMemoryPixels, true, patternAddress, spriteWidth, xOffset, y, yOffset + y, true, false, false);
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

        private void RenderTilePatternLine(Memory<byte> frame, bool secondPalette, ushort patternAddress, byte tileWidth, int xStart, int tileYOffset, int yline, bool isSprite, bool tileHighPriority, bool usePixelInformation)
        {
            // Get offset of 4 bytes for this line
            var patternLineAddress = (ushort)(patternAddress + (tileYOffset * 4));
            var colourAddresses = new byte[tileWidth];

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

            for (var i = 0; i < tileWidth; i++)
            {
                var xCoordinate = xStart + i;
                if (xCoordinate >= 256 || yline > 192)
                {
                    // if Tile is partially off screen, so don't draw the rest of the line for the tile
                    break;
                }

                var colourAddress = (ushort)(colourAddresses[i]);
                if (colourAddress == 0)
                {
                    // Address is 0 which means it is transparent, so don't write anything
                    continue;
                }
                var colours = GetColor(secondPalette, colourAddress);

                if (usePixelInformation)
                {
                    var status = GetPixelStatus(xCoordinate, yline);
                    if (status == PixelStatus.Transparent)
                    {
                        if (isSprite)
                        {
                            SetPixelStatus(xCoordinate, yline, PixelStatus.Sprite);
                        }
                        else
                        {
                            SetPixelStatus(xCoordinate, yline,
                                tileHighPriority
                                    ? PixelStatus.BackgroundHighPriority
                                    : PixelStatus.BackgroundLowPriority);
                        }

                        // Allow it to draw
                    }
                    else if (isSprite && status == PixelStatus.Sprite)
                    {
                        // Don't draw over top of sprites at all
                        // Assume drawn from highest priority to lowest priority order
                        // Also sprite overlap, flag sprite collision
                        _flags.SetFlag(VdpStatusFlags.SpriteCollision);
                        continue;
                    }
                    else if (isSprite && status == PixelStatus.BackgroundHighPriority)
                    {
                        // Don't draw over background high priority
                        continue;
                    }
                }

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

        private PixelStatus GetPixelStatus(int x, int y)
        {
            var offset = x + y * 256;
            return _pixelInformation.Span[offset];
        }

        private void SetPixelStatus(int x, int y, PixelStatus status)
        {
            var offset = x + y * 256;
            _pixelInformation.Span[offset] = status;
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

                var yAddress = baseAddress + (spriteNumber & 0x3F);
                var xAddress = baseAddress + 0x80 + spriteNumber * 2;
                var patternAddress = baseAddress + 0x81 + spriteNumber * 2;

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
