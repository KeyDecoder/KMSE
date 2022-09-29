using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Flags;

public class VdpFlags : IVdpFlags
{
    public VdpStatusFlags Flags { get; private set; }

    public void Reset()
    {
        Flags = 0x00;
    }

    public void ClearAllFlags()
    {
        Flags = 0x00;
    }

    public void SetFlag(VdpStatusFlags flags)
    {
        Flags |= flags;
    }

    public void ClearFlag(VdpStatusFlags flags)
    {
        Flags &= ~flags;
    }

    public void SetClearFlagConditional(VdpStatusFlags flags, bool condition)
    {
        if (condition)
        {
            SetFlag(flags);
        }
        else
        {
            ClearFlag(flags);
        }
    }

    public bool IsFlagSet(VdpStatusFlags flags)
    {
        var currentSetFlags = Flags & flags;
        return currentSetFlags == flags;
    }
}