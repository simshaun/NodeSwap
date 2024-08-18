using System;
using System.IO;
using System.Threading.Tasks;
using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NodeSwap.Commands;

namespace NodeSwap;

internal static class Program
{
    private const string StorageEnv = "NODESWAP_STORAGE";
    private static readonly IServiceProvider ServiceProvider;

    static Program()
    {
        var storageEnv = Environment.ExpandEnvironmentVariables($"%{StorageEnv}%");
        var globalContext = new GlobalContext
        {
            StoragePath = storageEnv,
            SymlinkPath = Path.Combine(storageEnv, "current"),
            Is64Bit = Environment.Is64BitOperatingSystem,
        };

        globalContext.ActiveVersionTrackerFilePath = Path.Combine(globalContext.StoragePath, "last-used");
        
        var services = new ServiceCollection();
        services.AddSingleton(globalContext);
        services.AddSingleton<NodeJsWebApi>();
        services.AddSingleton<NodeJs>();
        ServiceProvider = services.BuildServiceProvider();
        
        Cli.Ext.SetServiceProvider(ServiceProvider);
    }

    private static async Task<int> Main(string[] args)
    {
        var globalContext = ServiceProvider.GetRequiredService<GlobalContext>();
        if (Environment.GetEnvironmentVariable(StorageEnv) == null)
        {
            Console.Error.WriteLine($"Missing {StorageEnv} ENV var. It should exist and contain a folder path.");
            return 1;
        }

        if (!Directory.Exists(globalContext.StoragePath))
        {
            Console.Error.WriteLine($"The directory specified by the {StorageEnv} ENV var does not exist.");
            Console.Error.WriteLine($"{globalContext.StoragePath}");
            return 1;
        }

        return await Cli.RunAsync<RootCommand>(args);
    }
}