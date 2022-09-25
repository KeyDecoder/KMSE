using FluentAssertions;
using Kmse.Core.Cartridge;
using Kmse.Core.Memory;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Kmse.Core.UnitTests.MemoryTests;

[TestFixture]
public class MasterSystemMemoryFixture
{
    [SetUp]
    public void Setup()
    {
        _memoryLogger = Substitute.For<IMemoryLogger>();
        _cartridge = Substitute.For<IMasterSystemCartridge>();
        _cartridge.Length.Returns(0xFFFF);
        _cartridge.HasDumpHeader.Returns(false);

        _memory = new MasterSystemMemory(Substitute.For<ILogger>(), _memoryLogger);
    }

    private IMasterSystemMemory _memory;
    private IMemoryLogger _memoryLogger;
    private IMasterSystemCartridge _cartridge;

    [Test]
    public void WhenLoadingANewCartridgeThenMemoryIsReset()
    {
        _memory[0x00] = 0x01;
        _memory[0x01] = 0x02;
        _memory[0x1234] = 0x03;


        _cartridge.Length.Returns(0x1000);
        _memory.LoadCartridge(_cartridge);
        _memory[0x00].Should().Be(0x00);
        _memory[0x01].Should().Be(0x00);
        _memory[0x1234].Should().Be(0x00);
    }

    [Test]
    public void WhenGettingMinAndMaxAvailableMemoryWithPagingDisabled()
    {
        _memory.GetMinimumAvailableMemorySize().Should().Be(0xC000);
        _memory.GetMaximumAvailableMemorySize().Should().Be(0x10000);
    }

    [Test]
    public void WhenGettingMinAndMaxAvailableMemoryWithPagingEnabled()
    {
        // Set paging on (bit 3 of memory control register)
        _memory[0xFFFC] = 0x08;
        _memory.GetMinimumAvailableMemorySize().Should().Be(0x8000);
        _memory.GetMaximumAvailableMemorySize().Should().Be(0x10000);
    }

    [Test]
    [TestCase(0x0000)]
    [TestCase(0x3FFF)]
    [TestCase(0x4000)]
    [TestCase(0x7FFF)]
    [TestCase(0x8000)]
    [TestCase(0xBFFF)]
    public void WhenAttemptingToWriteToRomSpaceWithRamBankDisabledThenShouldNotChangeMemory(int address)
    {
        _memory.LoadCartridge(_cartridge);

        _memory[(ushort)address] = 0x12;

        _memoryLogger.Received(1).Error(Arg.Any<string>());
        _memory[(ushort)address].Should().Be(0x00);
    }

    [Test]
    [TestCase(0x0000, false)]
    [TestCase(0x3FFF, false)]
    [TestCase(0x4000, false)]
    [TestCase(0x7FFF, false)]
    [TestCase(0x8000, true)]
    [TestCase(0xBFFF, true)]
    public void WhenAttemptingToWriteToRomSpaceWithRamBankEnabledThenShouldOnlyChangeMemoryInPage3(int address,
        bool canWrite)
    {
        _memory.LoadCartridge(_cartridge);

        _memory[0xFFFC] = 0x08;
        _memory[(ushort)address] = 0x12;

        if (canWrite)
        {
            _memoryLogger.DidNotReceiveWithAnyArgs().Error(null);
            _memory[(ushort)address].Should().Be(0x12);
        }
        else
        {
            _memoryLogger.Received(1).Error(Arg.Any<string>());
            _memory[(ushort)address].Should().Be(0x00);
        }
    }

    [Test]
    [TestCase(0x0000)]
    [TestCase(0x3FFF)]
    [TestCase(0x4000)]
    [TestCase(0x7FFF)]
    [TestCase(0x8000)]
    [TestCase(0xBFFF)]
    public void WhenReadingFromCartridgeMemoryWithNoPagesSetThenFirst3SlotsAreReturned(int address)
    {
        _cartridge[(ushort)address].Returns((byte)0x01);
        _memory.LoadCartridge(_cartridge);

        var value = _memory[(ushort)address];
        value.Should().Be(0x01);
    }

    [Test]
    [TestCase(0x0000, 0, 0x0000)]
    [TestCase(0x03FF, 1, 0x03FF)]
    [TestCase(0x0400, 1, 0x4400)]
    [TestCase(0x0400, 2, 0x8400)]
    [TestCase(0x3FFF, 1, 0x7FFF)]
    [TestCase(0x3FFF, 3, 0xFFFF)]
    [TestCase(0x4000, 1, 0x4000)]
    [TestCase(0x7FFF, 1, 0x7FFF)]
    [TestCase(0x8000, 1, 0x8000)]
    [TestCase(0xBFFF, 1, 0xBFFF)]
    public void WhenReadingFromCartridgeMemoryWithFirstBankPageSet(int memoryAddress, byte firstBankPage,
        int pagedRomAddress)
    {
        _cartridge[(ushort)pagedRomAddress].Returns((byte)0x01);
        _memory.LoadCartridge(_cartridge);
        _memory[0xFFFD] = firstBankPage;
        var value = _memory[(ushort)memoryAddress];
        value.Should().Be(0x01);
    }

