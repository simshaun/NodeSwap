using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace NodeSwap.Commands;

public class UninstallCommand(NodeJs nodeLocal) : ICommandHandler
{
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var rawVersion = context.ParseResult.ValueForArgument("version");
        if (rawVersion == null)
        {
            await Console.Error.WriteLineAsync($"Missing version argument");
            return 1;
        }

        Version version;
        try
        {
            version = VersionParser.StrictParse(rawVersion.ToString()!);
        }
        catch (ArgumentException)
        {
            await Console.Error.WriteLineAsync($"Invalid version argument: {rawVersion}");
            return 1;
        }

        //
        // Is the requested version installed?
        //

        var nodeVersion = nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
        if (nodeVersion == null)
        {
            await Console.Error.WriteLineAsync($"{version} not installed");
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
            await Console.Error.WriteLineAsync($"Unable to delete {nodeVersion.Path}");
            return 1;
        }

        Console.WriteLine("Done");
        return 0;
    }
}