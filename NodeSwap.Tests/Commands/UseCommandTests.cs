using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Tests.TestUtils;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class UseCommandTests
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
    public void Run_WhenVersionIsNull_ShouldReturnError()
    {
        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = null,
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("Missing version argument");
    }

    [TestMethod]
    public void Run_WhenVersionIsInvalid_ShouldReturnError()
    {
        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = "invalid.version",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("Invalid version argument: invalid.version");
    }

    [TestMethod]
    public void Run_WhenVersionNotInstalled_ShouldReturnError()
    {
        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = "99.99.99",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("99.99.99 not installed");
    }

    [TestMethod]
    public void Run_WhenLatestRequestedButNoneInstalled_ShouldReturnError()
    {
        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = "latest",
        };

        var result = useCommand.Run();

        result.ShouldBe(1);
        _mockConsoleWriter.ErrorMessages.ShouldContain("There are no versions installed");
    }

    [TestMethod]
    public void Run_WhenNotAdministrator_ShouldAttemptElevation()
    {
        // Setup installed version in mock
        var version = new Version(18, 17, 0);
        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = false;
        _mockProcessElevation.ElevateApplicationReturn = 42;

        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(42);
        _mockProcessElevation.ElevateApplicationCalled.ShouldBeTrue();
    }

    [TestMethod]
    public void Run_WhenAdministratorAndVersionInstalled_ShouldSwitchSuccessfully()
    {
        // Setup installed version in mock
        var version = new Version(18, 17, 0);
        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.DirectoryExistsReturn = false; // No existing symlink
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = version.ToString(),
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Messages.ShouldContain("Done");
        _mockFileSystem.CreateSymbolicLinkCalled.ShouldBeTrue();
        _mockFileSystem.WriteAllTextCalls.ShouldContainKey(_globalContext.ActiveVersionTrackerFilePath);
        _mockFileSystem.WriteAllTextCalls[_globalContext.ActiveVersionTrackerFilePath].ShouldBe(version.ToString());
    }

    [TestMethod]
    public void Run_WithLatestVersion_ShouldUseLatestInstalled()
    {
        // Setup multiple installed versions in mock (ordered by version descending)
        var version1 = new Version(18, 17, 0);
        var version2 = new Version(20, 11, 0);
        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version2, // Latest version first
            Path = $"/fake/path/node-v{version2}",
            IsActive = false,
        });
        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version1,
            Path = $"/fake/path/node-v{version1}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var useCommand = new UseCommand(
            _globalContext,
            _mockNodeJs,
            _mockProcessElevation,
            _mockConsoleWriter,
            _mockFileSystem
        )
        {
            Version = "latest",
        };

        var result = useCommand.Run();

        result.ShouldBe(0);
        _mockConsoleWriter.Messages.ShouldContain("Done");
    }
}