using CertOps.Handler;
using CertOps.Model;
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using Dignus.Extensions.Log;
using Dignus.Log;
using System.Reflection;
using System.Text.Json;

namespace CertOps
{
    internal class Program
    {
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogHelper.Fatal(e.ExceptionObject as Exception);
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            LogBuilder.Configuration(LogConfigXmlReader.Load("DignusLog.config")).Build();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                cts.Cancel();
            };

            var serviceProvider = InitDependencies();
            var handler = serviceProvider.GetService<ScheduleHandler>();
            handler.Start(cts.Token);
        }
        private static IServiceProvider InitDependencies()
        {
            var serviceContainer = new ServiceContainer();
            serviceContainer.RegisterDependencies(Assembly.GetExecutingAssembly());

            var configFileName = "config.json";

            var stage = Environment.GetEnvironmentVariable("Stage");

            if (string.IsNullOrEmpty(stage) == false)
            {
                configFileName = $"config.{stage}.json";
            }

            var configJson = File.ReadAllText(configFileName);

            var config = JsonSerializer.Deserialize<Config>(configJson);
            serviceContainer.RegisterType(config);
            serviceContainer.RegisterType(config.AcmeConfig);
            serviceContainer.RegisterType(config.AzureConfig);

            InitAcmeConfig(serviceContainer, config.AcmeConfig);

            return serviceContainer.Build();
        }
        private static void InitAcmeConfig(ServiceContainer serviceContainer, AcmeConfig acmeConfig)
        {

        }
    }
}
