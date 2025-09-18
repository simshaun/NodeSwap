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
public class FileCommandTests
{
    private string _testDirectory;
    private string _nodeSwapFilePath;
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
        _nodeSwapFilePath = Path.Combine(_testDirectory, ".nodeswap");

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage"),
            ActiveVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "last-used"),
        };

        _mockNodeJs = Substitute.For<INodeJs>();
        _mockProcessElevation = Substitute.For<IProcessElevation>();
        _mockConsoleWriter = Substitute.For<IConsoleWriter>();
        _mockFileSystem = Substitute.For<IFileSystem>();
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileExists_ShouldUseVersionFromFile()
    {
        const string testVersion = "18.17.0";
        var version = new Version(18, 17, 0);
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };

        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(true);
        _mockFileSystem.ReadAllText(_nodeSwapFilePath).Returns(testVersion);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockConsoleWriter.Received(1).WriteLine($"Using Node.js version from .nodeswap: {testVersion}");
            _mockConsoleWriter.Received(1).WriteLine("Done");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileExistsButEmpty_ShouldReturnError()
    {
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(true);
        _mockFileSystem.ReadAllText(_nodeSwapFilePath).Returns("");

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.Received(1).WriteErrorLine("The .nodeswap file is empty");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileExistsButWhitespace_ShouldReturnError()
    {
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(true);
        _mockFileSystem.ReadAllText(_nodeSwapFilePath).Returns("   \n\t   ");

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.Received(1).WriteErrorLine("The .nodeswap file is empty");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenNodeSwapFileDoesNotExistAndNoActiveVersion_ShouldReturnError()
    {
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(false);
        _mockNodeJs.GetActiveVersion().Returns((Version) null);

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.Received(1).WriteErrorLine("No active Node.js version found. Please use 'nodeswap use <version>' to set a version first.");
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
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(false);
        _mockNodeJs.GetActiveVersion().Returns(activeVersion);

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockFileSystem.Received(1).WriteAllText(_nodeSwapFilePath, activeVersion.ToString());
            _mockConsoleWriter.Received(1).WriteLine($"Created .nodeswap with current Node.js version: {activeVersion}");
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
        var installedVersions = new List<NodeJsVersion>
        {
            new() { Version = version, Path = $"/fake/path/node-v{version}", IsActive = false },
        };

        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(true);
        _mockFileSystem.ReadAllText(_nodeSwapFilePath).Returns(testVersionWithWhitespace);
        _mockNodeJs.GetInstalledVersions().Returns(installedVersions);
        _mockProcessElevation.IsAdministrator().Returns(true);
        _mockFileSystem.CreateSymbolicLink(Arg.Any<string>(), Arg.Any<string>(), true).Returns(true);

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(0);
            _mockConsoleWriter.Received(1).WriteLine($"Using Node.js version from .nodeswap: {trimmedVersion}");
            _mockConsoleWriter.Received(1).WriteLine("Done");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenFileReadThrowsException_ShouldReturnError()
    {
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(true);
        _mockFileSystem.ReadAllText(_nodeSwapFilePath).Returns(_ => throw new IOException("Test exception"));

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.Received(1).WriteErrorLine("Error reading .nodeswap: Test exception");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    public void Run_WhenFileWriteThrowsException_ShouldReturnError()
    {
        var activeVersion = new Version(20, 11, 0);
        _mockFileSystem.FileExists(_nodeSwapFilePath).Returns(false);
        _mockNodeJs.GetActiveVersion().Returns(activeVersion);
        _mockFileSystem
            .When(x => x.WriteAllText(_nodeSwapFilePath, activeVersion.ToString()))
            .Do(_ => throw new IOException("Test exception"));

        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            var fileCommand = new FileCommand(_globalContext, _mockNodeJs, _mockProcessElevation, _mockConsoleWriter, _mockFileSystem);

            var result = fileCommand.Run();

            result.ShouldBe(1);
            _mockConsoleWriter.Received(1).WriteErrorLine("Error creating .nodeswap: Test exception");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }
}