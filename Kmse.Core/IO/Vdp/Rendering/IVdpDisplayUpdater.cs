namespace Kmse.Core.IO.Vdp.Rendering;

public interface IVdpDisplayUpdater
{
    void UpdateDisplay(Span<byte> frame);
}