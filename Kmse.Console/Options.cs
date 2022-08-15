using CommandLine;

namespace Kmse.Console;

public class Options
{
    [Option('f', "file", Required = false, HelpText = "ROM file to load")]
    public string Filename { get; set; }
}