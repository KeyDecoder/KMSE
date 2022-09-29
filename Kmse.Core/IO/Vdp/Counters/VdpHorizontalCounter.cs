namespace Kmse.Core.IO.Vdp.Counters;

public class VdpHorizontalCounter : IVdpHorizontalCounter
{
    public int Counter { get; private set; }
    public int LatchedCounter { get; private set; }

    public void UpdateLatchedCounter()
    {
        LatchedCounter = Counter;
    }

    public byte LatchedCounterAsByte()
    {
        return (byte)((LatchedCounter >> 1) & 0xFF);
    }

    public void Increment(int cycles)
    {
        Counter += cycles;
    }

    public void Reset()
    {
        Counter = 0;
        LatchedCounter = 0;
    }

    public void ResetLine()
    {
        // Since cycles can go over 228, we add cycles from last line to next line counter
        Counter -= 228;
    }

    public bool EndOfScanline()
    {
        // Z80 cycles per VDP cycle is CPU divided by 3 and renders 1 pixel per 2 cycles
        // Z80 cycles per scanline = Z80 cycles per VDP cycle * VDP cycles per pixel * pixels per scanline = 1/3 * 2 * 342 = 228 (exactly)
        return Counter >= 228;
    }
}