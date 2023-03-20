using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.Utilities;
using Kmse.Core.IO.Logging;
using System.Drawing;
using Kmse.Core.IO.Vdp.Rendering.Sprites;

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
        private readonly ISpriteRenderInformation _spriteRenderInformation;
        private Memory<byte> _pixels;
        private Memory<PixelStatus> _pixelInformation;
        private int _currentYFineScrollValue;
        private int _maxScreenWidth = 256;
        private int _maxScreenHeight = 192;

        private enum PixelStatus
        {
            Transparent,
            BackgroundLowPriority,
            BackgroundHighPriority,
            Sprite
        }

        public VdpMode4DisplayModeRenderer(IVdpRegisters registers, IVdpRam ram, IVdpDisplayUpdater displayUpdater, IVdpVerticalCounter verticalCounter, IVdpFlags flags, IIoPortLogger ioPortLogger, ISpriteRenderInformation spriteRenderInformation)
        {
            _registers = registers;
            _ram = ram;
            _displayUpdater = displayUpdater;
            _verticalCounter = verticalCounter;
            _flags = flags;
            _ioPortLogger = ioPortLogger;
            _spriteRenderInformation = spriteRenderInformation;
            // Mode 4 is always 256 x 192 pixels
            // Pixels are RGBA but stored as GBRA
            _pixels = new Memory<byte>(new byte[_maxScreenWidth * _maxScreenHeight * 4]);
            _pixelInformation = new Memory<PixelStatus>(new PixelStatus[_maxScreenWidth * _maxScreenHeight]);
        }

        public void Reset()
        {
            ResetBuffer();
        }

        public void ResetBuffer()
        {
            var colors = _ram.GetColor(_registers.GetOverscanBackdropColour(), false);
            ResetPixels(_pixels, colors.red, colors.green, colors.blue);
            _pixelInformation.Span.Fill(PixelStatus.Transparent);
            _currentYFineScrollValue = _registers.GetBackgroundYFineScrollValue();
        }

        public void RenderLine()
        {
            //RenderBackground();
            //RenderSpriteLine();
            
            // Render the sprites first, this makes it easier to determine sprite collision and easier to draw background since don't need to keep track of what high priority background pixels
            RenderSpriteLine();
            RenderBackground();
        }

        public void UpdateDisplay()
        {
            _displayUpdater.DisplayFrame(_pixels.Span);
        }

        private void RenderBackground()
        {
            var nameTableBaseAddress = _registers.GetNameTableBaseAddressOffset();
            var startingColumn = _registers.GetBackgroundXStartingColumn();
            var xFineScrollValue = _registers.GetBackgroundXFineScrollValue();
            var startingRow = _registers.GetBackgroundYStartingRow();
            var maskColumn0 = _registers.MaskColumn0WithOverscanColor();
            var backdropColour = _registers.GetOverscanBackdropColour();

            // Background is 32 8x8 tiles across each line
            // For each line, we draw the line of each tile

            if (_verticalCounter.RawCounter > _maxScreenHeight)
            {
                // Gone below active display
                return;
            }

            // Tile index depends on the tile line we are on x 32
            var tileRow = ((byte)Math.Floor((double)_verticalCounter.RawCounter / 8));

            if (_registers.IsHorizontalScrollingEnabledForRows0To15() && tileRow < 2)
            {
                // Scrolling is disabled for rows 0 - 15 or top two sets of tiles, so set starting column and fine scroll value to 0
                xFineScrollValue = 0;
                startingColumn = 0;
            }

            var tileIndex = (tileRow * 32) + (32 - startingColumn) + (32 - startingRow);
            for (var column = 0; column < 32; column++)
            {
                // Tile index in name table starts at start column and wraps at 32
                var tileInformation = _ram.GetTileInformation(nameTableBaseAddress, tileIndex);

                //MSB LSB
                //---pcvhnnnnnnnnn

                //    - = Unused.Some games use these bits as flags for collision and damage
                //zones. (such as Wonderboy in Monster Land, Zillion 2)
                //p = Priority flag.When set, sprites will be displayed underneath the background pattern in question.
                //    c = Palette select.
                //    v = Vertical flip flag.
                //    h = Horizontal flip flag.
                //    n = Pattern index, any one of 512 patterns in VRAM can be selected.
                var highPriority = Bitwise.IsSet(tileInformation, 12);
                var secondPalette = Bitwise.IsSet(tileInformation, 11);
                var verticalFlip = Bitwise.IsSet(tileInformation, 10);
                var horizontalFlip = Bitwise.IsSet(tileInformation, 9);
                var patternIndex = (byte)(tileInformation & 0xFF);
                var patternAddress = (ushort)(patternIndex * 32);

                var yFineScrollValue = _currentYFineScrollValue;
                if (_registers.IsVerticalScrollingEnabledForColumns24To31() && column >= 24)
                {
                    // Scrolling disabled for last 8 columns
                    yFineScrollValue = 0;
                }
                
                var xOffset = (column * 8) + xFineScrollValue;
                // Wrap around if goes off the edge due to fine scroll
                xOffset %= _maxScreenWidth;
                var yOffset = _verticalCounter.RawCounter;
                var tileYOffset = (_verticalCounter.RawCounter - tileRow * 8) + yFineScrollValue;

                if (xOffset >= 0)
                {
                    RenderTilePatternLine(patternAddress, false, secondPalette, xOffset, yOffset, 8, tileYOffset, highPriority);
                }

                if (maskColumn0 && column == 0)
                {
                    // Mask first column out and leave as background colour
                    // Note that we override what was there since some backgrounds will draw partially as they scroll horizontally 
                    // and this hides that
                    for (var i = 0; i < 8; i++)
                    {
                        RenderPixel(i, _verticalCounter.RawCounter, backdropColour, false);
                    }

                    // Mark as high priority background since sprites cannot display over this
                    SetPixelStatus(xOffset, yOffset, PixelStatus.BackgroundHighPriority);
                    continue;
                }

                tileIndex++;
            }
        }

        private void RenderSpriteLine()
        {
            var sprites = _spriteRenderInformation.GetSpritesToDraw();

            // Now we have a list of sprites to draw, draw them all, but draw them in reverse order
            // The inter-sprite priority is defined by the order of the sprites in the
            // internal buffer.An opaque pixel from a lower-entry sprite is displayed
            // over any opaque pixel from a higher-entry sprite.

            // For example: Out of the eight possible sprites, 5 are used.
            // Sprites 1,2,3 have transparent pixels.Sprites 4 and 5 have opaque pixels.
            // Only the pixel from sprite 4 will be shown since it comes before sprite 5.

            if (sprites.Count == 0 || _verticalCounter.RawCounter > _maxScreenHeight)
            {
                return;
            }

            var spriteWidth = (byte)_registers.GetSpriteWidth();
            for (var spriteNumber = sprites.Count - 1; spriteNumber >= 0; spriteNumber--)
            {
                var sprite = sprites[spriteNumber];
                var patternAddress = (ushort)(_registers.GetSpritePatternGeneratorBaseAddressOffset() + (sprite.PatternIndex * 32));
                var spriteYOffset = _verticalCounter.RawCounter - sprite.Y;
                RenderTilePatternLine(patternAddress, true, true, sprite.X, _verticalCounter.RawCounter, spriteWidth, spriteYOffset, false);
            }
        }

        private bool IsPixelBackgroundColor(int x, int y)
        {
            var colours = _ram.GetColor(_registers.GetOverscanBackdropColour(), false);
            var index = GetPixelIndex(x, y);

            return _pixels.Span[index + 0] == colours.blue &&
            _pixels.Span[index + 1] == colours.green && 
            _pixels.Span[index + 2] == colours.red &&
            _pixels.Span[index + 3] == colours.alpha;
        }

        private void RenderTilePatternLine(ushort patternAddress, bool isSprite, bool useSecondPalette, int x, int y, byte tileWidth, int tileYOffset, bool highPriorityBackground)
        {
            var tile = _ram.GetTile(patternAddress, tileYOffset, tileWidth);

            for (var i = 0; i < tileWidth; i++)
            {
                var xCoordinate = x + i;
                if (xCoordinate >= _maxScreenWidth)
                {
                    // if Tile is partially off screen, so don't draw the rest of the line for the tile
                    break;
                }

                var colourAddress = tile[i];
                if (colourAddress == 0)
                {
                    // Address is 0 which means it is transparent, so don't write anything
                    continue;
                }

                if (isSprite)
                {
                    // If not matching background color, we are overriding an existing sprite pixel which indicates a collision
                    // Note that this relies on the sprites being drawn before the background otherwise this would trigger on background pixels
                    if (!IsPixelBackgroundColor(xCoordinate, y))
                    {
                        _flags.SetFlag(VdpStatusFlags.SpriteCollision);

                        var colours = _ram.GetColor(_registers.GetOverscanBackdropColour(), false);
                        var index = GetPixelIndex(xCoordinate, y);

                        var blue = _pixels.Span[index + 0];
                        var green = _pixels.Span[index + 1];
                        var red = _pixels.Span[index + 2];
                        var alpha = _pixels.Span[index + 3];
                    }
                }

                if (!isSprite && !highPriorityBackground && !IsPixelBackgroundColor(xCoordinate, y))
                {
                    // If not a high priority background and the pixel has already been drawn, then do not draw over it
                    // Sprites have priority over low priority background items

                    var colours = _ram.GetColor(_registers.GetOverscanBackdropColour(), false);
                    var index = GetPixelIndex(xCoordinate, y);

                    var blue = _pixels.Span[index + 0];
                    var green = _pixels.Span[index + 1];
                    var red = _pixels.Span[index + 2];
                    var alpha = _pixels.Span[index + 3];

                    continue;
                }

                //if (usePixelInformation)
                //{
                //    var status = GetPixelStatus(xCoordinate, yline);
                //    if (status == PixelStatus.Transparent)
                //    {
                //        if (isSprite)
                //        {
                //            SetPixelStatus(xCoordinate, yline, PixelStatus.Sprite);
                //        }
                //        else
                //        {
                //            SetPixelStatus(xCoordinate, yline,
                //                tileHighPriority
                //                    ? PixelStatus.BackgroundHighPriority
                //                    : PixelStatus.BackgroundLowPriority);
                //        }

                //        // Allow it to draw
                //    }
                //    else if (isSprite && status == PixelStatus.Sprite)
                //    {
                //        // Don't draw over top of sprites at all
                //        // Assume drawn from highest priority to lowest priority order
                //        // Also sprite overlap, flag sprite collision
                //        _flags.SetFlag(VdpStatusFlags.SpriteCollision);
                //        continue;
                //    }
                //    else if (isSprite && status == PixelStatus.BackgroundHighPriority)
                //    {
                //        // Don't draw over background high priority
                //        continue;
                //    }
                //}

                RenderPixel(xCoordinate, y, colourAddress, useSecondPalette);
            }
        }

        private void RenderPixel(int x, int y, ushort colourAddress, bool useSecondPalette)
        {
            var colours = _ram.GetColor(colourAddress, useSecondPalette);
            var index = GetPixelIndex(x, y);

            _pixels.Span[index + 0] = colours.blue;
            _pixels.Span[index + 1] = colours.green;
            _pixels.Span[index + 2] = colours.red;
            _pixels.Span[index + 3] = colours.alpha;
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
    }
}
