using System.IO.Abstractions;
using Serilog;

namespace Kmse.Core.Rom;

public class RomLoader : IRomLoader
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private Memory<byte> _currentRom;
    private bool _dumpHeader;

    public RomLoader(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<bool> LoadRom(string filename, CancellationToken cancellationToken)
    {
        _logger.Information("Loading ROM from file {Filename}", filename);
        if (!_fileSystem.File.Exists(filename))
        {
            _logger.Error("ROM file {Filename} does not exist", filename);
            return false;
        }

        var data = await _fileSystem.File.ReadAllBytesAsync(filename, cancellationToken);

        _currentRom = new Memory<byte>(data);

        // Check for a 512 header at the front sometimes added by ROM dumping programs and strip it off if found
        // Easiest way to find is if the length is not a mod of 0x4k pages
        if (data.Length % 0x4000 == 512)
        {
            _logger.Information("ROM contains dump program header, stripping off first 512 bytes of ROM image");
            _dumpHeader = true;
        }

        return true;
    }

    public Span<byte> CurrentRomData()
    {
        return _dumpHeader ? _currentRom.Span.Slice(512, _currentRom.Length - 512) : _currentRom.Span;
    }
}