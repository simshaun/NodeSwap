using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class UninstallCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private StringWriter _consoleOutput;
    private StringWriter _consoleError;
    private TextWriter _originalConsoleOut;
    private TextWriter _originalConsoleError;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _globalContext = new GlobalContext
        {
            StoragePath = Path.Combine(_testDirectory, "storage")
        };
        Directory.CreateDirectory(_globalContext.StoragePath);

        _consoleOutput = new StringWriter();
        _consoleError = new StringWriter();
        _originalConsoleOut = Console.Out;
        _originalConsoleError = Console.Error;
        Console.SetOut(_consoleOutput);
        Console.SetError(_consoleError);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(_originalConsoleOut);
        Console.SetError(_originalConsoleError);
        _consoleOutput?.Dispose();
        _consoleError?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TestMethod]
    public void Run_WhenVersionIsNull_ShouldReturnError()
    {
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = null };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(1);
        _consoleError.ToString().ShouldContain("Missing version argument");
    }

    [TestMethod]
    public void Run_WhenVersionIsInvalid_ShouldReturnError()
    {
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = "invalid.version" };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(1);
        _consoleError.ToString().ShouldContain("Invalid version argument");
    }

    [TestMethod]
    public void Run_WhenVersionIsFuzzy_ShouldReturnError()
    {
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = "18.17" };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(1);
        _consoleError.ToString().ShouldContain("Invalid version argument");
    }

    [TestMethod]
    public void Run_WhenVersionNotInstalled_ShouldReturnError()
    {
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = "99.99.99" };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(1);
        _consoleError.ToString().ShouldContain("99.99.99 not installed");
    }

    [TestMethod]
    public void Run_WhenVersionInstalled_ShouldUninstallSuccessfully()
    {
        var version = new Version(18, 17, 0);
        var versionPath = Path.Combine(_globalContext.StoragePath, $"node-v{version}");
        Directory.CreateDirectory(versionPath);
        
        var testFile = Path.Combine(versionPath, "node.exe");
        File.WriteAllText(testFile, "test content");

        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = version.ToString() };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(0);
        _consoleOutput.ToString().ShouldContain("Done");
        Directory.Exists(versionPath).ShouldBeFalse();
    }

    [TestMethod]
    public void Run_WhenVersionInstalledButDirectoryInUse_ShouldReturnError()
    {
        var version = new Version(18, 17, 0);
        var versionPath = Path.Combine(_globalContext.StoragePath, $"node-v{version}");
        Directory.CreateDirectory(versionPath);
        
        // Create and keep a file handle open to simulate directory in use
        var testFile = Path.Combine(versionPath, "node.exe");
        using var fileStream = File.Create(testFile);

        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = version.ToString() };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(1);
        _consoleError.ToString().ShouldContain($"Unable to delete {versionPath}");
    }

    [TestMethod]
    public void Run_WhenActiveVersionUninstalled_ShouldStillProceed()
    {
        var version = new Version(18, 17, 0);
        var versionPath = Path.Combine(_globalContext.StoragePath, $"node-v{version}");
        Directory.CreateDirectory(versionPath);
        
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = version.ToString() };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(0);
        _consoleOutput.ToString().ShouldContain("Done");
        Directory.Exists(versionPath).ShouldBeFalse();
    }

    [TestMethod]
    public void Run_WhenValidVersionWithVPrefix_ShouldUninstallSuccessfully()
    {
        var version = new Version(18, 17, 0);
        var versionPath = Path.Combine(_globalContext.StoragePath, $"node-v{version}");
        Directory.CreateDirectory(versionPath);
        
        var nodeJs = new NodeJs(_globalContext);
        var uninstallCommand = new UninstallCommand(nodeJs) { Version = $"v{version}" };
        var result = uninstallCommand.Run();
        
        result.ShouldBe(0);
        _consoleOutput.ToString().ShouldContain("Done");
        Directory.Exists(versionPath).ShouldBeFalse();
    }
}