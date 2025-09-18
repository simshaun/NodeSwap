using System;
using DotMake.CommandLine;
using NodeSwap.Interfaces;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Switch to the previously installed version of Node.js.",
    Parent = typeof(RootCommand)
)]
public class PrevCommand(
    GlobalContext globalContext, 
    INodeJs nodeLocal,
    IProcessElevation processElevation,
    IConsoleWriter console,
    IFileSystem fileSystem)
{
    public int Run()
    {
        var prevVersion = nodeLocal.GetPreviousVersion();
        if (prevVersion == null)
        {
            console.WriteErrorLine("No previous version found");
            return 1;
        }
        
        console.WriteLine($"Swapping to {prevVersion}");

        if (!processElevation.IsAdministrator())
        {
            return processElevation.ElevateApplication();
        }
        
        // Create a UseCommand with all required dependencies and execute it
        var useCommand = new UseCommand(globalContext, nodeLocal, processElevation, console, fileSystem)
        {
            Version = prevVersion.ToString(),
        };

        return useCommand.Run();
    }
}