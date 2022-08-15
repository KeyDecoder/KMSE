using System.IO.Abstractions;
using FluentAssertions;
using Kmse.Core.Cartridge;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Kmse.Core.UnitTests.CartridgeTests;

[TestFixture]
public class MasterSystemCartridgeFixture
{
    [SetUp]
    public void Setup()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _loader = new MasterSystemCartridge(Substitute.For<ILogger>(), _fileSystem);
    }

    private IFileSystem _fileSystem;
    private MasterSystemCartridge _loader;

    [Test]
    public async Task WhenLoadingFromNonExistantFile()
    {
        var filename = "testfile.sms";
        _fileSystem.File.Exists(filename).Returns(false);
        var result = await _loader.LoadRomFromFile(filename, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task WhenLoadingFromFileWithoutDumpHeader()
    {
        var filename = "testfile.sms";
        var testRom = new byte[0x4000];
        for (var i = 0; i < testRom.Length; i++) testRom[i] = (byte)(i % 255);

        _fileSystem.File.ReadAllBytesAsync(filename, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testRom));

        var result = await _loader.LoadRomFromFile(filename, CancellationToken.None);
        result.Should().BeTrue();

        _loader.Length.Should().Be(testRom.Length);
        var loadedData = new byte[_loader.Length];
        for (ushort i = 0; i < _loader.Length; i++)
        {
            loadedData[i] = _loader[i];
        }
        loadedData.Should().BeEquivalentTo(testRom);
    }

    [Test]
    public async Task WhenLoadingFromFileWithDumpHeader()
    {
        var filename = "testfile.sms";
        var testRom = new byte[0x4000 + 512];
        for (var i = 0; i < testRom.Length; i++) testRom[i] = (byte)(i % 255);

        _fileSystem.File.ReadAllBytesAsync(filename, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testRom));

        var result = await _loader.LoadRomFromFile(filename, CancellationToken.None);
        result.Should().BeTrue();
        _loader.Length.Should().Be(testRom.Length-512);
        var loadedData = new byte[_loader.Length];
        for (ushort i = 0; i < _loader.Length; i++)
        {
            loadedData[i] = _loader[i];
        }
        loadedData.Should().BeEquivalentTo(testRom.AsSpan().Slice(512, testRom.Length - 512).ToArray());
    }
}