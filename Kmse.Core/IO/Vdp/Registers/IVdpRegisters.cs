using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Registers;

public interface IVdpRegisters
{
    void Reset();
    void SetRegister(int index, byte value);
    byte[] DumpRegisters();
    bool IsVerticalScrollingEnabledForColumns24To31();
    bool IsHorizontalScrollingEnabledForRows0To1();
    bool MaskColumn0WithOverscanColor();
    bool IsLineInterruptEnabled();
    bool ShiftSpritesLeftBy8Pixels();
    VdpVideoMode GetVideoMode();
    bool IsNoSyncAndMonochrome();
    bool IsDisplayVisible();
    bool IsFrameInterruptEnabled();
    bool IsSprites16By16();
    bool IsSprites8By16();
    int GetSpriteWidth();
    int GetSpriteHeight();
    bool IsSpritePixelsDoubledInSize();
    ushort GetNameTableBaseAddressOffset();
    ushort GetSpriteAttributeTableBaseAddressOffset();
    ushort GetSpritePatternGeneratorBaseAddressOffset();
    byte GetOverscanBackdropColour();
    byte GetBackgroundXScroll();
    byte GetBackgroundXStartingColumn();
    byte GetBackgroundXFineScrollValue();
    byte GetBackgroundYScroll();


    byte GetLineCounterValue();
}