using System;
using System.Threading.Tasks;
using DotMake.CommandLine;
using NodeSwap.Utils;

namespace NodeSwap.Commands;

[CliCommand(
    Description =
        "Discover Node.js versions available for download.",
    Parent = typeof(RootCommand)
)]
public class AvailCommand(NodeJsWebApi nodeWeb)
{
    [CliArgument(
        Description =
            "Can be specific like `22.6.0`, or fuzzy like `22.6` or `22`.")
    ]
    public string Prefix { get; set; } = "";

    public async Task<int> RunAsync()
    {
        try
        {
            var versions = await nodeWeb.GetInstallableNodeVersions(Prefix);
            if (versions.Count == 0)
            {
                Console.WriteLine("None found");
                return 1;
            }

            var consoleWidth = Console.WindowWidth;
            var numColumns = (int) Math.Ceiling(consoleWidth / 14.0);
            Console.WriteLine();
            ConsoleColumns.WriteColumns(
                versions,
                numColumns,
                (v) => v.ToString().PadLeft(consoleWidth / numColumns, ' ')
            );
            Console.WriteLine();
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            return 1;
        }

        return 0;
    }
}