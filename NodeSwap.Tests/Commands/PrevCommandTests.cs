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
public class PrevCommandTests
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
    public void Run_WhenNoPreviousVersion_ShouldReturnError()
    {
        _mockNodeJs.GetPreviousVersion().Returns((Version?)null);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteErrorLine("No previous version found");
    }

    [TestMethod]
    public void Run_WhenPreviousVersionExists_ShouldDisplaySwappingMessage()
    {
        var previousVersion = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = previousVersion, Path = $"/fake/path/node-v{previousVersion}", IsActive = false },
        };

        _mockNodeJs.GetPreviousVersion().Returns(previousVersion);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {previousVersion}");
        _mockConsoleWriter.Received(1).WriteLine("Done");
    }

    [TestMethod]
    public void Run_WhenNotAdministrator_ShouldAttemptElevation()
    {
        var previousVersion = new Version(18, 17, 0);
        _mockNodeJs.GetPreviousVersion().Returns(previousVersion);
        _mockProcessElevation.IsAdministrator().Returns(false);
        _mockProcessElevation.ElevateApplication().Returns(42);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(42);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {previousVersion}");
        _mockProcessElevation.Received(1).ElevateApplication();
    }

    [TestMethod]
    public void Run_WhenPreviousVersionNotInstalled_ShouldFailInUseCommand()
    {
        var previousVersion = new Version(99, 99, 99);
        _mockNodeJs.GetPreviousVersion().Returns(previousVersion);
        _mockNodeJs.GetInstalledVersions().Returns(new List<NodeJsVersion>());
        _mockProcessElevation.IsAdministrator().Returns(true);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {previousVersion}");
        _mockConsoleWriter.Received(1).WriteErrorLine($"{previousVersion} not installed");
    }

    [TestMethod]
    public void Run_WhenPreviousVersionSameAsCurrent_ShouldStillAttemptSwap()
    {
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = true },
        };

        _mockNodeJs.GetPreviousVersion().Returns(version);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {version}");
        _mockConsoleWriter.Received(1).WriteLine("Done");
    }

    [TestMethod]
    public void Run_ShouldPassCorrectVersionToUseCommand()
    {
        var previousVersion = new Version(20, 11, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = previousVersion, Path = $"/fake/path/node-v{previousVersion}", IsActive = false },
        };

        _mockNodeJs.GetPreviousVersion().Returns(previousVersion);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {previousVersion}");
        _mockConsoleWriter.Received(1).WriteLine("Done");

        _mockFileSystem.Received(1).CreateSymbolicLink(_globalContext.SymlinkPath, $"/fake/path/node-v{previousVersion}", true);
        _mockFileSystem.Received(1).WriteAllText(_globalContext.ActiveVersionTrackerFilePath, previousVersion.ToString());
    }

    [TestMethod]
    public void Run_WhenUseCommandFails_ShouldReturnErrorCode()
    {
        var previousVersion = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = previousVersion, Path = $"/fake/path/node-v{previousVersion}", IsActive = false },
        };

        _mockNodeJs.GetPreviousVersion().Returns(previousVersion);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(false);

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

        var result = prevCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Received(1).WriteLine($"Swapping to {previousVersion}");
        _mockConsoleWriter.Received().WriteErrorLine(Arg.Is<string>(msg => msg.Contains("Unable to create the symlink")));
    }
}