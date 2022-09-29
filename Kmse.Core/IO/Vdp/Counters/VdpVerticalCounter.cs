using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Registers;

namespace Kmse.Core.IO.Vdp.Counters;

public class VdpVerticalCounter : IVdpVerticalCounter
{
    private readonly IVdpRegisters _registers;
    private bool _secondVCount;
    private int _internalCounter;
    private VdpDisplayType _displayType;
    public byte Counter { get; private set; }
    public byte LineCounter { get; private set; }
    public bool IsLineInterruptPending { get; private set; }

    public VdpVerticalCounter(IVdpRegisters registers)
    {
        _registers = registers;
    }

    public void Reset()
    {
        Counter = 0;
        _secondVCount = false;
    }

    public void ResetFrame()
    {
        Reset();
    }

    public void ClearLineInterruptPending()
    {
        IsLineInterruptPending = false;
    }

    public void SetDisplayType(VdpDisplayType displayType)
    {
        _displayType = displayType;
    }

    public void Increment()
    {
        // These jumps change depending on mode and display type
        Counter++;
        if (_secondVCount)
        {
            return;
        }

        // Quick implementation, but this needs to be mapped using a better data structure to make this easier
        // TODO: Support the other modes, this only works with PAL 192 lines for now
        if (Counter == 0xF3)
        {
            Counter = 0xBA;
            _secondVCount = true;
        }
    }

    public bool EndOfFrame()
    {
        // The increment will adjust to always make this end up at 0xFF as the last line in a complete frame (active and inactive)
        return Counter == 0xFF;
    }

    public bool EndOfActiveFrame()
    {
        return !_secondVCount && Counter == GetActiveFrameSize();
    }

    public bool IsInsideActiveFrame()
    {
        return Counter < GetActiveFrameSize();
    }

    public void UpdateLineCounter()
    {
        // Apply line counter when drawing active display
        // Otherwise simply load from VDP Register 10 ready for the next active display screen

        if (Counter < GetVerticalLineCount() + 1)
        {
            // Decrement the line counter and when zero, trigger a line interrupt
            // This is used to notify applications when reach specific line in rendering
            var underflow = LineCounter == 0;

            LineCounter--;
            if (!underflow)
            {
                return;
            }

            IsLineInterruptPending = true;
        }

        LineCounter = _registers.GetLineCounterValue();
    }

    public int GetVerticalLineCount()
    {
        return _displayType switch
        {
            VdpDisplayType.Ntsc => 262,
            VdpDisplayType.Pal => 313,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private int GetActiveFrameSize()
    {
        // TODO: If inside active display then this should use cached value, only use video mode from registers when inside inactive display
        var currentMode = _registers.GetVideoMode();
        return currentMode switch
        {
            VdpVideoMode.Mode4With224Lines => 224,
            VdpVideoMode.Mode4With240Lines => 240,
            _ => 192
        };
    }
}