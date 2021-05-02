using System;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using NodeSwap;
using ShellProgressBar;

namespace NodeSwap.Commands
{
    public class UninstallCommand : ICommandHandler
    {
        private readonly GlobalContext _globalContext;
        private readonly NodeJs _nodeLocal;

        public UninstallCommand(GlobalContext globalContext, NodeJs nodeLocal)
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
            // Uninstall it
            //

            if (nodeVersion.IsActive && Directory.Exists(_globalContext.NodeDirPath))
            {
                // Remove the symlink
                try
                {
                    Directory.Delete(_globalContext.NodeDirPath);
                }
                catch (IOException)
                {
                    await Console.Error.WriteLineAsync($"Unable to delete the symlink at {_globalContext.NodeDirPath}");
                    return 1;
                }
            }


            // Then delete the version
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
}
