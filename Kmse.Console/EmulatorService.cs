using Kmse.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.Console;

internal class EmulatorService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger _log;
    private readonly IMasterSystemConsole _masterSystemConsole;
    private readonly string _romFilename;

    public EmulatorService(ILogger log, Options options, IMasterSystemConsole masterSystemConsole,
        IHostApplicationLifetime applicationLifetime)
    {
        _log = log;
        _masterSystemConsole = masterSystemConsole;
        _applicationLifetime = applicationLifetime;
        _romFilename = options.Filename;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        stoppingToken.Register(() => _masterSystemConsole.PowerOff());

        await _masterSystemConsole.LoadCartridge(_romFilename, stoppingToken);
        _masterSystemConsole.PowerOn();

        // Run two tasks, one to run the main CPU loop and one to capture key inputs
        // Key inputs is temporary until we wire up proper inputs

        var task = Task.Run(() => { _masterSystemConsole.Run(); }, stoppingToken);

        var controlTask = Task.Run(() =>
        {
            System.Console.WriteLine("Press Enter key to stop");
            System.Console.ReadLine();
            _masterSystemConsole.PowerOff();
        });

        await Task.WhenAll(task, controlTask);

        _log.Information("Stopping");
        System.Console.WriteLine("Press any key to exit");
        System.Console.ReadKey();
        _applicationLifetime.StopApplication();
    }
}