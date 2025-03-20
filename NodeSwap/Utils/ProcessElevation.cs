using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace NodeSwap.Utils;

public class ProcessElevation
{
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static int ElevateApplication()
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
}