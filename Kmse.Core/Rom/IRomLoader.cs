namespace Kmse.Core.Rom;

public interface IRomLoader
{
    Task<bool> LoadRom(string filename, CancellationToken cancellationToken);
    Span<byte> CurrentRomData();
}