using System;
using System.IO;
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
    [CliArgument(
        Description = "`latest`, specific e.g. `22.6.0`, or fuzzy e.g. `22.6` or `22`. Run `list` command to see installed versions.",
        Required = false
    )]
    public string Version { get; set; }

    public int Run()
    {
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

    private NodeJsVersion ResolveNodeVersion()
    {
        // If no version specified, try to read from .nodeswap file
        if (Version == null)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var nodeSwapFilePath = Path.Combine(currentDirectory, ".nodeswap");

            if (fileSystem.FileExists(nodeSwapFilePath))
            {
                try
                {
                    var versionText = fileSystem.ReadAllText(nodeSwapFilePath).Trim();
                    if (string.IsNullOrWhiteSpace(versionText))
                    {
                        console.WriteErrorLine("The .nodeswap file is empty");
                        return null;
                    }

                    console.WriteLine($"Using Node.js version from .nodeswap: {versionText}");
                    Version = versionText;
                }
                catch (Exception ex)
                {
                    console.WriteErrorLine($"Error reading .nodeswap: {ex.Message}");
                    return null;
                }
            }
            else
            {
                console.WriteErrorLine(
                    "Missing version argument. Either provide a version or create a .nodeswap file.");
                return null;
            }
        }

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
            var versionPrefix = NormalizeVersionPrefix(Version);
            var nodeVersion = nodeLocal
                .GetInstalledVersions()
                .Find(v => v.Version.ToString().StartsWith(versionPrefix, StringComparison.CurrentCulture));
            if (nodeVersion == null)
            {
                console.WriteErrorLine($"{versionPrefix} not installed");
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

    private static string NormalizeVersionPrefix(string rawVersion)
    {
        rawVersion = rawVersion.Trim();
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            throw new ArgumentException("Invalid version argument");
        }

        try
        {
            _ = VersionParser.Parse(rawVersion);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IndexOutOfRangeException)
        {
            throw new ArgumentException($"Unable to parse version: {rawVersion}", ex);
        }

        return rawVersion.StartsWith("v", StringComparison.CurrentCultureIgnoreCase)
            ? rawVersion[1..]
            : rawVersion;
    }

    private int SwitchToVersion(NodeJsVersion nodeVersion)
    {
        // Check if we're already using this version
        var activeVersion = nodeLocal.GetActiveVersion();
        if (activeVersion != null && activeVersion.Equals(nodeVersion.Version))
        {
            console.WriteLine($"Already using Node.js version {nodeVersion.Version}");
            return 0;
        }

        // Track the previous version only if switching to a new version
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
