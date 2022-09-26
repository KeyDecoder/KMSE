using Kmse.Core.Utilities;

namespace Kmse.Core.IO.Vdp;

/// <summary>
///     Helper class for handling VDP registers
/// </summary>
/// <remarks>
///     https://www.smspower.org/uploads/Development/msvdp-20021112.txt - Registers section
/// </remarks>
public class VdpRegisters : IVdpRegisters
{
    // We store the VDP registers in byte form here since they all do slightly different things
    // Also some data is spread across multiple registers, so easier to store this in raw form and add methods 
    // to allow easier access to the data
    // Easier to test as well
    private byte[] _vdpRegisters;

    public VdpRegisters()
    {
        _vdpRegisters = new byte[11];
    }

    public void Reset()
    {
        _vdpRegisters = new byte[11];
    }

    public void SetRegister(int index, byte value)
    {
        if (index is < 0 or > 11)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Invalid register index {index}");
        }

        _vdpRegisters[index] = value;
    }

    public byte[] DumpRegisters()
    {
        return _vdpRegisters.ToArray();
    }

    public bool IsVerticalScrollingEnabledForColumns24To31()
    {
        return !Bitwise.IsSet(_vdpRegisters[0], 7);
    }

    public bool IsHorizontalScrollingEnabledForRows0To1()
    {
        return !Bitwise.IsSet(_vdpRegisters[0], 6);
    }

    public bool MaskColumn0WithOverscanColor()
    {
        return Bitwise.IsSet(_vdpRegisters[0], 5);
    }

    public bool IsLineInterruptEnabled()
    {
        return Bitwise.IsSet(_vdpRegisters[0], 4);
    }

    public bool ShiftSpritesLeftBy8Pixels()
    {
        return Bitwise.IsSet(_vdpRegisters[0], 3);
    }

    public VdpVideoMode GetVideoMode()
    {
        byte value = 0;
        Bitwise.SetOrClearIf(ref value, 3, () => Bitwise.IsSet(_vdpRegisters[0], 2));
        Bitwise.SetOrClearIf(ref value, 2, () => Bitwise.IsSet(_vdpRegisters[1], 3));
        Bitwise.SetOrClearIf(ref value, 1, () => Bitwise.IsSet(_vdpRegisters[0], 1));
        Bitwise.SetOrClearIf(ref value, 0, () => Bitwise.IsSet(_vdpRegisters[1], 4));

        return value switch
        {
            0 => VdpVideoMode.Graphic1,
            1 => VdpVideoMode.Text,
            2 => VdpVideoMode.Graphic2,
            3 => VdpVideoMode.Mode1Plus2,
            4 => VdpVideoMode.MultiColour,
            5 => VdpVideoMode.Mode1Plus3,
            6 => VdpVideoMode.Mode2Plus3,
            7 => VdpVideoMode.Mode1Plus2Plus3,
            8 => VdpVideoMode.Mode4,
            9 => throw new InvalidOperationException("Invalid text mode for mode 4"),
            10 => VdpVideoMode.Mode4,
            11 => VdpVideoMode.Mode4With224Lines,
            12 => VdpVideoMode.Mode4,
            13 => throw new InvalidOperationException("Invalid text mode for mode 4"),
            14 => VdpVideoMode.Mode4With240Lines,
            15 => VdpVideoMode.Mode4,
            _ => throw new InvalidOperationException("Mode is not valid")
        };
    }

    public bool IsNoSyncAndMonochrome()
    {
        return Bitwise.IsSet(_vdpRegisters[0], 0);
    }

    public bool IsDisplayVisible()
    {
        return Bitwise.IsSet(_vdpRegisters[1], 6);
    }

    public bool IsFrameInterruptEnabled()
    {
        return Bitwise.IsSet(_vdpRegisters[1], 5);
    }

    public bool IsSprites16By16()
    {
        return Bitwise.IsSet(_vdpRegisters[1], 1);
    }

    public bool IsSprites8By16()
    {
        return Bitwise.IsSet(_vdpRegisters[1], 1);
    }

    public bool IsSpritePixelsDoubledInSize()
    {
        return Bitwise.IsSet(_vdpRegisters[1], 0);
    }

    public ushort GetNameTableBaseAddressOffset()
    {
        var mode = GetVideoMode();
        if (mode is VdpVideoMode.Mode4With224Lines or VdpVideoMode.Mode4With240Lines)
        {
            var option = (_vdpRegisters[2] >> 2) & 0x03;
            return option switch
            {
                0 => 0x0700,
                1 => 0x1700,
                2 => 0x2700,
                3 => 0x3700,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return (ushort)((_vdpRegisters[2] & 0x0E) << 10);
    }

    public ushort GetSpriteAttributeTableBaseAddressOffset()
    {
        // Bits 8 to 13 of table base address but only from bits 1-6 so only bitshift left by 7
        return (ushort)((_vdpRegisters[5] & 0x7E) << 7);
    }

    public ushort GetSpritePatternGeneratorBaseAddressOffset()
    {
        // Bits 13 of base address but since bit 2 shift up by 11
        return (ushort)((_vdpRegisters[6] & 0x04) << 11);
    }

    public byte GetOverscanBackdropColour()
    {
        return (byte)(_vdpRegisters[7] & 0x0F);
    }

    public byte GetBackgroundXScroll()
    {
        return _vdpRegisters[8];
    }

    public byte GetBackgroundYScroll()
    {
        return _vdpRegisters[9];
    }

    public byte GetLineCounterValue()
    {
        return _vdpRegisters[10];
    }
}