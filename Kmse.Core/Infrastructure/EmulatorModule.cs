using System.IO.Abstractions;
using Autofac;
using Kmse.Core.Cartridge;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80;

namespace Kmse.Core.Infrastructure;

public class EmulatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<MasterSystemCartridge>().As<IMasterSystemCartridge>().InstancePerDependency();
        builder.RegisterType<MasterSystemMemory>().As<IMasterSystemMemory>();
        builder.RegisterType<Z80Cpu>().As<IZ80Cpu>();
        builder.RegisterModule<IoPortsModule>();
        builder.RegisterType<MasterSystemMk2>().As<IMasterSystemConsole>();
    }
}