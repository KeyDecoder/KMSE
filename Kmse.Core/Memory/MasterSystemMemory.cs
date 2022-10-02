using Kmse.Core.Cartridge;
using Kmse.Core.Utilities;
using Serilog;

namespace Kmse.Core.Memory;

public class MasterSystemMemory : IMasterSystemMemory
{
    // Memory map

    // https://www.smspower.org/Development/MemoryMap
    // https://www.smspower.org/uploads/Development/smstech-20021112.txt - Section 2

    // Pages can be swapped in and out at any point
    // The first 0x400 bytes of slot 1 does not get paged out ever since the interrupt handler routines are at this address space and they need to always be present so interrupts can get handled correctly.
    // This means that address range 0x0-0x400 is always data of page0 0x0-0x400 regardless of what is paged into slot 0

    // You cannot write to address range 0x0-0x8000 this is because this region is for ROM You can only page data in and out of this area
    // Slot 0x8000-0xBFFF (Slot 3) is also treated as read only memory unless one of the two RAM banks is paged in there

    //0x0000-0x3FFF : ROM Slot 1
    //0x4000-0x7FFF : Rom Slot 2
    //0x8000-0xBFFF : Rom Slot 3 / RAM Slot
    //0xC000-0xDFFF : RAM
    //0xE000-0xFFFF : Mirrored RAM

    //The memory mapping registers are address 0xFFFC-0xFFFF

    //0xFFFC: Memory Control Register
    //0xFFFD: Writing a value to this address maps that values page into slot 1
    //0xFFFE: Writing a value to this address maps that values page into slot 2
    //0xFFFF: Writing a value to this address maps that values page into slot 3

    private const ushort MemoryPageSize = 0x4000;
    private const ushort RamBankBankSize = 0x4000;
    private const ushort MemorySlot1 = 0x00;
    private const ushort MemorySlot2 = MemorySlot1 + MemoryPageSize;
    private const ushort MemorySlot3 = MemorySlot2 + MemoryPageSize;
    private const ushort MemoryControlRegister = 0xFFFC;
    private const ushort MemoryControlRegisterSlot1 = 0xFFFD;
    private const ushort MemoryControlRegisterSlot2 = 0xFFFE;
    private const ushort MemoryControlRegisterSlot3 = 0xFFFF;
    private const ushort MirrorOffset = 0x2000;

    /// <summary>
    ///     Internal RAM memory, maximum memory map size is 0xFFFF
    /// </summary>
    private readonly Memory<byte> _internalRAM = new(new byte[0xFFFF + 1]);

    private readonly ILogger _log;
    private readonly IMemoryLogger _memoryLogger;

    private readonly Memory<byte> _ramBank0 = new(new byte[RamBankBankSize]);
    private readonly Memory<byte> _ramBank1 = new(new byte[RamBankBankSize]);
    private IMasterSystemCartridge _cartridgeROM;
    private int _currentRamBank = -1;
    private int _firstBankPage;
    private int _secondBankPage = 1;
    private int _thirdBankPage = 2;

    private readonly bool _isCodeMasters = false;
    private bool _oneMegCartridge;
    private byte _pagingControl;

    public MasterSystemMemory(ILogger log, IMemoryLogger memoryLogger)
    {
        _log = log;
        _memoryLogger = memoryLogger;
    }

    public void LoadCartridge(IMasterSystemCartridge masterSystemCartridge)
    {
        Reset();

        // If rom size is larger than 0x80000 (524288) then it's a one meg cartridge so needs special handling
        _oneMegCartridge = masterSystemCartridge.Length > 0x80000;
        _cartridgeROM = masterSystemCartridge;

        //TODO: Check if codemasters since this is special as well

        // Normally SMS would copy the first three pages of the ROM into internal memory
        // Each page is 0x4000 bytes so this is 0xC000 bytes
        // However since we access the ROM directly in the read memory method below, we can essentially skip this here
    }

    public byte this[ushort address]
    {
        get => ReadMemory(address);
        set => WriteMemory(address, value);
    }

    public int GetMaximumAvailableMemorySize()
    {
        return _internalRAM.Length;
    }

    public int GetMinimumAvailableMemorySize()
    {
        if (IsRamBankEnabled())
        {
            // If paging enabled, we can access RAM down to the start of the 3rd memory slot
            // since being used as RAM bank
            return MemorySlot3;
        }
        else
        {
            // If paging not enabled, then all three memory slots are for ROM usage only so cannot be accessed
            return MemorySlot3 + MemoryPageSize;
        }
    }

    private void Reset()
    {
        _oneMegCartridge = false;
        _cartridgeROM = null;
        _pagingControl = 0x00;
        _currentRamBank = -1;
        _firstBankPage = 0;
        _secondBankPage = 1;
        _thirdBankPage = 2;
        _internalRAM.Span.Fill(0x00);
        _ramBank0.Span.Fill(0x00);
        _ramBank1.Span.Fill(0x00);
}

    private bool IsRamBankEnabled()
    {
        return Bitwise.IsSet(_pagingControl, 3);
    }

    private byte ReadRam(ushort address)
    {
        var data = _internalRAM.Span[address];
        _memoryLogger.MemoryRead(address, data);
        return data;
    }

    private void WriteRam(ushort address, byte data)
    {
        var oldData = _internalRAM.Span[address];
        _internalRAM.Span[address] = data;
        _memoryLogger.MemoryWrite(address, oldData, data);
    }

    private byte ReadRom(int address)
    {
        var data = _cartridgeROM[address];
        _memoryLogger.CartridgeRead(address, data);
        return data;
    }

