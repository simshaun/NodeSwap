using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using DotMake.CommandLine;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Switch to an installed version of Node.js.",
    Parent = typeof(RootCommand)
)]
public class UseCommand(GlobalContext globalContext, NodeJs nodeLocal)
{
    [CliArgument(Description = "`latest` or specific e.g. `22.6.0`. Run `list` command to see installed versions.")]
    public string Version { get; set; }

    public int Run()
    {
        if (Version == null)
        {
            Console.Error.WriteLine("Missing version argument");
            return 1;
        }

        NodeJsVersion nodeVersion;

        if (Version == "latest")
        {
            nodeVersion = nodeLocal.GetLatestInstalledVersion();
            if (nodeVersion == null)
            {
                Console.Error.WriteLine("There are no versions installed");
                return 1;
            }
        }
        else
        {
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

            nodeVersion = nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
            if (nodeVersion == null)
            {
                Console.Error.WriteLine($"{version} not installed");
                return 1;
            }
        }

        if (!IsAdministrator())
        {
            // Restart the application with elevated privileges
            return ElevateApplication();
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
                Console.Error.WriteLine(
                    $"Unable to delete the symlink at {globalContext.SymlinkPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
                return 1;
            }
        }

        CreateSymbolicLink(globalContext.SymlinkPath, nodeVersion.Path, SymbolicLink.Directory);
        if (!Directory.Exists(globalContext.SymlinkPath))
        {
            Console.Error.WriteLine(
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

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int ElevateApplication()
    {
        var currentProcessModule = Process.GetCurrentProcess().MainModule;
        if (currentProcessModule == null) throw new Exception("Unable to get the current process module");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = currentProcessModule.FileName,
                UseShellExecute = true,
                Verb = "runas", // Forces the application to run with elevated permissions
                Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1)),
            },
        };

        try
        {
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Could not restart as Administrator: " + ex.Message);
            return 1;
        }
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