using System;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using NodeSwap;
using NodeSwap.Utils;
using ShellProgressBar;

namespace NodeSwap.Commands
{
    public class InstallCommand : ICommandHandler
    {
        private readonly GlobalContext _globalContext;
        private readonly NodeJsWebApi _nodeWeb;
        private readonly NodeJs _nodeLocal;

        public InstallCommand(GlobalContext globalContext, NodeJsWebApi nodeWeb, NodeJs nodeLocal)
        {
            _globalContext = globalContext;
            _nodeWeb = nodeWeb;
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
            if (rawVersion.ToString()?.ToLower() == "latest")
            {
                try
                {
                    version = _nodeWeb.GetLatestNodeVersion();
                }
                catch (Exception)
                {
                    await Console.Error.WriteLineAsync("Unable to determine latest Node.js version.");
                    return 1;
                }
            }
            else if (rawVersion.ToString()?.Split(".").Length < 3)
            {
                try
                {
                    version = _nodeWeb.GetLatestNodeVersion(rawVersion.ToString());
                }
                catch (Exception)
                {
                    await Console.Error.WriteLineAsync($"Unable to get latest Node.js version " +
                                                       $"with prefix {rawVersion}.");
                    return 1;
                }
            }
            else
            {
                try
                {
                    version = VersionParser.Parse(rawVersion.ToString());
                }
                catch (ArgumentException)
                {
                    await Console.Error.WriteLineAsync($"Invalid version argument: {rawVersion}");
                    return 1;
                }
            }

            //
            // Is the requested version already installed?
            //

            if (_nodeLocal.GetInstalledVersions().FindIndex(v => v.Version.Equals(version)) != -1)
            {
                await Console.Error.WriteLineAsync($"{version} already installed");
                return 1;
            }

            //
            // Download it
            //

            var downloadUrl = _nodeWeb.GetDownloadUrl(version);
            var zipPath = Path.Join(_globalContext.StoragePath, Path.GetFileName(downloadUrl));
            var progressBar = new ProgressBar(100, "Download progress", new ProgressBarOptions
            {
                ProgressCharacter = '\u2593',
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.Green,
            });

            var webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) => { progressBar.Tick(e.ProgressPercentage); };
            webClient.DownloadFileCompleted += (s, e) => { progressBar.Dispose(); };

            try
            {
                await webClient.DownloadFileTaskAsync(downloadUrl, zipPath).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync("Unable to download the Node.js zip file.");
                if (e.InnerException == null) return 1;
                await Console.Error.WriteLineAsync(e.InnerException.Message);
                await Console.Error.WriteLineAsync("You may need to run this command from an " +
                                                   "elevated prompt. (Run as Administrator)");
                return 1;
            }

            Console.WriteLine("Extracting...");
            ConsoleSpinner.Instance.Update();
            var timer = new Timer(250);
            timer.Elapsed += (s, e) => ConsoleSpinner.Instance.Update();
            timer.Enabled = true;
            ZipFile.ExtractToDirectory(zipPath, _globalContext.StoragePath);
            timer.Enabled = false;
            ConsoleSpinner.Reset();
            File.Delete(zipPath);

            Console.WriteLine($"Done. To use, run `nodeswap use {version}`");
            return 0;
        }
    }
}
