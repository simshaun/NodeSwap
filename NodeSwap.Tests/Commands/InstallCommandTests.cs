using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Tests.TestUtils;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class InstallCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private MockNodeJs _mockNodeJs;
    private MockNodeJsWebApi _mockNodeJsWebApi;
    private MockConsoleWriter _mockConsoleWriter;
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage"),
        };
        Directory.CreateDirectory(_globalContext.StoragePath);

        _mockNodeJs = new MockNodeJs();
        _mockNodeJsWebApi = new MockNodeJsWebApi();
        _mockConsoleWriter = new MockConsoleWriter();
        _mockFileSystem = new MockFileSystem();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionIsNull_ShouldReturnError()
    {
        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = null,
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("Missing version argument");
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionIsEmpty_ShouldReturnError()
    {
        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = "",
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("Missing version argument");
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionAlreadyInstalledAndNotForced_ShouldReturnError()
    {
        var version = new Version(18, 17, 0);

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockNodeJsWebApi.GetLatestNodeVersionReturn = version;

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = version.ToString(),
            Force = false,
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain($"{version} already installed");
    }

    [TestMethod]
    public async Task RunAsync_WhenLatestRequested_ShouldCallGetLatestNodeVersion()
    {
        var latestVersion = new Version(20, 11, 0);
        _mockNodeJsWebApi.GetLatestNodeVersionReturn = latestVersion;
        _mockNodeJsWebApi.GetDownloadUrlReturn = "https://example.com/node.zip";

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = "latest",
        };

        // This will fail at download stage but should call GetLatestNodeVersion
        await command.RunAsync();

        _mockNodeJsWebApi.GetLatestNodeVersionCalled.ShouldBeTrue();
        _mockNodeJsWebApi.GetDownloadUrlCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenFuzzyVersionRequested_ShouldCallGetLatestNodeVersionWithPrefix()
    {
        var resolvedVersion = new Version(18, 17, 0);
        _mockNodeJsWebApi.GetLatestNodeVersionWithPrefixReturn = resolvedVersion;
        _mockNodeJsWebApi.GetDownloadUrlReturn = "https://example.com/node.zip";

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = "18",
        };

        // This will fail at download stage but should call GetLatestNodeVersion with prefix
        await command.RunAsync();

        _mockNodeJsWebApi.GetLatestNodeVersionWithPrefixCalled.ShouldBeTrue();
        _mockNodeJsWebApi.LastPrefixUsed.ShouldBe("18");
    }

    [TestMethod]
    public async Task RunAsync_WhenSpecificVersionRequested_ShouldParseVersion()
    {
        _mockNodeJsWebApi.GetDownloadUrlReturn = "https://example.com/node.zip";

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = "18.17.0",
        };

        // This will fail at download stage but should parse the version correctly
        await command.RunAsync();

        _mockNodeJsWebApi.GetDownloadUrlCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenForceInstallOnExisting_ShouldProceedWithInstall()
    {
        var version = new Version(18, 17, 0);
        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockNodeJsWebApi.GetDownloadUrlReturn = "https://example.com/node.zip";

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = version.ToString(),
            Force = true,
        };

        await command.RunAsync();

        // Should proceed to download (which will fail in test environment)
        // but won't get the "already installed" error
        _mockConsoleWriter.ErrorMessages.ShouldNotContain(msg => msg.Contains("already installed"));
        _mockNodeJsWebApi.GetDownloadUrlCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenWebApiThrowsException_ShouldReturnError()
    {
        _mockNodeJsWebApi.ShouldThrowException = true;

        var command = new InstallCommand(
            _globalContext,
            _mockNodeJsWebApi,
            _mockNodeJs,
            _mockConsoleWriter,
            _mockFileSystem)
        {
            Version = "latest",
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain(msg => msg.Contains("Error determining version"));
    }
}