using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Tests.TestUtils;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class FileCommandTests
{
    private string _testDirectory;
    private string _nodeSwapFilePath;
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
        _nodeSwapFilePath = Path.Combine(_testDirectory, ".nodeswap");

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage"),
            ActiveVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "last-used"),
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
    public void Run_WhenNodeSwapFileExists_ShouldUseVersionFromFile()
    {
        const string testVersion = "18.17.0";
        var version = new Version(18, 17, 0);

        _mockFileSystem.FileExistsReturn = true;
        _mockFileSystem.ReadAllTextReturn = testVersion;

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockConsoleWriter.Messages.ShouldContain($"Using Node.js version from .nodeswap: {testVersion}");
            _mockConsoleWriter.Messages.ShouldContain("Done");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileExistsButEmpty_ShouldReturnError()
    {
        _mockFileSystem.FileExistsReturn = true;
        _mockFileSystem.ReadAllTextReturn = "";

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.ErrorMessages.ShouldContain("The .nodeswap file is empty");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileExistsButWhitespace_ShouldReturnError()
    {
        _mockFileSystem.FileExistsReturn = true;
        _mockFileSystem.ReadAllTextReturn = "   \n\t   ";

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.ErrorMessages.ShouldContain("The .nodeswap file is empty");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileDoesNotExistAndNoActiveVersion_ShouldReturnError()
    {
        _mockFileSystem.FileExistsReturn = false;
        _mockNodeJs.ActiveVersion = null;

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.ErrorMessages.ShouldContain(
                "No active Node.js version found. Please use 'nodeswap use <version>' to set a version first.");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileDoesNotExistAndActiveVersionExists_ShouldCreateFile()
    {
        var activeVersion = new Version(20, 11, 0);

        _mockFileSystem.FileExistsReturn = false;
        _mockNodeJs.ActiveVersion = activeVersion;

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockFileSystem.WriteAllTextCalls.ShouldContainKey(_nodeSwapFilePath);
            _mockFileSystem.WriteAllTextCalls[_nodeSwapFilePath].ShouldBe(activeVersion.ToString());
            _mockConsoleWriter.Messages.ShouldContain(
                $"Created .nodeswap with current Node.js version: {activeVersion}");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void UseVersionFromFile_ShouldTrimWhitespace()
    {
        const string testVersionWithWhitespace = "  18.17.0  \n";
        const string trimmedVersion = "18.17.0";
        var version = new Version(18, 17, 0);

        _mockFileSystem.FileExistsReturn = true;
        _mockFileSystem.ReadAllTextReturn = testVersionWithWhitespace;

        _mockNodeJs.InstalledVersions.Add(new NodeJsVersion
        {
            Version = version,
            Path = $"/fake/path/node-v{version}",
            IsActive = false,
        });

        _mockProcessElevation.IsAdministratorReturn = true;
        _mockFileSystem.CreateSymbolicLinkReturn = true;

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(
                _globalContext,
                _mockNodeJs,
                _mockProcessElevation,
                _mockConsoleWriter,
                _mockFileSystem);
            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockConsoleWriter.Messages.ShouldContain($"Using Node.js version from .nodeswap: {trimmedVersion}");
            _mockConsoleWriter.Messages.ShouldContain("Done");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }
}