using System;
using System.IO.Abstractions;
using Autofac;
using Kmse.Core.Cartridge;
using Kmse.Core.IO;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;

namespace Kmse.Core.Infrastructure;

public class EmulatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<MasterSystemCartridge>().As<IMasterSystemCartridge>().InstancePerDependency();
        builder.RegisterType<MasterSystemMemory>().As<IMasterSystemMemory>().InstancePerLifetimeScope();
        builder.RegisterType<MasterSystemMk2>().As<IMasterSystemConsole>().InstancePerLifetimeScope();

        builder.RegisterModule<IoPortsModule>();
        builder.RegisterModule<Z80CpuModule>();
    }
}