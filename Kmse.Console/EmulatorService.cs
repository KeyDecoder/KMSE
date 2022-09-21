using System.Text;
using Autofac;
using Kmse.Console.Logging;
using Kmse.Core;
using Kmse.Core.IO.Controllers;
using Kmse.Core.Utilities;
using Kmse.Core.Z80.Support;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.Console;

internal class EmulatorService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SerilogCpuLogger _cpuLogger;
    private readonly SerilogIoLogger _ioLogger;
    private readonly ILifetimeScope _scope;
    private readonly ILogger _log;
    private readonly SerilogMemoryLogger _memoryLogger;
    private readonly string _romFilename;

    public EmulatorService(ILifetimeScope scope, ILogger log, Options options, IHostApplicationLifetime applicationLifetime,
        SerilogCpuLogger cpuLogger, SerilogMemoryLogger memoryLogger, SerilogIoLogger ioLogger)
    {
        _scope = scope;
        _log = log;
        _applicationLifetime = applicationLifetime;
        _cpuLogger = cpuLogger;
        _memoryLogger = memoryLogger;
        _ioLogger = ioLogger;
        _romFilename = options.Filename;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await using var emulationScope = _scope.BeginLifetimeScope();

        var masterSystemConsole = emulationScope.Resolve<IMasterSystemConsole>();
        var controllers = emulationScope.Resolve<IControllerPort>();
        stoppingToken.Register(() => masterSystemConsole.PowerOff());

        await masterSystemConsole.LoadCartridge(_romFilename, stoppingToken);
        masterSystemConsole.PowerOn();

        // Run two tasks, one to run the main CPU loop and one to capture key inputs
        // Key inputs is temporary until we wire up proper inputs

        var emulationCancellationTokenSource = new CancellationTokenSource();
        var emulationRunTask = Task.Run(() => RunEmulation(masterSystemConsole, emulationCancellationTokenSource.Token, stoppingToken), stoppingToken);
        await Task.Run(() => HandleInputs(masterSystemConsole, controllers, emulationCancellationTokenSource), stoppingToken);
        await emulationRunTask.WaitAsync(stoppingToken);

        emulationScope.Dispose();

        _log.Information("Emulation stopped");
        System.Console.WriteLine("Press any key to exit");
        System.Console.ReadKey();
        _applicationLifetime.StopApplication();
    }

    private void RunEmulation(IMasterSystemConsole masterSystemConsole, CancellationToken emulationCancellationToken, CancellationToken applicationCancellationToken)
    {
        while (!emulationCancellationToken.IsCancellationRequested && !applicationCancellationToken.IsCancellationRequested)
        {
            // This loop allows us to restart since if powered off then just stays here until we power it back on again
            if (masterSystemConsole.IsRunning())
            {
                masterSystemConsole.Run();
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }

    private void UpdateControllerEmulationForKey(IControllerPort controllerPort, ConsoleKey key, bool inputA, ControllerInputStatus button)
    {
        // TODO: Not particularly efficient since we are setting this to be pressed or not pressed each time
        var pressed = Keyboard.IsKeyDown(key);
        if (inputA)
        {
            controllerPort.ChangeInputAControlState(button, pressed);
        }
        else
        {
            controllerPort.ChangeInputBControlState(button, pressed);
        }
    }

    private void HandleInputs(IMasterSystemConsole masterSystemConsole, IControllerPort controllerPort, CancellationTokenSource emulationCancellationTokenSource)
    {
        System.Console.WriteLine("Esc to exit console app");
        System.Console.WriteLine("TAB to stop/start emulation");
        System.Console.WriteLine("F to turn on/off file logging (default is off)");
        System.Console.WriteLine("P to pause/unpause emulation (directly)");
        System.Console.WriteLine("O to trigger pause button input");
        System.Console.WriteLine("R to trigger reset button input");
        System.Console.WriteLine("L to enable/disable CPU verbose logging");
        System.Console.WriteLine("M to enable/disable Memory verbose logging");
        System.Console.WriteLine("I to enable/disable I/O ports verbose logging");
        System.Console.WriteLine("S to get CPU current status");

        System.Console.WriteLine("Up to trigger controller A up");
        System.Console.WriteLine("Down to trigger controller A down");
        System.Console.WriteLine("Left to trigger controller A left");
        System.Console.WriteLine("Right to trigger controller A right");
        System.Console.WriteLine("Z to trigger controller A left button");
        System.Console.WriteLine("X to trigger controller A right button");

        while (!emulationCancellationTokenSource.IsCancellationRequested)
        {
            // Check the state of the input keys first and then handle regular commands like normal
            // This is so the controller emulation can get key up and down separately
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.Z, true, ControllerInputStatus.LeftButton);
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.X, true, ControllerInputStatus.RightButton);
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.UpArrow, true, ControllerInputStatus.Up);
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.DownArrow, true, ControllerInputStatus.Down);
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.LeftArrow, true, ControllerInputStatus.Left);
            UpdateControllerEmulationForKey(controllerPort, ConsoleKey.RightArrow, true, ControllerInputStatus.Right);
            controllerPort.ChangeResetButtonState(Keyboard.IsKeyDown(ConsoleKey.R));

            if (!System.Console.KeyAvailable)
            {
                Thread.Sleep(1);
                continue;
            }

            var key = System.Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                {
                    _log.Information("Exiting emulation console app");
                    masterSystemConsole.PowerOff();
                    emulationCancellationTokenSource.Cancel();
                    return;
                }
                case ConsoleKey.Tab:
                {
                    if (masterSystemConsole.IsRunning())
                    {
                        _log.Information("Stopping emulation");
                        masterSystemConsole.PowerOff();
                    }
                    else
                    {
                        _log.Information("Starting emulation");
                        masterSystemConsole.PowerOn();
                    }
                }
                    break;
                case ConsoleKey.P:
                {
                    if (masterSystemConsole.IsPaused())
                    {
                        _log.Information("Unpausing emulation");
                        masterSystemConsole.Unpause();
                    }
                    else
                    {
                        _log.Information("Pausing emulation");
                        masterSystemConsole.Pause();
                    }
                }
                    break;
                case ConsoleKey.S:
                {
                    var status = masterSystemConsole.GetCpuStatus();
                    _log.Information($"Flags: {GetStatusFlagsAsString(status)}");
                    _log.Information($"Registers: {GetRegistersAsString(status)}");
                }
                    break;
                case ConsoleKey.L:
                {
                    _log.Information("Enabling/Disabling CPU logging");
                    _cpuLogger.EnableDisableVerboseLogging();
                }
                    break;
                case ConsoleKey.M:
                {
                    _log.Information("Enabling/Disabling Memory logging");
                    _memoryLogger.EnableDisableVerboseLogging();
                }
                    break;
                case ConsoleKey.I:
                {
                    _log.Information("Enabling/Disabling I/O logging");
                    _ioLogger.EnableDisableVerboseLogging();
                }
                    break;
                case ConsoleKey.O:
                {
                    _log.Information("Hitting Console Pause Button");
                    masterSystemConsole.TriggerPauseButton();
                }
                    break;
            }
        }

        masterSystemConsole.PowerOff();
    }



    private string GetStatusFlagsAsString(CpuStatus status)
    {
        var flags = new[] { "C", "N", "P/V", "X", "H", "X", "Z", "S" };
        var flagString = new StringBuilder();
        for (var i = 0; i < flags.Length; i++)
        {
            flagString.Append(Bitwise.IsSet(status.Af.Low, i) ? flags[i] : ".");
        }

        return flagString.ToString();
    }

    private string GetRegistersAsString(CpuStatus status)
    {
        var data = $"A: {status.Af.High:X2} ";
        data += $"BC: {status.Bc.Word:X4} ";
        data += $"DE: {status.De.Word:X4} ";
        data += $"HL: {status.Hl.Word:X4} ";
        data += $"IX: {status.Ix.Word:X4} ";
        data += $"IY: {status.Iy.Word:X4} ";
        data += $"PC: {status.Pc:X4} ";
        data += $"SP: {status.StackPointer:X4} ";
        data += $"I: {status.IRegister:X2} ";
        data += $"R: {status.RRegister:X2} ";
        data += $"Halt: {status.Halted}";
        return data;
    }
}