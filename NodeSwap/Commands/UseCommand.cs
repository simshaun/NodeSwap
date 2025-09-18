using System;
using DotMake.CommandLine;
using NodeSwap.Interfaces;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Switch to an installed version of Node.js.",
    Parent = typeof(RootCommand)
)]
public class UseCommand(
    GlobalContext globalContext, 
    INodeJs nodeLocal, 
    IProcessElevation processElevation,
    IConsoleWriter console,
    IFileSystem fileSystem)
{
    [CliArgument(Description = "`latest` or specific e.g. `22.6.0`. Run `list` command to see installed versions.")]
    public string Version { get; set; }

    public int Run()
    {
        // Validate input
        var validationResult = ValidateInput();
        if (validationResult != null) return validationResult.Value;

        // Find the version to use
        var nodeVersion = ResolveNodeVersion();
        if (nodeVersion == null) return 1;

        // Check elevation
        if (!processElevation.IsAdministrator())
        {
            return processElevation.ElevateApplication();
        }

        // Perform the switch
        return SwitchToVersion(nodeVersion);
    }

    private int? ValidateInput()
    {
        if (Version == null)
        {
            console.WriteErrorLine("Missing version argument");
            return 1;
        }
        return null;
    }

    private NodeJsVersion ResolveNodeVersion()
    {
        if (Version == "latest")
        {
            var latestVersion = nodeLocal.GetLatestInstalledVersion();
            if (latestVersion == null)
            {
                console.WriteErrorLine("There are no versions installed");
                return null;
            }
            return latestVersion;
        }

        try
        {
            var version = VersionParser.StrictParse(Version);
            var nodeVersion = nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
            if (nodeVersion == null)
            {
                console.WriteErrorLine($"{version} not installed");
                return null;
            }
            return nodeVersion;
        }
        catch (ArgumentException)
        {
            console.WriteErrorLine($"Invalid version argument: {Version}");
            return null;
        }
    }

    private int SwitchToVersion(NodeJsVersion nodeVersion)
    {
        // Track the previous version
        var activeVersion = nodeLocal.GetActiveVersion();
        if (activeVersion != null)
        {
            fileSystem.WriteAllText(globalContext.PreviousVersionTrackerFilePath, activeVersion.ToString());
        }

        // Replace the symlink
        if (fileSystem.DirectoryExists(globalContext.SymlinkPath))
        {
            try
            {
                fileSystem.DeleteDirectory(globalContext.SymlinkPath, true);
            }
            catch (Exception)
            {
                console.WriteErrorLine(
                    $"Unable to delete the symlink at {globalContext.SymlinkPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
                return 1;
            }
        }

        var symlinkCreated = fileSystem.CreateSymbolicLink(globalContext.SymlinkPath, nodeVersion.Path, true);
        if (!symlinkCreated)
        {
            console.WriteErrorLine(
                $"Unable to create the symlink at {globalContext.SymlinkPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
            return 1;
        }

        // Track the new active version
        fileSystem.WriteAllText(globalContext.ActiveVersionTrackerFilePath, nodeVersion.Version.ToString());
        console.WriteLine("Done");
        return 0;
    }
}