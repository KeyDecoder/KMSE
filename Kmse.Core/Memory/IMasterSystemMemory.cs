using Kmse.Core.Cartridge;

namespace Kmse.Core.Memory;

public interface IMasterSystemMemory
{
    void LoadCartridge(IMasterSystemCartridge masterSystemCartridge);
    byte this[ushort address] { get; set; }

    // TODO: Add methods to load/save current RAM memory for loading/saving games
}