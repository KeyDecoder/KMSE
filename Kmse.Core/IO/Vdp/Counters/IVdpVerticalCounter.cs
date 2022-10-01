using Kmse.Core.IO.Vdp.Model;

namespace Kmse.Core.IO.Vdp.Counters;

public interface IVdpVerticalCounter
{
    byte Counter { get; }
    int RawCounter { get; }
    byte LineCounter { get; }
    bool IsLineInterruptPending { get; }
    void Reset();
    void ResetFrame();
    void ClearLineInterruptPending();
    void SetDisplayType(VdpDisplayType displayType);
    void Increment();
    bool EndOfFrame();
    bool EndOfActiveFrame();
    bool IsInsideActiveFrame();
    void UpdateLineCounter();
    int GetVerticalLineCount();
}