    private byte ReadRamBank(int bank, ushort address)
    {
        var ramBank = bank == 0 ? _ramBank0: _ramBank1;
        var data = ramBank.Span[address];
        _memoryLogger.RamBankMemoryRead(bank, address, data);
        return data;
    }

    private void WriteRamBank(int bank, ushort address, byte data)
    {
        var ramBank = bank == 0 ? _ramBank0 : _ramBank1;
        var oldData = ramBank.Span[address];
        ramBank.Span[address] = data;
        _memoryLogger.RamBankMemoryWrite(bank, address, oldData, data);
    }

    private void WriteMemory(ushort address, byte data)
    {
        if (_isCodeMasters)
        {
            throw new NotImplementedException("Codemasters ROMs are not supported");
        }

        switch (address)
        {
            case < MemorySlot3:
                // Cannot write to first two memory slots at all since ROM space only
                _memoryLogger.Error("Attempted to write to Memory Slot 1 or 2 which is not allowed");
                return;

            // Allow writing to 3rd slot if RAM bank is mapped to slot 3
            case < MemorySlot3 + MemoryPageSize when IsRamBankEnabled():
                if (_currentRamBank is 0 or 1)
                {
                    WriteRamBank(_currentRamBank, (ushort)(address & 0x3FFF), data);
                }
                else
                {
                    _memoryLogger.Error($"Attempted to write RAM bank when disabled");
                    throw new InvalidOperationException($"Attempted to write RAM bank when disabled");
                }

                return;
            // Cannot write to slot 3 since RAM banking not enabled so this is used for ROM pages only
            case < MemorySlot3 + MemoryPageSize:
                _memoryLogger.Error("Attempted to write to ROM Memory Slot 3 with RAM banking disabled");
                return;
        }

        WriteRam(address, data);

        // Handle standard memory paging
        if (address >= MemoryControlRegister)
        {
            // Writing memory control page registers
            if (_isCodeMasters)
            {
                throw new NotImplementedException("Codemasters ROM not supported");
            }

            // Set memory page since writing to memory control registers
            WriteToMemoryControlRegisters(address, data);
        }

        // Handle mirroring of RAM
        // RAM from $C000 -$DFFF is mirrored to $E000 - $FFFF

        if (address < 0xE000)
        {
            // Anything written to normal memory gets written to mirror, hence add mirror offset
            WriteRam((ushort)(address + MirrorOffset), data);
        }
        else
        {
            // Anything written to mirror gets written to original memory, hence minus mirror offset
            WriteRam((ushort)(address - MirrorOffset), data);
        }
    }

    private byte ReadMemory(ushort address)
    {
        var readAddress = address;

        if (!_isCodeMasters && readAddress < 0x400)
        {
            // Fixed memory in cartridge that never changes so can return directly since no paging look ups required
            return ReadRom(readAddress);
        }

        if (readAddress < MemorySlot1 + MemoryPageSize)
        {
            // 0 - 0x4000
            // Reading data from slot 1, use page to lookup ROM data
            // We just add since accessing inside the first slot
            var offsetInPage = readAddress;
            var bankAddress = offsetInPage + MemoryPageSize * _firstBankPage;
            return ReadRom(bankAddress);
        }

        if (readAddress < MemorySlot2 + MemoryPageSize)
        {
            // 0x4000 - 0x8000
            // Reading data from slot 2, use page to lookup ROM data
            var offsetInPage = (ushort)(readAddress - MemorySlot2);
            var bankAddress = offsetInPage + MemoryPageSize * _secondBankPage;
            return ReadRom(bankAddress);
        }

        if (readAddress < MemorySlot3 + MemoryPageSize)
        {
            // 0x8000 - 0xBFFF
            if (_currentRamBank > -1 && _currentRamBank < 2)
            {
                // Return data from ram bank and adjust address to offset page * 2 since slot 3
                return ReadRamBank(_currentRamBank, (ushort)(address - MemoryPageSize * 2));
            }

            if (_currentRamBank > 1)
            {
                _memoryLogger.Error($"Attempted to read RAM bank {_currentRamBank} which does not exist");
                throw new InvalidOperationException($"Attempted to read RAM bank {_currentRamBank} which does not exist");
            }

            // Reading data from slot 2, use page to lookup ROM data
            // Adjust address to offset page * 2 since slot 3
            var offsetInPage = (ushort)(readAddress - MemorySlot3);
            var bankAddress = offsetInPage + MemoryPageSize * _thirdBankPage;
            return ReadRom(bankAddress);
        }

        return ReadRam(readAddress);
    }

    private void WriteToMemoryControlRegisters(ushort address, byte data)
    {
        // bits 0-5 are the page number unless 1 meg than bits 0-6
        var page = _oneMegCartridge ? data & 0x3F : data & 0x1F;

        switch (address)
        {
            case MemoryControlRegister:
            {
                // Writing RAM bank
                _pagingControl = data;
                if (Bitwise.IsSet(data, 3))
                {
                    // Swap in the ram banks
                    _currentRamBank = Bitwise.IsSet(data, 2) ? 1 : 0;
                    _memoryLogger.Debug($"Setting RAM bank to {_currentRamBank}");
                }
                else
                {
                    // Clear ram bank setting since not being used
                    _memoryLogger.Debug($"Disabling RAM bank, was previously ({_currentRamBank})");
                    _currentRamBank = -1;
                }
            }
                break;
            case MemoryControlRegisterSlot1:
                _firstBankPage = page;
                break;
            case MemoryControlRegisterSlot2:
                _secondBankPage = page;
                break;
            case MemoryControlRegisterSlot3:
                _thirdBankPage = page;
                break;
            default:
                throw new ArgumentOutOfRangeException($"Invalid memory page write: Address: 0x{address:X}");
        }
    }
}