using System;
using DotMake.CommandLine;
using NodeSwap.Utils;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Switch to the previously installed version of Node.js.",
    Parent = typeof(RootCommand)
)]
public class PrevCommand(GlobalContext globalContext, NodeJs nodeLocal)
{
    public int Run()
    {
        var prevVersion = nodeLocal.GetPreviousVersion();
        if (prevVersion == null)
        {
            Console.Error.WriteLine("No previous version found");
            return 1;
        }
        
        Console.WriteLine($"Swapping to {prevVersion}");

        if (!ProcessElevation.IsAdministrator())
        {
            // Restart the application with elevated privileges
            return ProcessElevation.ElevateApplication();
        }
        
        var useCommand = new UseCommand(globalContext, nodeLocal)
        {
            Version = prevVersion.ToString(),
        };

        return useCommand.Run();
    }
}