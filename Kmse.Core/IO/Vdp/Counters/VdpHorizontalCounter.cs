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
        return (byte)(LatchedCounter >> 1 & 0xFF);
    }

    public void Increment()
    {
        Counter++;
    }

    public void Reset()
    {
        Counter = 0;
        LatchedCounter = 0;
    }

    public void ResetLine()
    {
        Counter = 0;
    }

    public bool EndOfScanline()
    {
        return Counter >= GetHorizontalLineCount();
    }

    public int GetHorizontalLineCount()
    {
        return 342;
    }
}