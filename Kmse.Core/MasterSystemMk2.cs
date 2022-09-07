using System.Diagnostics;
using Kmse.Core.Cartridge;
using Kmse.Core.IO;
using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Support;

namespace Kmse.Core;

public class MasterSystemMk2 : IMasterSystemConsole
{
    // https://segaretro.org/Sega_Master_System/Technical_specifications
    // Maximum frame rates
    private const double FrameRateNtsc = 59.922743;
    private const double FrameRatePal = 49.701459;

    // Clock rates - https://www.smspower.org/Development/ClockRate
    // 53.6931MHz for NTSC
    // 53.2034MHz for PAL
    private const double MasterClockCrystalRateNtsc = 53.6931 * 1000000;
    private const double MasterClockCrystalRatePal = 53.2034 * 1000000;
    private const double CpuClockDivider = 15.0;

    private readonly IMasterSystemCartridge _cartridge;
    private readonly IMasterSystemMemory _memory;
    private readonly ICpuLogger _cpuLogger;
    private readonly IControllerPort _controllers;
    private readonly IZ80Cpu _cpu;
    private readonly IDebugConsolePort _debugConsole;

    private readonly DisplayType _displayType = DisplayType.Pal;
    private readonly IMasterSystemIoManager _io;
    private readonly ISoundPort _sound;
    private readonly IVdpPort _vdp;

    private bool _paused;
    private bool _running;

    public MasterSystemMk2(IZ80Cpu cpu, IMasterSystemIoManager io, IVdpPort vdp, IControllerPort controllers,
        ISoundPort sound, IDebugConsolePort debugConsole, IMasterSystemCartridge cartridge, IMasterSystemMemory memory, ICpuLogger cpuLogger)
    {
        _cpu = cpu;
        _io = io;
        _vdp = vdp;
        _controllers = controllers;
        _sound = sound;
        _debugConsole = debugConsole;
        _cartridge = cartridge;
        _memory = memory;
        _cpuLogger = cpuLogger;
        _cpu.Initialize(_memory, _io);
        _io.Initialize(_vdp, _controllers, _sound, _debugConsole);
    }

    public async Task<bool> LoadCartridge(string filename, CancellationToken cancellationToken)
    {
        // Loading a new cart automatically powers this off
        PowerOff();
        var result = await _cartridge.LoadRomFromFile(filename, cancellationToken);
        if (!result)
        {
            return false;
        }
        _memory.LoadCartridge(_cartridge);

        return true;
    }

    public void PowerOn()
    {
        _cpuLogger.Debug("CPU Powering On");
        Reset();
        _paused = false;
        _running = true;
    }

    public void PowerOff()
    {
        _cpuLogger.Debug("CPU Powering Off");
        _running = false;
    }

    public void Pause()
    {
        _cpuLogger.Debug("CPU Emulation Pause");
        _paused = true;
    }

    public void Unpause()
    {
        _cpuLogger.Debug("CPU Emulation Unpause");
        _paused = false;
    }

    public bool IsRunning()
    {
        return _running;
    }

    public bool IsPaused()
    {
        return _paused;
    }

    public void Run()
    {
        var stopWatch = Stopwatch.StartNew();

        while (_running)
        {
            if (_paused)
            {
                Thread.Sleep(1);
                continue;
            }

            // We only want to run the target number of frames per second
            // To do this, we work out the time per frame (how long does a frame run for)
            // Then we work out how many CPU cycles run per frame
            // Then run the CPU that until that many cycles have run (note it may run more cycles in some cases)
            // If any time left over, we just wait until the next frame

            stopWatch.Restart();
            var startTime = stopWatch.ElapsedMilliseconds;
            var timePerFrame = 1000 / GetDisplayFrameRate();

            // Run CPU the total number of times per frame
            var cpuCyclesPerFrame = GetClockCyclesPerFrame() / CpuClockDivider;
            var totalCycles = 0;
            while (totalCycles < cpuCyclesPerFrame)
            {
                var cpuCycles = _cpu.ExecuteNextCycle();

                // TODO: Update VDP
                // TODO: Update Sound chip
                // TODO: Process any inputs from controllers and update controller port

                totalCycles += cpuCycles;

                if (_paused || !_running)
                {
                    break;
                }
            }

            while (stopWatch.ElapsedMilliseconds - startTime < timePerFrame) Thread.Sleep(1);

            // TODO: Calculate actual running frame rate for diagnostics
        }

        stopWatch.Stop();
    }

    public CpuStatus GetCpuStatus()
    {
        return _cpu.GetStatus();
    }

    private double GetDisplayFrameRate()
    {
        return _displayType == DisplayType.Ntsc ? FrameRateNtsc : FrameRatePal;
    }

    private double GetClockCyclesPerFrame()
    {
        var frameRate = GetDisplayFrameRate();
        var clockRate = _displayType == DisplayType.Ntsc ? MasterClockCrystalRateNtsc : MasterClockCrystalRatePal;
        return clockRate / frameRate;
    }

    private void Reset()
    {
        _cpu.Reset();
        _io.Reset();
        _vdp.Reset();
        _controllers.Reset();
        _debugConsole.Reset();
    }

    private enum DisplayType
    {
        Ntsc,
        Pal
    }
}