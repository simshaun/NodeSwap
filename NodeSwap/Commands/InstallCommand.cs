using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using DotMake.CommandLine;
using NodeSwap.Utils;
using ShellProgressBar;

namespace NodeSwap.Commands;

[CliCommand(
    Description = "Install a version of Node.js",
    Parent = typeof(RootCommand)
)]
public class InstallCommand(GlobalContext globalContext, NodeJsWebApi nodeWeb, NodeJs nodeLocal)
{
    [CliArgument(Description = "`latest`, specific e.g. `22.6.0`, or fuzzy e.g. `22.6` or `22`.")]
    public string Version { get; set; }

    [CliOption(Description = "Re-install if installed already")]
    public bool Force { get; set; }

    public async Task<int> RunAsync()
    {
        // Retrieve and validate version argument
        if (string.IsNullOrEmpty(Version))
        {
            await Console.Error.WriteLineAsync("Missing version argument");
            return 1;
        }

        // Determine the version to install
        var version = await GetVersion(Version);
        if (version == null) return 1;

        // Check if the requested version is already installed
        if (!Force && IsVersionInstalled(version))
        {
            await Console.Error.WriteLineAsync($"{version} already installed");
            return 1;
        }

        // Download and install Node.js
        var downloadUrl = nodeWeb.GetDownloadUrl(version);
        var zipPath = Path.Join(globalContext.StoragePath, Path.GetFileName(downloadUrl));
        var downloadResult = await DownloadNodeJs(downloadUrl, zipPath);

        if (!downloadResult) return 1;

        // Extract the downloaded file
        ExtractNodeJs(zipPath);

        // Completion message
        Console.WriteLine($"Done. To use, run `nodeswap use {version}`");
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
            await Console.Error.WriteLineAsync($"Error determining version: {ex.Message}");
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
            await Console.Error.WriteLineAsync("Unable to download the Node.js zip file.");
            if (e.InnerException == null) return false;
            await Console.Error.WriteLineAsync(e.InnerException.Message);
            await Console.Error.WriteLineAsync(
                "You may need to run this command from an elevated prompt. (Run as Administrator)");
            return false;
        }
    }

    private void ExtractNodeJs(string zipPath)
    {
        Console.WriteLine("Extracting...");
        ConsoleSpinner.Instance.Update();

        var timer = new Timer(250);
        timer.Elapsed += (_, _) => ConsoleSpinner.Instance.Update();
        timer.Start();

        ZipFile.ExtractToDirectory(zipPath, globalContext.StoragePath, overwriteFiles: true);

        timer.Stop();
        ConsoleSpinner.Reset();
        File.Delete(zipPath);
    }
}