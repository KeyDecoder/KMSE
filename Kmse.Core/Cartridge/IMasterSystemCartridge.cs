namespace Kmse.Core.Cartridge;

public interface IMasterSystemCartridge
{
    Task<bool> LoadRomFromFile(string filename, CancellationToken cancellationToken);
    bool HasDumpHeader { get; }
    int Length { get; }
    byte this[ushort address] { get; }
}