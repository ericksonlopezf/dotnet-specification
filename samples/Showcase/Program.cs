// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Showcase.Levels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(services =>
            {
                // Register Levels
                services.AddTransient<ILevel, Level0_Conceptual>();
                services.AddTransient<ILevel, Level1_QuickStart>();
                services.AddTransient<ILevel, Level2_Configuration>();
                services.AddTransient<ILevel, Level3_RealUseCases>();
                services.AddTransient<ILevel, Level4_AdvancedIntegration>();
                services.AddTransient<ILevel, Level5_Processing>();
                services.AddTransient<ILevel, Level6_ErrorHandling>();
                services.AddTransient<ILevel, Level7_Scalability>();
                services.AddTransient<ILevel, Level8_Customization>();
                services.AddTransient<ILevel, Level9_Extensions>();
                services.AddTransient<ILevel, Level10_EnterpriseArchitecture>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting EricksonLopez.Specification Showcase...");

        var levels = host.Services.GetServices<ILevel>();

        foreach (var level in levels)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 50));
            Console.WriteLine($" Executing: {level.Name}");
            Console.WriteLine($" Description: {level.Description}");
            Console.WriteLine(new string('=', 50));

            await level.ExecuteAsync();

            Console.WriteLine("\nPress Enter to continue to the next level...");
            // If running interactively
            if (!Console.IsInputRedirected)
            {
                Console.ReadLine();
            }
        }

        logger.LogInformation("Showcase completed successfully.");
    }
}



