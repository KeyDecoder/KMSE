using Kmse.Core.Cartridge;
using Kmse.Core.Memory;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.Console;

internal class EmulatorService : BackgroundService
{
    private readonly ILogger _log;
    private readonly Func<IMasterSystemCartridge> _masterSystemCartridgeFactory;
    private readonly IMasterSystemMemory _memory;
    private readonly string _romFilename;

    public EmulatorService(ILogger log, Options options, Func<IMasterSystemCartridge> masterSystemCartridgeFactory, IMasterSystemMemory memory)
    {
        _log = log;
        _masterSystemCartridgeFactory = masterSystemCartridgeFactory;
        _memory = memory;
        _romFilename = options.Filename;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cartridge = _masterSystemCartridgeFactory();
        await cartridge.LoadRomFromFile(_romFilename, stoppingToken);
        _memory.LoadCartridge(cartridge);

        // Read and write some memory for testing
        System.Console.WriteLine(_memory[0]);
        System.Console.WriteLine(_memory[1]);
        System.Console.WriteLine(_memory[2]);
        _memory[0xE000] = 0x01;
    }
}