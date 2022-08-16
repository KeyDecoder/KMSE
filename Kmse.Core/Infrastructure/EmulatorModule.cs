﻿using System.IO.Abstractions;
using Autofac;
using Kmse.Core.Cartridge;
using Kmse.Core.IO;
using Kmse.Core.Memory;

namespace Kmse.Core.Infrastructure;

public class EmulatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FileSystem>().As<IFileSystem>();
        builder.RegisterType<MasterSystemCartridge>().As<IMasterSystemCartridge>().InstancePerDependency();
        builder.RegisterType<MasterSystemMemory>().As<IMasterSystemMemory>().SingleInstance();
        builder.RegisterModule<IOPortsModule>();
    }
}