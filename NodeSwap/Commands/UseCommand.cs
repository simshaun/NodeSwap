using System;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ShellProgressBar;

namespace NVM.Commands
{
    public class UseCommand : ICommandHandler
    {
        private readonly GlobalContext _globalContext;
        private readonly NodeJs _nodeLocal;

        public UseCommand(GlobalContext globalContext, NodeJs nodeLocal)
        {
            _globalContext = globalContext;
            _nodeLocal = nodeLocal;
        }

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

            var nodeVersion = _nodeLocal.GetInstalledVersions().Find(v => v.Version.Equals(version));
            if (nodeVersion == null)
            {
                await Console.Error.WriteLineAsync($"{version} not installed");
                return 1;
            }

            //
            // Replace the symlink
            //

            if (Directory.Exists(_globalContext.NodeDirPath))
            {
                try
                {
                    Directory.Delete(_globalContext.NodeDirPath);
                }
                catch (IOException)
                {
                    await Console.Error.WriteLineAsync(
                        $"Unable to delete the symlink at {_globalContext.NodeDirPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
                    return 1;
                }
            }

            CreateSymbolicLink(_globalContext.NodeDirPath, nodeVersion.Path, SymbolicLink.Directory);
            if (!Directory.Exists(_globalContext.NodeDirPath))
            {
                await Console.Error.WriteLineAsync(
                    $"Unable to create the symlink at {_globalContext.NodeDirPath}. Be sure you are running this in an elevated terminal (i.e. Run as Administrator).");
                return 1;
            }

            //
            // Track it
            //

            // ReSharper disable once MethodHasAsyncOverload
            File.WriteAllText(_globalContext.ActiveVersionTrackerFilePath, nodeVersion.Version.ToString());
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
            Directory = 1
        }
    }
}
