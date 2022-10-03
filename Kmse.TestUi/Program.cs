using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Kmse.Core.Infrastructure;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Vdp.Rendering;
using Kmse.Core.Memory;
using Kmse.Core.Z80.Logging;
using Kmse.TestUI.Logging;
using Kmse.TestUI.Render;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.TestUi
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var builder = Host.CreateDefaultBuilder();
            var host = Configure(builder).Build();

            ApplicationConfiguration.Initialize();
            var mainForm = host.Services.GetService<frmMain>();
            Application.Run(mainForm);
        }

        public static IHostBuilder Configure(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    //configuration.SetBasePath(AppContext.BaseDirectory);
                    //configuration.AddJsonFile("appsettings.json", false, false);
                })
                .ConfigureContainer<ContainerBuilder>(containerBuilder =>
                {
                    containerBuilder.RegisterLogger();
                    containerBuilder.RegisterModule<EmulatorModule>();
                    containerBuilder.RegisterType<UiMemoryLogger>().As<IMemoryLogger>().AsSelf().SingleInstance();
                    containerBuilder.RegisterType<UiIoLogger>().As<IIoPortLogger>().AsSelf().SingleInstance();
                    containerBuilder.RegisterType<UiDebugConsoleOutput>().As<IDebugConsoleOutput>().AsSelf().SingleInstance();
                    containerBuilder.RegisterType<UiCpuLogger>().As<ICpuLogger>().AsSelf().SingleInstance();
                    containerBuilder.RegisterType<WinFormsDisplayUpdater>().As<IVdpDisplayUpdater>();
                    containerBuilder.RegisterType<frmMain>().SingleInstance();
                })
                .ConfigureServices((_, services) =>
                {
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .UseSerilog();

            return builder;
        }
    }
}