    [Test]
    [TestCase(0x0000, 0, 0x0000)]
    [TestCase(0x03FF, 1, 0x03FF)]
    [TestCase(0x0400, 1, 0x0400)]
    [TestCase(0x3FFF, 1, 0x3FFF)]
    [TestCase(0x4000, 1, 0x4000)]
    [TestCase(0x7FFF, 2, 0xBFFF)]
    [TestCase(0x4001, 3, 0xC001)]
    [TestCase(0x8000, 1, 0x8000)]
    [TestCase(0xBFFF, 1, 0xBFFF)]
    public void WhenReadingFromCartridgeMemoryWithSecondBankPageSet(int memoryAddress, byte secondBankPage,
        int pagedRomAddress)
    {
        _cartridge[(ushort)pagedRomAddress].Returns((byte)0x01);
        _memory.LoadCartridge(_cartridge);
        _memory[0xFFFE] = secondBankPage;
        var value = _memory[(ushort)memoryAddress];
        value.Should().Be(0x01);
    }

    [Test]
    [TestCase(0x0000, 0, 0x0000)]
    [TestCase(0x03FF, 1, 0x03FF)]
    [TestCase(0x0400, 1, 0x0400)]
    [TestCase(0x3FFF, 1, 0x3FFF)]
    [TestCase(0x4000, 1, 0x4000)]
    [TestCase(0x7FFF, 2, 0x7FFF)]
    [TestCase(0x8000, 1, 0x4000)]
    [TestCase(0x8001, 1, 0x4001)]
    [TestCase(0x8001, 2, 0x8001)]
    [TestCase(0x8001, 3, 0xC001)]
    [TestCase(0xBFFF, 2, 0xBFFF)]
    [TestCase(0xBFFF, 3, 0xFFFF)]
    public void WhenReadingFromCartridgeMemoryWithThirdBankPageSet(int memoryAddress, byte thirdBankPage,
        int pagedRomAddress)
    {
        _cartridge[(ushort)pagedRomAddress].Returns((byte)0x01);
        _memory.LoadCartridge(_cartridge);
        _memory[0xFFFF] = thirdBankPage;
        var value = _memory[(ushort)memoryAddress];
        value.Should().Be(0x01);
    }

    [Test]
    [TestCase(0xC000)]
    [TestCase(0xFFFF)]
    public void WhenWritingAndReadingNormalRamThenDataIsReturned(int address)
    {
        _memory[(ushort)address] = 0x01;
        var value = _memory[(ushort)address];
        value.Should().Be(0x01);
    }

    [Test]
    public void WhenReadingWritingUsingFirstRamBankThenDataIsStoredInRamBank0()
    {
        _cartridge[0x8001].Returns((byte)0x55);
        _memory.LoadCartridge(_cartridge);

        // enable RAM bank 0
        _memory[0xFFFC] = 0x08;

        _memory[0x8001] = 0x03;
        _memory[0x8001].Should().Be(0x03, "if value is 0x55 then reading from ROM not RAM bank 0");
    }

    [Test]
    public void WhenReadingWritingUsingSecondRamBankThenDataIsStoredInRamBank1()
    {
        _cartridge[0x8001].Returns((byte)0x55);
        _memory.LoadCartridge(_cartridge);

        // enable RAM bank 1
        _memory[0xFFFC] = 0x0C;

        _memory[0x8001] = 0x04;
        _memory[0x8001].Should().Be(0x04, "if value is 0x55 then reading from ROM not RAM bank 1");
    }

    [Test]
    public void WhenSwitchingBetweenRamBanks()
    {
        // This test has data in ROM and both RAM banks and ensures can switch between them without affecting any other data
        // and tests that switching and disabling RAM bank is working properly
        _cartridge[0x8001].Returns((byte)0x55);
        _memory.LoadCartridge(_cartridge);

        // enable RAM bank 0
        _memory[0xFFFC] = 0x08;
        _memory[0x8001] = 0x03;

        // enable RAM bank 1
        _memory[0xFFFC] = 0x0C;
        _memory[0x8001] = 0x04;

        // Disable RAM bank
        _memory[0xFFFC] = 0x00;
        _memory[0x8001].Should().Be(0x55, "if value is 0x04 then disabling RAM bank not working");

        // Enable RAM bank 0
        _memory[0xFFFC] = 0x08;
        _memory[0x8001].Should().Be(0x03,
            "if value is 0x55 then reading from ROM not RAM bank 0, if 0x04 then did not switch to RAM bank 0 since reading from RAM bank 1");

        // Enable RAM bank 1
        _memory[0xFFFC] = 0x0C;
        _memory[0x8001].Should().Be(0x04,
            "if value is 0x55 then reading from ROM not RAM bank 1, if 0x03 then did not switch to RAM bank 1");
    }

    [Test]
    public void WhenWritingNormalRamThenDataIsMirrored()
    {
        _memory[0xC000] = 0x01;
        _memory[0xC000].Should().Be(0x01);
        _memory[0xE000].Should().Be(0x01);
    }

    [Test]
    public void WhenWritingMirroredRamThenRegularDataIsUpdated()
    {
        _memory[0xE000] = 0x01;
        _memory[0xE000].Should().Be(0x01);
        _memory[0xC000].Should().Be(0x01);
    }
}