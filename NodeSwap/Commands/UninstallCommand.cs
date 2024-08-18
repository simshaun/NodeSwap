using System;
using System.IO;
using DotMake.CommandLine;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Uninstall a specific version of Node.js",
    Parent = typeof(RootCommand)
)]
public class UninstallCommand(NodeJs nodeLocal)
{
    [CliArgument(Description = "e.g. `22.6.0`. Run `list` command to see installed versions.")]
    public string Version { get; set; }

    public int Run()
    {
        if (Version == null)
        {
            Console.Error.WriteLine("Missing version argument");
            return 1;
        }

        Version version;
        try
        {
            version = VersionParser.StrictParse(Version);
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine($"Invalid version argument: {Version}");
            return 1;
        }

        //
        // Is the requested version installed?
        //

        var nodeVersion = nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
        if (nodeVersion == null)
        {
            Console.Error.WriteLine($"{version} not installed");
            return 1;
        }

        //
        // Uninstall it
        //

        try
        {
            Directory.Delete(nodeVersion.Path, true);
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"Unable to delete {nodeVersion.Path}");
            return 1;
        }

        Console.WriteLine("Done");
        return 0;
    }
}