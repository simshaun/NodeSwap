using System;
using System.Threading.Tasks;
using DotMake.CommandLine;
using NodeSwap.Interfaces;
using NodeSwap.Utils;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Discover Node.js versions available for download.",
    Parent = typeof(RootCommand)
)]
public class AvailCommand(INodeJsWebApi nodeWeb, IConsoleWriter console)
{
    [CliArgument(Description = "Can be specific like `22.6.0`, or fuzzy like `22.6` or `22`.")]
    public string Prefix { get; set; } = "";

    public async Task<int> RunAsync()
    {
        try
        {
            var versions = await nodeWeb.GetInstallableNodeVersions(Prefix);
            if (versions.Count == 0)
            {
                console.WriteLine("None found");
                return 1;
            }

            var consoleWidth = GetConsoleWidth();
            var numColumns = (int) Math.Ceiling(consoleWidth / 14.0);
            console.WriteLine("");
            ConsoleColumns.WriteColumns(
                versions,
                numColumns,
                (v) => v.ToString().PadLeft(consoleWidth / numColumns, ' ')
            );
            console.WriteLine("");
        }
        catch (Exception e)
        {
            console.WriteErrorLine(e.Message);
            return 1;
        }

        return 0;
    }

    private int GetConsoleWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch
        {
            // Fallback for test environments or environments where console width is not available
            return 80;
        }
    }
}