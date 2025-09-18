using System;
using System.IO;
using DotMake.CommandLine;
using NodeSwap.Interfaces;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Use or create a .nodeswap file to manage Node.js version for the current directory.",
    Parent = typeof(RootCommand)
)]
public class FileCommand(
    GlobalContext globalContext, 
    INodeJs nodeJs,
    IProcessElevation processElevation,
    IConsoleWriter console,
    IFileSystem fileSystem)
{
    private const string NodeSwapFileName = ".nodeswap";

    public int Run()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var nodeSwapFilePath = Path.Combine(currentDirectory, NodeSwapFileName);

        return fileSystem.FileExists(nodeSwapFilePath)
            ? UseVersionFromFile(nodeSwapFilePath)
            : CreateFileWithCurrentVersion(nodeSwapFilePath);
    }

    private int UseVersionFromFile(string filePath)
    {
        try
        {
            var versionText = fileSystem.ReadAllText(filePath).Trim();
            if (string.IsNullOrWhiteSpace(versionText))
            {
                console.WriteErrorLine($"The {NodeSwapFileName} file is empty");
                return 1;
            }

            console.WriteLine($"Using Node.js version from {NodeSwapFileName}: {versionText}");

            var useCommand = new UseCommand(globalContext, nodeJs, processElevation, console, fileSystem) { Version = versionText };
            return useCommand.Run();
        }
        catch (Exception ex)
        {
            console.WriteErrorLine($"Error reading {NodeSwapFileName}: {ex.Message}");
            return 1;
        }
    }

    private int CreateFileWithCurrentVersion(string filePath)
    {
        var activeVersion = nodeJs.GetActiveVersion();
        if (activeVersion == null)
        {
            console.WriteErrorLine("No active Node.js version found. " +
                                    "Please use 'nodeswap use <version>' to set a version first.");
            return 1;
        }

        try
        {
            fileSystem.WriteAllText(filePath, activeVersion.ToString());
            console.WriteLine($"Created {NodeSwapFileName} with current Node.js version: {activeVersion}");
            return 0;
        }
        catch (Exception ex)
        {
            console.WriteErrorLine($"Error creating {NodeSwapFileName}: {ex.Message}");
            return 1;
        }
    }
}