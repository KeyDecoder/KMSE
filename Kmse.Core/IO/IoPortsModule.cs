using Autofac;
using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;

namespace Kmse.Core.IO;

public class IoPortsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MasterSystemIoManager>().As<IMasterSystemIoManager>().InstancePerLifetimeScope();
        builder.RegisterType<ControllerPort>().As<IControllerPort>().InstancePerLifetimeScope();
        builder.RegisterType<VdpPort>().As<IVdpPort>().InstancePerLifetimeScope();
        builder.RegisterType<SoundPort>().As<ISoundPort>().InstancePerLifetimeScope();
        builder.RegisterType<DebugConsolePort>().As<IDebugConsolePort>().InstancePerLifetimeScope();
        builder.RegisterType<VdpRegisters>().As<IVdpRegisters>().InstancePerLifetimeScope();
    }
}