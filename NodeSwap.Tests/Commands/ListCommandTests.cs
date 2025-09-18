using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class ListCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private StringWriter _consoleOutput;
    private TextWriter _originalConsoleOut;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage"),
            ActiveVersionTrackerFilePath = Path.Combine(_testDirectory, "storage", "last-used"),
        };
        Directory.CreateDirectory(_globalContext.StoragePath);

        // Capture console output
        _consoleOutput = new StringWriter();
        _originalConsoleOut = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(_originalConsoleOut);
        _consoleOutput?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TestMethod]
    public void Run_WhenNoVersionsInstalled_ShouldDisplayNoneInstalled()
    {
        var nodeJs = new NodeJs(_globalContext);
        var listCommand = new ListCommand(nodeJs);
        listCommand.Run();

        var output = _consoleOutput.ToString();
        output.ShouldContain("None installed");
    }

    [TestMethod]
    public void Run_WhenVersionsInstalled_ShouldDisplayVersionsList()
    {
        var version1 = new Version(20, 11, 0);
        var version2 = new Version(18, 17, 0);
        
        Directory.CreateDirectory(Path.Combine(_globalContext.StoragePath, $"node-v{version1}"));
        Directory.CreateDirectory(Path.Combine(_globalContext.StoragePath, $"node-v{version2}"));
        
        // Set one as active
        File.WriteAllText(_globalContext.ActiveVersionTrackerFilePath, version2.ToString());

        var nodeJs = new NodeJs(_globalContext);
        var listCommand = new ListCommand(nodeJs);
        listCommand.Run();

        var output = _consoleOutput.ToString();
        output.ShouldContain("20.11.0");
        output.ShouldContain("18.17.0");
        output.ShouldContain("  * 18.17.0"); // Active version should have asterisk
        output.ShouldContain("    20.11.0"); // Inactive version should have spaces
    }

    [TestMethod]
    public void Run_WhenSingleVersionInstalled_ShouldDisplayCorrectFormat()
    {
        var version = new Version(16, 14, 0);
        Directory.CreateDirectory(Path.Combine(_globalContext.StoragePath, $"node-v{version}"));
        File.WriteAllText(_globalContext.ActiveVersionTrackerFilePath, version.ToString());

        var nodeJs = new NodeJs(_globalContext);
        var listCommand = new ListCommand(nodeJs);
        listCommand.Run();

        var output = _consoleOutput.ToString();
        output.ShouldContain("  * 16.14.0");
    }

    [TestMethod]
    public void Run_WhenMultipleVersionsWithoutActive_ShouldDisplayAllWithSpaces()
    {
        var version1 = new Version(20, 11, 0);
        var version2 = new Version(18, 17, 0);
        
        Directory.CreateDirectory(Path.Combine(_globalContext.StoragePath, $"node-v{version1}"));
        Directory.CreateDirectory(Path.Combine(_globalContext.StoragePath, $"node-v{version2}"));

        var nodeJs = new NodeJs(_globalContext);
        var listCommand = new ListCommand(nodeJs);
        listCommand.Run();

        var output = _consoleOutput.ToString();
        output.ShouldContain("    20.11.0");
        output.ShouldContain("    18.17.0");
        output.ShouldNotContain("  * ");
    }
}