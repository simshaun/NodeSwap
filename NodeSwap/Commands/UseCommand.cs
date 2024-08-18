using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NodeSwap.Commands;

public class UseCommand(GlobalContext globalContext, NodeJs nodeLocal) : ICommandHandler
{
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var rawVersion = context.ParseResult.ValueForArgument("version");
        if (rawVersion == null)
        {
            await Console.Error.WriteLineAsync("Missing version argument");
            return 1;
        }

        NodeJsVersion nodeVersion;

        if (rawVersion.ToString() == "latest")
        {
            nodeVersion = nodeLocal.GetLatestInstalledVersion();
            if (nodeVersion == null)
            {
                await Console.Error.WriteLineAsync("There are no versions installed");
                return 1;
            }
        }
        else
        {
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

            nodeVersion = nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
            if (nodeVersion == null)
            {
                await Console.Error.WriteLineAsync($"{version} not installed");
                return 1;
            }
        }

        //
        // Replace the symlink
        //

        if (Directory.Exists(globalContext.SymlinkPath))
        {
            try
            {
                Directory.Delete(globalContext.SymlinkPath);
            }
            catch (IOException)
            {
                await Console.Error.WriteLineAsync(
                    $"Unable to delete the symlink at {globalContext.SymlinkPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
                return 1;
            }
        }

        CreateSymbolicLink(globalContext.SymlinkPath, nodeVersion.Path, SymbolicLink.Directory);
        if (!Directory.Exists(globalContext.SymlinkPath))
        {
            await Console.Error.WriteLineAsync(
                $"Unable to create the symlink at {globalContext.SymlinkPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
            return 1;
        }

        //
        // Track it
        //

        // ReSharper disable once MethodHasAsyncOverload
        File.WriteAllText(globalContext.ActiveVersionTrackerFilePath, nodeVersion.Version.ToString());
        Console.WriteLine("Done");
        return 0;
    }


    [DllImport("kernel32.dll")]
    private static extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        SymbolicLink dwFlags);

    private enum SymbolicLink
    {
        File = 0,
        Directory = 1,
    }
}