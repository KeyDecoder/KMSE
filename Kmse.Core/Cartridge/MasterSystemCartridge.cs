using System.IO.Abstractions;
using Serilog;

namespace Kmse.Core.Cartridge;

public class MasterSystemCartridge : IMasterSystemCartridge
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private Memory<byte> _processedRom;
    private Memory<byte> _rawRom;
    public bool HasDumpHeader { get; private set; }

    public MasterSystemCartridge(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<bool> LoadRomFromFile(string filename, CancellationToken cancellationToken)
    {
        _logger.Information("Loading ROM from file {Filename}", filename);
        if (!_fileSystem.File.Exists(filename))
        {
            _logger.Error("ROM file {Filename} does not exist", filename);
            return false;
        }

        var data = await _fileSystem.File.ReadAllBytesAsync(filename, cancellationToken);
        return LoadRom(data);
    }

    public byte this[int address] => ReadMemory(address);

    public int Length => _processedRom.Length;

    private bool LoadRom(byte[] data)
    {
        _rawRom = new Memory<byte>(data);

        // Check for a 512 header at the front sometimes added by ROM dumping programs and strip it off if found
        // Easiest way to find is if the length is not a mod of 0x4k pages
        if (data.Length % 0x4000 == 512)
        {
            _logger.Information("ROM contains dump program header, stripping off first 512 bytes of ROM image");
            _processedRom = _rawRom.Slice(512, data.Length - 512);
            HasDumpHeader = true;
        }
        else
        {
            _processedRom = _rawRom;
            HasDumpHeader = false;
        }

        return true;
    }

    private byte ReadMemory(int address)
    {
        if (address > _processedRom.Length)
        {
            _logger.Error("Attempt to read past end of available cartridge memory (request: {Address}, ROM length: {Length}", address, _processedRom.Length);
            return 0x00;
        }

        return _processedRom.Span[address];
    }
}