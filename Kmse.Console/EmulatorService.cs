using Kmse.Core.Rom;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.Console;

internal class EmulatorService : BackgroundService
{
    private readonly ILogger _log;
    private readonly IRomLoader _romLoader;
    private readonly string _romFilename;

    public EmulatorService(ILogger log, Options options, IRomLoader romLoader)
    {
        _log = log;
        _romLoader = romLoader;
        _romFilename = options.Filename;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _romLoader.LoadRom(_romFilename, stoppingToken);
    }
}