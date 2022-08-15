using System.IO.Abstractions;
using FluentAssertions;
using Kmse.Core.Rom;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Kmse.Core.UnitTests.RomLoaderTests;

[TestFixture]
public class RomLoaderFixture
{
    [SetUp]
    public void Setup()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _loader = new RomLoader(Substitute.For<ILogger>(), _fileSystem);
    }

    private IFileSystem _fileSystem;
    private RomLoader _loader;

    [Test]
    public async Task WhenLoadingRomFromNonExistantFile()
    {
        var filename = "testfile.sms";
        _fileSystem.File.Exists(filename).Returns(false);
        var result = await _loader.LoadRom(filename, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task WhenLoadingFromWithoutDumpHeader()
    {
        var filename = "testfile.sms";
        var testRom = new byte[0x4000];
        for (var i = 0; i < testRom.Length; i++) testRom[i] = (byte)(i % 255);

        _fileSystem.File.ReadAllBytesAsync(filename, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testRom));

        var result = await _loader.LoadRom(filename, CancellationToken.None);
        result.Should().BeTrue();
        _loader.CurrentRomData().ToArray().Should().BeEquivalentTo(testRom);
    }

    [Test]
    public async Task WhenLoadingFromWithDumpHeader()
    {
        var filename = "testfile.sms";
        var testRom = new byte[0x4000 + 512];
        for (var i = 0; i < testRom.Length; i++) testRom[i] = (byte)(i % 255);

        _fileSystem.File.ReadAllBytesAsync(filename, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testRom));

        var result = await _loader.LoadRom(filename, CancellationToken.None);
        result.Should().BeTrue();
        _loader.CurrentRomData().ToArray().Should()
            .BeEquivalentTo(testRom.AsSpan().Slice(512, testRom.Length - 512).ToArray());
    }
}