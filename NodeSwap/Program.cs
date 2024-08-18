using System;
using System.CommandLine;
using System.IO;
using NodeSwap.Commands;
using Container = SimpleInjector.Container;

namespace NodeSwap;

internal static class Program
{
    private const string StorageEnv = "NODESWAP_STORAGE";
    private static readonly Container Container;

    static Program()
    {
        var storageEnv = Environment.ExpandEnvironmentVariables($"%{StorageEnv}%");
        var globalContext = new GlobalContext
        {
            StoragePath = storageEnv,
            SymlinkPath = Path.Combine(storageEnv, "current"),
            Is64Bit = Environment.Is64BitOperatingSystem
        };

        globalContext.ActiveVersionTrackerFilePath = Path.Combine(globalContext.StoragePath, "last-used");

        Container = new Container();
        Container.RegisterInstance(globalContext);
        Container.RegisterSingleton<NodeJsWebApi>();
        Container.RegisterSingleton<NodeJs>();
        Container.RegisterSingleton<AvailCommand>();
        Container.RegisterSingleton<ListCommand>();
        Container.RegisterSingleton<InstallCommand>();
        Container.RegisterSingleton<UninstallCommand>();
        Container.RegisterSingleton<UseCommand>();
        Container.Verify();
    }

    private static int Main(string[] args)
    {
        var globalContext = Container.GetInstance<GlobalContext>();
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

        var rootCommand = new RootCommand();

        var listCommand = new Command("list")
        {
            Handler = Container.GetInstance<ListCommand>(),
            Description = "List the Node.js installations."
        };
        rootCommand.Add(listCommand);

        var availPrefixArg = new Argument("prefix");
        availPrefixArg.SetDefaultValue("");
        var availCommand = new Command("avail") {availPrefixArg};
        availCommand.Description =
            "List versions available for download. Prefix can be specific like `14.16.1`, or fuzzy like `14.16` or `14`.";
        availCommand.Handler = Container.GetInstance<AvailCommand>();
        rootCommand.Add(availCommand);

        var installCommand = new Command("install") {new Argument("version")};
        installCommand.Description =
            "The version can be `latest`, a specific version like `14.16.1`, or a fuzzy version like `14.16` or `14`.";
        installCommand.Handler = Container.GetInstance<InstallCommand>();
        rootCommand.Add(installCommand);

        var uninstallCommand = new Command("uninstall") {new Argument("version")};
        uninstallCommand.Description = "The version must be specific like `14.16.1`.";
        uninstallCommand.Handler = Container.GetInstance<UninstallCommand>();
        rootCommand.Add(uninstallCommand);

        var useCommand = new Command("use") {new Argument("version")};
        useCommand.Description = "Switch to a specific version. May be `latest` or specific like `14.16.1`.";
        useCommand.Handler = Container.GetInstance<UseCommand>();
        rootCommand.Add(useCommand);

        return rootCommand.InvokeAsync(args).Result;
    }
}