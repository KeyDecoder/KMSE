using Autofac;
using Kmse.Core.IO.Controllers;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Sound;
using Kmse.Core.IO.Vdp;
using Kmse.Core.IO.Vdp.Control;
using Kmse.Core.IO.Vdp.Counters;
using Kmse.Core.IO.Vdp.Flags;
using Kmse.Core.IO.Vdp.Model;
using Kmse.Core.IO.Vdp.Ram;
using Kmse.Core.IO.Vdp.Registers;
using Kmse.Core.IO.Vdp.Rendering;

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
        builder.RegisterType<VdpRam>().As<IVdpRam>().InstancePerLifetimeScope();
        builder.RegisterType<VdpVerticalCounter>().As<IVdpVerticalCounter>().InstancePerLifetimeScope();
        builder.RegisterType<VdpHorizontalCounter>().As<IVdpHorizontalCounter>().InstancePerLifetimeScope();

        builder.RegisterType<VdpFlags>().As<IVdpFlags>().InstancePerLifetimeScope();
        builder.RegisterType<VdpControlPortManager>().As<IVdpControlPortManager>().InstancePerLifetimeScope();
        builder.RegisterType<VdpMode4DisplayModeRenderer>().As<IVdpDisplayModeRenderer>().InstancePerLifetimeScope();//.Keyed<VdpVideoMode>(VdpVideoMode.Mode4);
    }
}