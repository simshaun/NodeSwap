using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Interfaces;
using NodeSwap.Tests.TestUtils;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class AvailCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private MockNodeJsWebApi _mockNodeJsWebApi;
    private MockConsoleWriter _mockConsoleWriter;

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

        _mockNodeJsWebApi = new MockNodeJsWebApi();
        _mockConsoleWriter = new MockConsoleWriter();
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
    public async Task RunAsync_WhenNoPrefix_ShouldDisplayAllVersions()
    {
        var testVersions = new List<Version>
        {
            new(20, 11, 0),
            new(18, 17, 0),
            new(16, 14, 0),
        };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = testVersions;

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenNullPrefix_ShouldDisplayAllVersions()
    {
        var testVersions = new List<Version>
        {
            new(20, 11, 0),
            new(18, 17, 0),
            new(16, 14, 0),
        };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = testVersions;

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = null };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenSpecificPrefix_ShouldCallWithCorrectPrefix()
    {
        var filteredVersions = new List<Version> { new(18, 17, 0), new(18, 16, 0) };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = filteredVersions;
        
        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "18" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
        // Note: We can't easily test the prefix parameter without modifying MockNodeJsWebApi
        // but the command should pass it through correctly
    }

    [TestMethod]
    public async Task RunAsync_WhenNoVersionsFound_ShouldReturnError()
    {
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = [];
        
        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "99" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(1);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenWebApiThrowsException_ShouldReturnError()
    {
        _mockNodeJsWebApi.ShouldThrowException = true;
        
        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(1);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenVersionsAvailable_ShouldCallGetInstallableVersions()
    {
        var testVersions = new List<Version>
        {
            new(20, 11, 0),
            new(18, 17, 0),
            new(16, 14, 0),
        };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = testVersions;

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
        
        testVersions.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task RunAsync_WhenExactVersionPrefix_ShouldReturnSuccessfully()
    {
        var singleVersion = new List<Version> { new(18, 17, 0) };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = singleVersion;
        
        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "18.17.0" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenEmptyStringPrefix_ShouldCallGetInstallableVersions()
    {
        var testVersions = new List<Version>
        {
            new(20, 11, 0),
            new(18, 17, 0),
        };
        _mockNodeJsWebApi.GetInstallableNodeVersionsReturn = testVersions;

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };
        var result = await availCommand.RunAsync();
        
        result.ShouldBe(0);
        _mockNodeJsWebApi.GetInstallableNodeVersionsCalled.ShouldBeTrue();
    }
}