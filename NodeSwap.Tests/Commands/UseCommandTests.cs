using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Interfaces;
using NSubstitute;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class UseCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private INodeJs _mockNodeJs;
    private IProcessElevation _mockProcessElevation;
    private IConsoleWriter _mockConsoleWriter;
    private IFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage"),
            SymlinkPath = Path.Combine(_testDirectory, "storage", "current"),
            ActiveVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "last-used"),
            PreviousVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "previous-used"),
        };
        Directory.CreateDirectory(_globalContext.StoragePath);

        _mockNodeJs = Substitute.For<INodeJs>();
        _mockProcessElevation = Substitute.For<IProcessElevation>();
        _mockConsoleWriter = Substitute.For<IConsoleWriter>();
        _mockFileSystem = Substitute.For<IFileSystem>();
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
    public void Run_WhenVersionIsNull_ShouldReturnError()
    {
        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Missing version argument");
    }

    [TestMethod]
    public void Run_WhenVersionIsInvalid_ShouldReturnError()
    {
        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = "invalid.version",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Invalid version argument: invalid.version");
    }

    [TestMethod]
    public void Run_WhenVersionNotInstalled_ShouldReturnError()
    {
        _mockNodeJs.GetInstalledVersions().Returns([]);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = "99.99.99",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("99.99.99 not installed");
    }

    [TestMethod]
    public void Run_WhenLatestRequestedButNoneInstalled_ShouldReturnError()
    {
        _mockNodeJs.GetLatestInstalledVersion().Returns((NodeJsVersion)null);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = "latest",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("There are no versions installed");
    }

    [TestMethod]
    public void Run_WhenNotAdministrator_ShouldAttemptElevation()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(false);
        _mockProcessElevation.ElevateApplication().Returns(42);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(42);
        _mockProcessElevation.Received(1).ElevateApplication();
    }

    [TestMethod]
    public void Run_WhenAdministratorAndVersionInstalled_ShouldSwitchSuccessfully()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.DirectoryExists(_globalContext.SymlinkPath).Returns(false);
        _mockFileSystem.CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true).Returns(true);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine("Done");
        _mockFileSystem.Received(1).CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true);
        _mockFileSystem.Received(1).WriteAllText(_globalContext.ActiveVersionTrackerFilePath, version.ToString());
    }

    [TestMethod]
    public void Run_WhenSymlinkCreationFails_ShouldReturnError()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.DirectoryExists(_globalContext.SymlinkPath).Returns(false);
        _mockFileSystem.CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true).Returns(false);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received().WriteErrorLine(Arg.Is<string>(msg => msg.Contains("Unable to create the symlink")));
    }

    [TestMethod]
    public void Run_WithLatestVersion_ShouldUseLatestInstalled()
    {
        var version = new Version(20, 11, 0);
        var latestVersion = new NodeJsVersion { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false };

        _mockNodeJs.GetLatestInstalledVersion().Returns(latestVersion);
        _mockNodeJs.GetActiveVersion().Returns((Version)null);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = "latest",
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine("Done");
        _mockFileSystem.Received(1).CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true);
    }

    [TestMethod]
    public void Run_WhenExistingSymlinkExists_ShouldDeleteFirst()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.DirectoryExists(_globalContext.SymlinkPath).Returns(true);
        _mockFileSystem.CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true).Returns(true);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockFileSystem.Received(1).DeleteDirectory(_globalContext.SymlinkPath, true);
        _mockFileSystem.Received(1).CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{version}", true);
    }
}