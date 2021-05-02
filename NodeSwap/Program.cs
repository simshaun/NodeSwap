using System;
using System.CommandLine;
using System.IO;
using NodeSwap.Commands;
using Container = SimpleInjector.Container;

namespace NodeSwap
{
    internal static class Program
    {
        private static readonly Container Container;

        static Program()
        {
            var globalContext = new GlobalContext
            {
                ConfigDirPath = Path.Combine(
                    Environment.ExpandEnvironmentVariables(@"%LocalAppData%"),
                    "NodeSwap"
                ),
                NodeDirPath = Path.Combine(
                    Environment.ExpandEnvironmentVariables(@"%ProgramFiles%"),
                    "nodejs"
                ),
                Is64Bit = Environment.Is64BitOperatingSystem
            };

            globalContext.ActiveVersionTrackerFilePath = Path.Combine(globalContext.ConfigDirPath, "last-used");

            Container = new Container();
            Container.RegisterInstance(globalContext);
            Container.RegisterSingleton<NodeJsWebApi>();
            Container.RegisterSingleton<NodeJs>();
            Container.RegisterSingleton<ListCommand>();
            Container.RegisterSingleton<InstallCommand>();
            Container.RegisterSingleton<UninstallCommand>();
            Container.RegisterSingleton<UseCommand>();
            Container.Verify();
        }

        private static int Main(string[] args)
        {
            var globalContext = Container.GetInstance<GlobalContext>();
            Directory.CreateDirectory(globalContext.ConfigDirPath);

            var rootCommand = new RootCommand();

            var listCommand = new Command("list")
            {
                Handler = Container.GetInstance<ListCommand>(),
                Description = "List the Node.js installations."
            };
            rootCommand.Add(listCommand);

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
            useCommand.Description = "Switch to a specific version. Must be specific like `14.16.1`.";
            useCommand.Handler = Container.GetInstance<UseCommand>();
            rootCommand.Add(useCommand);

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
