using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.Utilities;

namespace Kmse.Core.IO.Vdp.Rendering.DebugRendering
{
    public class VdpDebugRenderer : IVdpDebugRenderer
    {
        private readonly IVdpRegisters _registers;
        private readonly IVdpRam _ram;
        private readonly IVdpDisplayUpdater _displayUpdater;
        private readonly IVdpFlags _flags;
        private readonly IIoPortLogger _ioPortLogger;
        private readonly Memory<byte> _debugSpriteTablePixels;
        private readonly Memory<byte> _debugSpriteTileMemoryPixels;

        public VdpDebugRenderer(IVdpRegisters registers, IVdpRam ram, IVdpDisplayUpdater displayUpdater, IVdpFlags flags, IIoPortLogger ioPortLogger)
        {
            _registers = registers;
            _ram = ram;
            _displayUpdater = displayUpdater;
            _flags = flags;
            _ioPortLogger = ioPortLogger;
            // Since this is debug only, we don't change this even if rendering mode is changed since we are just rendering a view of the tiles/sprites
            _debugSpriteTablePixels = new Memory<byte>(new byte[256 * 192 * 4]);
            _debugSpriteTileMemoryPixels = new Memory<byte>(new byte[256 * 192 * 4]);
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
                    RenderTilePatternLine(_debugSpriteTablePixels, true, patternAddress, spriteWidth, xOffset, y, yOffset+y, true);
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
            const int end = 447;
            var yOffset = 0;
            var spriteCounter = 0;

            ResetPixels(_debugSpriteTileMemoryPixels, 0x00, 0x00, 0x00);

            for (var spriteNumber = start; spriteNumber < end; spriteNumber++)
            {
                var patternAddress = (ushort)(spriteNumber * 32);
                var xOffset = (spriteNumber % 32) * 8;

                for (var y = 0; y < 8; y++)
                {
                    // Since we are rendering all the tiles and sprites, we use the tile palette
                    RenderTilePatternLine(_debugSpriteTileMemoryPixels, false, patternAddress, spriteWidth, xOffset, y, yOffset + y, true);
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

        private void RenderTilePatternLine(Memory<byte> frame, bool secondPalette, ushort patternAddress, byte tileWidth, int xStart, int tileYOffset, int yline, bool isSprite)
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

        private void ResetPixels(Memory<byte> pixels, byte red, byte green, byte blue)
        {
            for (var i = 0; i < pixels.Span.Length; i += 4)
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
