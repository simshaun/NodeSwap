using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Interfaces;
using NodeSwap.Tests.TestUtils;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class PrevCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private MockNodeJs _mockNodeJs;
    private MockProcessElevation _mockProcessElevation;
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
            SymlinkPath = Path.Combine(_testDirectory, "storage", "current"),
            ActiveVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "last-used"),
            PreviousVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "previous-used"),
        };
        Directory.CreateDirectory(_globalContext.StoragePath);

        _mockNodeJs = new MockNodeJs();
        _mockProcessElevation = new MockProcessElevation();
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
    public void Run_WhenNoPreviousVersion_ShouldReturnError()
    {
        _mockNodeJs.PreviousVersion = null;

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("No previous version found");
    }

    [TestMethod]
    public void Run_WhenPreviousVersionExists_ShouldDisplaySwappingMessage()
    {
        var previousVersion = new Version(18, 17, 0);
        _mockNodeJs.PreviousVersion = previousVersion;

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = previousVersion,
            Path = $"/fake/path/node-v{previousVersion}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var prevCommand = new PrevCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Messages.ShouldContain($"Swapping to {previousVersion}");
        _mockConsoleWriter.Messages.ShouldContain("Done");
    }

    [TestMethod]
    public void Run_WhenNotAdministrator_ShouldAttemptElevation()
    {
        var previousVersion = new Version(18, 17, 0);
        _mockNodeJs.PreviousVersion = previousVersion;

        _mockProcessElevation.IsAdministratorReturn = false;
        _mockProcessElevation.ElevateApplicationReturn = 42;

        var prevCommand = new PrevCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(42);
        _mockConsoleWriter.Messages.ShouldContain($"Swapping to {previousVersion}");
        _mockProcessElevation.ElevateApplicationCalled.ShouldBeTrue();
    }

    [TestMethod]
    public void Run_WhenPreviousVersionNotInstalled_ShouldFailInUseCommand()
    {
        var previousVersion = new Version(99, 99, 99);
        _mockNodeJs.PreviousVersion = previousVersion;

        _mockProcessElevation.IsAdministratorReturn = true;

        var prevCommand = new PrevCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.Messages.ShouldContain($"Swapping to {previousVersion}");
        _mockConsoleWriter.ErrorMessages.ShouldContain($"{previousVersion} not installed");
    }

    [TestMethod]
    public void Run_WhenPreviousVersionSameAsCurrent_ShouldStillAttemptSwap()
    {
        var version = new Version(18, 17, 0);
        _mockNodeJs.PreviousVersion = version;

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = true,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var prevCommand = new PrevCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Messages.ShouldContain($"Swapping to {version}");
        _mockConsoleWriter.Messages.ShouldContain("Done");
    }

    [TestMethod]
    public void Run_ShouldPassCorrectVersionToUseCommand()
    {
        var previousVersion = new Version(20, 11, 0);
        _mockNodeJs.PreviousVersion = previousVersion;

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = previousVersion,
            Path = $"/fake/path/node-v{previousVersion}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var prevCommand = new PrevCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem);
        var result = prevCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Messages.ShouldContain($"Swapping to {previousVersion}");
        _mockConsoleWriter.Messages.ShouldContain("Done");

        // Verify symlink creation was called (indicating UseCommand executed successfully)
        _mockFileSystem.CreateSymbolicLinkCalled.ShouldBeTrue();
        _mockFileSystem.WriteAllTextCalls.ShouldContainKey(_globalContext.ActiveVersionTrackerFilePath);
        _mockFileSystem.WriteAllTextCalls[_globalContext.ActiveVersionTrackerFilePath]
            .ShouldBe(previousVersion.ToString());
    }
}