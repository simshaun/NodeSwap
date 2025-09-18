using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Interfaces;
using NSubstitute;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class InstallCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private INodeJs _mockNodeJs;
    private INodeJsWebApi _mockNodeJsWebApi;
    private IConsoleWriter _mockConsoleWriter;
    private IFileSystem _mockFileSystem;
    private IConsoleSpinner _mockConsoleSpinner;

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

        _mockNodeJs = Substitute.For<INodeJs>();
        _mockNodeJsWebApi = Substitute.For<INodeJsWebApi>();
        _mockConsoleWriter = Substitute.For<IConsoleWriter>();
        _mockFileSystem = Substitute.For<IFileSystem>();
        _mockConsoleSpinner = Substitute.For<IConsoleSpinner>();
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
        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = null,
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Missing version argument");
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionIsEmpty_ShouldReturnError()
    {
        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "",
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Missing version argument");
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionAlreadyInstalledAndNotForced_ShouldReturnError()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };

        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockNodeJsWebApi.GetLatestNodeVersion().Returns(version);

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = version.ToString(),
            Force = false,
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine($"{version} already installed");
    }

    [TestMethod]
    public async Task RunAsync_WhenLatestRequested_ShouldCallGetLatestNodeVersion()
    {
        var latestVersion = new Version(20, 11, 0);
        _mockNodeJsWebApi.GetLatestNodeVersion().Returns(latestVersion);
        _mockNodeJsWebApi.GetDownloadUrl(latestVersion).Returns("https://example.com/node.zip");
        _mockNodeJs.GetInstalledVersions().Returns([]);

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "latest",
        };

        await command.RunAsync();

        await _mockNodeJsWebApi.Received(1).GetLatestNodeVersion();
        _mockNodeJsWebApi.Received(1).GetDownloadUrl(latestVersion);
    }

    [TestMethod]
    public async Task RunAsync_WhenFuzzyVersionRequested_ShouldCallGetLatestNodeVersionWithPrefix()
    {
        var resolvedVersion = new Version(18, 17, 0);
        _mockNodeJsWebApi.GetLatestNodeVersion("18").Returns(resolvedVersion);
        _mockNodeJsWebApi.GetDownloadUrl(resolvedVersion).Returns("https://example.com/node.zip");
        _mockNodeJs.GetInstalledVersions().Returns([]);

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "18",
        };

        await command.RunAsync();

        await _mockNodeJsWebApi.Received(1).GetLatestNodeVersion("18");
        _mockNodeJsWebApi.Received(1).GetDownloadUrl(resolvedVersion);
    }

    [TestMethod]
    public async Task RunAsync_WhenSpecificVersionRequested_ShouldParseVersion()
    {
        var specificVersion = new Version(18, 17, 0);
        _mockNodeJsWebApi.GetDownloadUrl(specificVersion).Returns("https://example.com/node.zip");
        _mockNodeJs.GetInstalledVersions().Returns([]);

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "18.17.0",
        };

        await command.RunAsync();

        _mockNodeJsWebApi.Received(1).GetDownloadUrl(specificVersion);
    }

    [TestMethod]
    public async Task RunAsync_WhenForceInstallOnExisting_ShouldProceedWithInstall()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };

        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockNodeJsWebApi.GetDownloadUrl(version).Returns("https://example.com/node.zip");

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = version.ToString(),
            Force = true,
        };

        await command.RunAsync();

        _mockConsoleWriter.DidNotReceive().WriteErrorLine(Arg.Is<string>(msg => msg.Contains("already installed")));
        _mockNodeJsWebApi.Received(1).GetDownloadUrl(version);
    }

    [TestMethod]
    public async Task RunAsync_WhenWebApiThrowsException_ShouldReturnError()
    {
        _mockNodeJsWebApi.GetLatestNodeVersion().Returns<Version>(_ => throw new Exception("Test exception"));

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "latest",
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Error determining version: Test exception");
    }

    [TestMethod]
    public async Task RunAsync_WhenInvalidVersionFormat_ShouldReturnError()
    {
        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "invalid.version.format",
        };

        var result = await command.RunAsync();

        result.ShouldBe(1);
        _mockConsoleWriter.Received().WriteErrorLine(Arg.Is<string>(msg => msg.Contains("Error determining version")));
    }

    [TestMethod]
    public async Task RunAsync_WhenGetDownloadUrlCalled_ShouldUseCorrectVersion()
    {
        var version = new Version(20, 5, 1);
        _mockNodeJsWebApi.GetDownloadUrl(version).Returns("https://nodejs.org/dist/v20.5.1/node-v20.5.1-win-x64.zip");
        _mockNodeJs.GetInstalledVersions().Returns([]);

        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = "20.5.1",
        };

        await command.RunAsync();

        _mockNodeJsWebApi.Received(1).GetDownloadUrl(version);
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionCheckCompletes_ShouldCallGetInstalledVersions()
    {
        var version = new Version(18, 17, 0);
        _mockNodeJsWebApi.GetDownloadUrl(version).Returns("https://example.com/node.zip");
        _mockNodeJs.GetInstalledVersions().Returns([]);

        _mockConsoleSpinner = Substitute.For<IConsoleSpinner>();
        var command = new InstallCommand(_globalContext, _mockNodeJsWebApi, _mockNodeJs, _mockConsoleWriter, _mockFileSystem, _mockConsoleSpinner)
        {
            Version = version.ToString(),
        };

        await command.RunAsync();

        _mockNodeJs.Received().GetInstalledVersions();
    }
}