namespace Kmse.Core.IO.Vdp.Counters;

public interface IVdpHorizontalCounter
{
    int Counter { get; }
    int LatchedCounter { get; }
    void UpdateLatchedCounter();
    byte LatchedCounterAsByte();
    void Increment(int cycles);
    void Reset();
    void ResetLine();
    bool EndOfScanline();
}