using System;
using System.IO;
using AzureTableStorageCacheNetStandard;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static System.Console;

namespace AzureTableStorageCacheNetStandardSample
{
    class Program
    {
        private static ServiceProvider serviceProvider;
        private const string KEY = "test_key";

        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            //Determines the working environment as IHostingEnvironment is unavailable in a console app
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower().Equals("development");

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            if (isDevelopment)
                builder.AddUserSecrets<ConnectionStrings>();

            Configuration = builder.Build();

            var services = new ServiceCollection();

            serviceProvider = services.Configure<ConnectionStrings>(Configuration.GetSection(nameof(ConnectionStrings)))
                    .AddOptions()
                    .BuildServiceProvider();

            var connectionStrings = serviceProvider.GetService<IOptions<ConnectionStrings>>();
            var connString = connectionStrings.Value.AzureCosmosDbTable;
            services.AddSingleton<IDistributedCache>(new AzureTableStorageCacheHandler(connString, "test_table", "test_pk"));
            services.AddSingleton<CachingService>();

            serviceProvider = services.BuildServiceProvider();
            

            DoWork();
        }

        static void DoWork()
        {
            var cacheService = serviceProvider.GetService<CachingService>();

            cacheService.SetCache(KEY, "Hello World!");

            var result = cacheService.GetCache(KEY);

            WriteLine($"Cache: {result}");

            ReadKey();
        }
    }
}
