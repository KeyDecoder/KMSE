using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Flags;

public interface IVdpFlags
{
    VdpStatusFlags Flags { get; }
    void Reset();
    void SetFlag(VdpStatusFlags flags);
    void ClearFlag(VdpStatusFlags flags);
    void SetClearFlagConditional(VdpStatusFlags flags, bool condition);
    bool IsFlagSet(VdpStatusFlags flags);
    void ClearAllFlags();
}