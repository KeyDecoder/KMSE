using Autofac;
using Kmse.Core.Z80.Instructions;
using Kmse.Core.Z80.Interrupts;
using Kmse.Core.Z80.IO;
using Kmse.Core.Z80.Logging;
using Kmse.Core.Z80.Memory;
using Kmse.Core.Z80.Registers;
using Kmse.Core.Z80.Registers.General;
using Kmse.Core.Z80.Registers.SpecialPurpose;
using Kmse.Core.Z80.Running;

namespace Kmse.Core.Z80;

public class Z80CpuModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<Z80Cpu>().As<IZ80Cpu>().InstancePerLifetimeScope();
        builder.RegisterType<Z80InstructionLogger>().As<IZ80InstructionLogger>().InstancePerLifetimeScope();

        builder.RegisterType<Z80CpuRegisters>().AsSelf().PropertiesAutowired();
        builder.RegisterType<Z80CpuManagement>().AsSelf().PropertiesAutowired();

        builder.RegisterType<Z80FlagsManager>().As<IZ80FlagsManager>().InstancePerLifetimeScope();
        builder.RegisterType<Z80Accumulator>().As<IZ80Accumulator>().InstancePerLifetimeScope();
        builder.RegisterType<Z808BitGeneralPurposeRegister>().As<IZ808BitGeneralPurposeRegister>()
            .InstancePerDependency();

        builder.RegisterType<Z80AfRegister>().As<IZ80AfRegister>().InstancePerLifetimeScope();
        builder.RegisterType<Z80BcRegister>().As<IZ80BcRegister>().InstancePerLifetimeScope();
        builder.RegisterType<Z80DeRegister>().As<IZ80DeRegister>().InstancePerLifetimeScope();
        builder.RegisterType<Z80HlRegister>().As<IZ80HlRegister>().InstancePerLifetimeScope();
        builder.RegisterType<Z80IndexRegisterX>().As<IZ80IndexRegisterX>().InstancePerLifetimeScope();
        builder.RegisterType<Z80IndexRegisterY>().As<IZ80IndexRegisterY>().InstancePerLifetimeScope();
        builder.RegisterType<Z80InterruptPageAddressRegister>().As<IZ80InterruptPageAddressRegister>()
            .InstancePerLifetimeScope();
        builder.RegisterType<Z80MemoryRefreshRegister>().As<IZ80MemoryRefreshRegister>().InstancePerLifetimeScope();
        builder.RegisterType<Z80AfRegister>().As<IZ80AfRegister>().InstancePerLifetimeScope();

        builder.RegisterType<Z80ProgramCounter>().As<IZ80ProgramCounter>().InstancePerLifetimeScope();
        builder.RegisterType<Z80StackManager>().As<IZ80StackManager>().InstancePerLifetimeScope();

        builder.RegisterType<Z80CpuInputOutputManager>().As<IZ80CpuInputOutputManager>().InstancePerLifetimeScope();
        builder.RegisterType<Z80CpuMemoryManagement>().As<IZ80CpuMemoryManagement>().InstancePerLifetimeScope();
        builder.RegisterType<Z80InterruptManagement>().As<IZ80InterruptManagement>().InstancePerLifetimeScope();
        builder.RegisterType<Z80CpuCycleCounter>().As<IZ80CpuCycleCounter>().InstancePerLifetimeScope();
        builder.RegisterType<Z80CpuRunningStateManager>().As<IZ80CpuRunningStateManager>().InstancePerLifetimeScope();
        builder.RegisterType<Z80CpuInstructions>().As<IZ80CpuInstructions>().InstancePerLifetimeScope();
    }
}