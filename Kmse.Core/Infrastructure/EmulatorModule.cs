﻿using Autofac;
using Kmse.Core.Rom;

namespace Kmse.Core.Infrastructure;

public class EmulatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RomLoader>().As<IRomLoader>().InstancePerDependency();
    }
}