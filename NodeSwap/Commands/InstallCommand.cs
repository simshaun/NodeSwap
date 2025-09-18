using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using DotMake.CommandLine;
using NodeSwap.Interfaces;
using ShellProgressBar;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Install a version of Node.js",
    Parent = typeof(RootCommand)
)]
public class InstallCommand(
    GlobalContext globalContext,
    INodeJsWebApi nodeWeb,
    INodeJs nodeLocal,
    IConsoleWriter console,
    IFileSystem fileSystem,
    IConsoleSpinner consoleSpinner)
{
    [CliArgument(Description = "`latest`, specific e.g. `22.6.0`, or fuzzy e.g. `22.6` or `22`.")]
    public string Version { get; set; }

    [CliOption(Description = "Re-install if installed already")]
    public bool Force { get; set; }

    public async Task<int> RunAsync()
    {
        // Validate input
        if (string.IsNullOrEmpty(Version))
        {
            console.WriteErrorLine("Missing version argument");
            return 1;
        }

        // Determine the version to install
        var version = await GetVersion(Version);
        if (version == null) return 1;

        // Check if already installed
        if (!Force && IsVersionInstalled(version))
        {
            console.WriteErrorLine($"{version} already installed");
            return 1;
        }

        // Download and install
        var downloadUrl = nodeWeb.GetDownloadUrl(version);
        var zipPath = Path.Join(globalContext.StoragePath, Path.GetFileName(downloadUrl));

        var downloadResult = await DownloadNodeJs(downloadUrl, zipPath);
        if (!downloadResult) return 1;

        ExtractNodeJs(zipPath);
        console.WriteLine($"Done. To use, run `nodeswap use {version}`");
        return 0;
    }

    private async Task<Version> GetVersion(string rawVersion)
    {
        try
        {
            if (rawVersion.Equals("latest", StringComparison.CurrentCultureIgnoreCase))
                return await nodeWeb.GetLatestNodeVersion();

            if (rawVersion.Split(".").Length < 3)
                return await nodeWeb.GetLatestNodeVersion(rawVersion);

            return VersionParser.Parse(rawVersion);
        }
        catch (Exception ex)
        {
            console.WriteErrorLine($"Error determining version: {ex.Message}");
            return null;
        }
    }

    private bool IsVersionInstalled(Version version)
    {
        return nodeLocal.GetInstalledVersions().FindIndex(v => v.Version.Equals(version)) != -1;
    }

    private async Task<bool> DownloadNodeJs(string downloadUrl, string zipPath)
    {
        var progressBar = new ProgressBar(100, "Download progress", new ProgressBarOptions
        {
            ProgressCharacter = '\u2593',
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.Green,
        });

        try
        {
            var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                if (!canReportProgress) continue;
                totalRead += bytesRead;
                var progressPercentage = (int) (totalRead * 100 / totalBytes);
                progressBar.Tick(progressPercentage);
            }

            progressBar.Dispose();
            return true;
        }
        catch (Exception e)
        {
            console.WriteErrorLine("Unable to download the Node.js zip file.");
            if (e.InnerException != null)
            {
                console.WriteErrorLine(e.InnerException.Message);
                console.WriteErrorLine(
                    "You may need to run this command from an elevated prompt. (Run as Administrator)");
            }

            return false;
        }
    }

    private void ExtractNodeJs(string zipPath)
    {
        console.WriteLine("Extracting...");
        consoleSpinner.Update();

        var timer = new System.Timers.Timer(250);
        timer.Elapsed += (_, _) => consoleSpinner.Update();
        timer.Start();

        ZipFile.ExtractToDirectory(zipPath, globalContext.StoragePath, overwriteFiles: true);

        timer.Stop();
        consoleSpinner.Reset();
        fileSystem.DeleteFile(zipPath);
    }
}