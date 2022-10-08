﻿using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.Z80.Interrupts;

public class Z80InterruptManagement : IZ80InterruptManagement
{
    private ICpuLogger _cpuLogger;
    private readonly IZ80MemoryRefreshRegister _refreshRegister;
    private readonly IZ80ProgramCounter _programCounter;

    public Z80InterruptManagement(IZ80ProgramCounter programCounter, ICpuLogger cpuLogger, IZ80MemoryRefreshRegister refreshRegister)
    {
        _programCounter = programCounter;
        _cpuLogger = cpuLogger;
        _refreshRegister = refreshRegister;
    }

    /// <summary>
    ///     Disables interrupts from being accepted if set to False
    ///     IFF1
    /// </summary>
    public bool InterruptEnableFlipFlopStatus { get; private set; }

    /// <summary>
    ///     Temporary storage location for interrupt enable flip flip (IFF1) above
    ///     IFF2
    /// </summary>
    public bool InterruptEnableFlipFlopTempStorageStatus { get; private set; }

    public bool MaskableInterruptDelay { get; private set; }

    /// <summary>
    ///     Current Interrupt mode, used for maskable interrupts
    /// </summary>
    public byte InterruptMode { get; private set; }

    /// <summary>
    /// Status of Non Maskable Interrupt, this is set to true if a non-maskable interrupt has been triggered
    /// </summary>
    public bool NonMaskableInterrupt { get; private set; }

    /// <summary>
    /// Status of a Maskable Interrupt, this is set to true if a maskable interrupt has been triggered
    /// </summary>
    public bool MaskableInterrupt { get; private set; }

    public void Reset()
    {
        ClearMaskableInterrupt();
        ClearNonMaskableInterrupt();

        InterruptEnableFlipFlopStatus = false;
        InterruptEnableFlipFlopTempStorageStatus = false;
        InterruptMode = 0;
    }

    public bool InterruptWaiting()
    {
        // When an EI instruction is executed, any pending interrupt request is not accepted until after the
        // instruction following EI is executed.This single instruction delay is necessary when the
        // next instruction is a return instruction.
        if (MaskableInterruptDelay)
        {
            MaskableInterruptDelay = false;
            return false;
        }

        return NonMaskableInterrupt || (InterruptEnableFlipFlopStatus && MaskableInterrupt);
    }

    public void SetMaskableInterrupt()
    {
        if (!InterruptEnableFlipFlopStatus)
        {
            // Can only set if interrupts are enabled
            return;
        }
        MaskableInterrupt = true;
        _cpuLogger.SetMaskableInterruptStatus(MaskableInterrupt);
    }

    public void ClearMaskableInterrupt()
    {
        MaskableInterrupt = false;
        _cpuLogger.SetMaskableInterruptStatus(MaskableInterrupt);
    }

    public void SetNonMaskableInterrupt()
    {
        NonMaskableInterrupt = true;
        _cpuLogger.SetNonMaskableInterruptStatus(NonMaskableInterrupt);
    }

    public void ClearNonMaskableInterrupt()
    {
        NonMaskableInterrupt = false;
        _cpuLogger.SetNonMaskableInterruptStatus(NonMaskableInterrupt);
    }

    public void EnableMaskableInterrupts()
    {
        InterruptEnableFlipFlopStatus = true;
        InterruptEnableFlipFlopTempStorageStatus = true;
        // When enabling interrupts, we always skip handling the interrupt for the the first instruction after an EI
        MaskableInterruptDelay = true;
        ClearMaskableInterrupt();
    }

    public void DisableMaskableInterrupts()
    {
        InterruptEnableFlipFlopStatus = false;
        InterruptEnableFlipFlopTempStorageStatus = false;
        ClearMaskableInterrupt();
    }

    public void SetInterruptMode(byte mode)
    {
        if (mode > 2)
        {
            throw new InvalidOperationException("Z80 CPU interrupt mode must be set to 0, 1 or 2");
        }

        InterruptMode = mode;
    }

    public void ResetInterruptEnableFlipFlopFromTemporaryStorage()
    {
        InterruptEnableFlipFlopStatus = InterruptEnableFlipFlopTempStorageStatus;
    }

    public int ProcessInterrupts()
    {
        if (NonMaskableInterrupt)
        {
            return ProcessNonMaskableInterrupt();
        }

        if (InterruptEnableFlipFlopStatus && MaskableInterrupt)
        {
            return ProcessMaskableInterrupt();
        }

        return 0;
    }

    private int ProcessNonMaskableInterrupt()
    {
        _cpuLogger.LogInstruction(_programCounter.Value, "NMI", "Non Maskable Interrupt", "Non Maskable Interrupt",
            string.Empty);

        _refreshRegister.Increment(1);

        // Copy state of IFF1 into IFF2 to keep a copy and reset IFF1 so processing can continue without a masked interrupt occuring
        // This gets copied back with a RETN occurs
        InterruptEnableFlipFlopTempStorageStatus = InterruptEnableFlipFlopStatus;
        InterruptEnableFlipFlopStatus = false;

        // We have to clear this here to avoid this triggering in every cycle
        // but not sure this is accurate since if another NMI is triggered this could end up in an endless loop instead
        ClearNonMaskableInterrupt();

        // Handle NMI by jumping to 0x66
        _programCounter.SetAndSaveExisting(0x66);

        return 11;
    }

    private int ProcessMaskableInterrupt()
    {
        _cpuLogger.LogInstruction(_programCounter.Value, "MI", "Maskable Interrupt", "Maskable Interrupt",
            $"Mode {InterruptMode}");

        _refreshRegister.Increment(1);

        DisableMaskableInterrupts();

        if (InterruptMode is 0 or 1)
        {
            // The SMS hardware generates two types of interrupts: IRQs and NMIs.
            // An IRQ is a maskable interrupt which may be generated by:
            // * the VSync impulse which occurs when a frame has been rasterised, or:
            // * a scanline counter falling below zero(see the VDP Register 10 description
            // for details)

            // For the SMS 2, Game Gear, and Genesis, the value $FF is always read from
            // the data bus, which corresponds to the instruction 'RST 38H'.
            // Basically mode 0 is the same as mode 1

            // Mode 1, jump to address 0x0038h
            _programCounter.SetAndSaveExisting(0x0038);
            return 11;
        }

        // Mode 2 is not used in SMS since ports don't set a byte on data bus
        //https://www.smspower.org/uploads/Development/richard.txt
        // Maybe it is used but with random values returned?
        //https://www.smspower.org/uploads/Development/smstech-20021112.txt
        _cpuLogger.Error("Maskable Interrupt while in mode 2 which is not supported");

        // Treat this as a NOP which is 4 cycles
        return 4;
    }
}