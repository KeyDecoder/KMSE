namespace Kmse.Core.IO.Vdp.Model;

[Flags]
public enum VdpStatusFlags : byte
{
    Unused0 = 1 << 0,
    Unused1 = 1 << 1,
    Unused2 = 1 << 2,
    Unused3 = 1 << 3,
    Unused4 = 1 << 4,

    /// <summary>
    ///     COL - Sprite collision
    ///     This flag is set if an opaque pixel from any two sprites overlap. It is
    ///     cleared when the control port is read. For more information see the
    ///     sprites section.
    /// </summary>
    SpriteCollision = 1 << 5,

    /// <summary>
    ///     OVR - Sprite overflow
    ///     This flag is set if there are more than eight sprites that are positioned
    ///     on a single scanline. It is cleared when the control port is read. For more
    ///     information see the sprites section.
    /// </summary>
    SpriteOverflow = 1 << 6,

    /// <summary>
    ///     INT - Frame interrupt pending
    ///     This flag is set on the first line after the end of the active display
    ///     period. It is cleared when the control port is read. For more details,
    ///     see the interrupts section.
    /// </summary>
    FrameInterruptPending = 1 << 7
}