namespace Kmse.Core.Z80.Interrupts;

public interface IZ80InterruptManagement
{
    bool InterruptEnableFlipFlopStatus { get; }
    bool InterruptEnableFlipFlopTempStorageStatus { get; }
    byte InterruptMode { get; }
    bool NonMaskableInterrupt { get; }
    bool MaskableInterrupt { get; }

    void Reset();
    bool InterruptWaiting();

    void SetMaskableInterrupt();
    void ClearMaskableInterrupt();
    void SetNonMaskableInterrupt();
    void ClearNonMaskableInterrupt();

    void EnableMaskableInterrupts();
    void DisableMaskableInterrupts();
    void SetInterruptMode(byte mode);
    void ResetInterruptEnableFlipFlopFromTemporaryStorage();

    int ProcessInterrupts();
}