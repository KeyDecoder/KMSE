using Autofac;
using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;

namespace Kmse.Core.IO;

public class IOPortsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MasterSystemIoManager>().As<IMasterSystemIoManager>().InstancePerDependency();
        builder.RegisterType<ControllerPort>().As<IControllerPort>().InstancePerDependency();
        builder.RegisterType<VdpPort>().As<IVdpPort>().InstancePerDependency();
        builder.RegisterType<SoundPort>().As<ISoundPort>().InstancePerDependency();
        builder.RegisterType<DebugConsolePort>().As<IDebugConsolePort>().InstancePerDependency();
    }
}