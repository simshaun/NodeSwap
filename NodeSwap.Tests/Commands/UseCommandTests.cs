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
    public void Run_WhenVersionIsNull_AndNoNodeSwapFile_ShouldReturnError()
    {
        _mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Missing version argument. Either provide a version or create a .nodeswap file.");
    }

    [TestMethod]
    public void Run_WhenVersionIsNull_AndNodeSwapFileExists_ShouldUseVersionFromFile()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };

        _mockFileSystem.FileExists(Arg.Is<string>(path => path.EndsWith(".nodeswap"))).Returns(true);
        _mockFileSystem.ReadAllText(Arg.Is<string>(path => path.EndsWith(".nodeswap"))).Returns("18.17.0");
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine("Using Node.js version from .nodeswap: 18.17.0");
        _mockConsoleWriter.Received(1).WriteLine("Done");
    }

    [TestMethod]
    public void Run_WhenVersionIsNull_AndNodeSwapFileIsEmpty_ShouldReturnError()
    {
        _mockFileSystem.FileExists(Arg.Is<string>(path => path.EndsWith(".nodeswap"))).Returns(true);
        _mockFileSystem.ReadAllText(Arg.Is<string>(path => path.EndsWith(".nodeswap"))).Returns("   ");

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("The .nodeswap file is empty");
    }

    [TestMethod]
    public void Run_WhenVersionIsNull_AndNodeSwapFileReadFails_ShouldReturnError()
    {
        _mockFileSystem.FileExists(Arg.Is<string>(path => path.EndsWith(".nodeswap"))).Returns(true);
        _mockFileSystem.When(x => x.ReadAllText(Arg.Is<string>(path => path.EndsWith(".nodeswap"))))
                      .Do(x => throw new IOException("File access denied"));

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("Error reading .nodeswap: File access denied");
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
        _mockNodeJs.GetActiveVersion().Returns((Version)null); // No active version
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
        _mockNodeJs.GetActiveVersion().Returns((Version)null); // No active version
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
        _mockNodeJs.GetActiveVersion().Returns((Version)null); // No active version
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

    [TestMethod]
    public void Run_WhenAlreadyUsingRequestedVersion_ShouldReturnEarlyWithoutChanges()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockNodeJs.GetActiveVersion().Returns(version); // Already using this version
        _mockProcessElevation.IsAdministrator().Returns(true);

        var useCommand = new UseCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem)
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine($"Already using Node.js version {version}");
        
        // Should not perform any file operations
        _mockFileSystem.DidNotReceive().WriteAllText(_globalContext.PreviousVersionTrackerFilePath, Arg.Any<string>());
        _mockFileSystem.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
        _mockFileSystem.DidNotReceive().CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        _mockFileSystem.DidNotReceive().WriteAllText(_globalContext.ActiveVersionTrackerFilePath, Arg.Any<string>());
    }
}