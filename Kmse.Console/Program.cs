using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using CommandLine;
using Kmse.Console.Display;
using Kmse.Console.Logging;
using Kmse.Core.Infrastructure;
using Kmse.Core.IO.DebugConsole;
using Kmse.Core.IO.Logging;
using Kmse.Core.IO.Vdp.Rendering;
using Kmse.Core.Memory;
using Kmse.Core.Z80;
using Kmse.Core.Z80.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kmse.Console;

public class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configurationBuilder.Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var options = new Options();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    options = o;
                });

            if (!File.Exists(options.Filename))
            {
                Log.Logger.Information("File {Filename} does not exist", options.Filename);
                System.Console.WriteLine("Press any key to exit");
                System.Console.ReadKey();
                return -1;
            }

            var builder = Host.CreateDefaultBuilder(args);
            await Configure(builder, options).Build().RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder Configure(IHostBuilder builder, Options options)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", false, false);
            })
            .ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterLogger();
                containerBuilder.RegisterInstance(options);
                containerBuilder.RegisterModule<EmulatorModule>();
                containerBuilder.RegisterType<SerilogMemoryLogger>().As<IMemoryLogger>().AsSelf().SingleInstance();
                containerBuilder.RegisterType<SerilogIoLogger>().As<IIoPortLogger>().AsSelf().SingleInstance();
                containerBuilder.RegisterType<SerilogDebugConsoleOutput>().As<IDebugConsoleOutput>().AsSelf().SingleInstance();
                containerBuilder.RegisterType<SerilogCpuLogger>().As<ICpuLogger>().AsSelf().SingleInstance();
                containerBuilder.RegisterType<ConsoleDisplayUpdater>().As<IVdpDisplayUpdater>();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<EmulatorService>();
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .UseSerilog();

        return builder;
    }
}