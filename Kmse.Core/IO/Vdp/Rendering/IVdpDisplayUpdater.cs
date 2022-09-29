namespace Kmse.Core.IO.Vdp.Rendering;

public interface IVdpDisplayUpdater
{
    // TODO: If mode changes, pass the size through update display so display can change resolution
    void UpdateDisplay(Span<byte> frame);
}