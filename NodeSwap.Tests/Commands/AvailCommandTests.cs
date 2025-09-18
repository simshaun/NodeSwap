using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodeSwap.Commands;
using NodeSwap.Interfaces;
using NSubstitute;
using Shouldly;

namespace NodeSwap.Tests.Commands;

[TestClass]
public class AvailCommandTests
{
    private string _testDirectory;
    private GlobalContext _globalContext;
    private INodeJsWebApi _mockNodeJsWebApi;
    private IConsoleWriter _mockConsoleWriter;

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

        _mockNodeJsWebApi = Substitute.For<INodeJsWebApi>();
        _mockConsoleWriter = Substitute.For<IConsoleWriter>();
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
        _mockNodeJsWebApi.GetInstallableNodeVersions("").Returns(testVersions);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("");
        _mockConsoleWriter.Received(2).WriteLine("");
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
        _mockNodeJsWebApi.GetInstallableNodeVersions(null).Returns(testVersions);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = null };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions(null);
        _mockConsoleWriter.Received(2).WriteLine("");
    }

    [TestMethod]
    public async Task RunAsync_WhenSpecificPrefix_ShouldCallWithCorrectPrefix()
    {
        var filteredVersions = new List<Version> { new(18, 17, 0), new(18, 16, 0) };
        _mockNodeJsWebApi.GetInstallableNodeVersions("18").Returns(filteredVersions);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "18" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("18");
        _mockConsoleWriter.Received(2).WriteLine("");
    }

    [TestMethod]
    public async Task RunAsync_WhenNoVersionsFound_ShouldReturnError()
    {
        _mockNodeJsWebApi.GetInstallableNodeVersions("99").Returns(new List<Version>());

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "99" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(1);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("99");
        _mockConsoleWriter.Received(1).WriteLine("None found");
        _mockConsoleWriter.DidNotReceive().WriteLine("");
    }

    [TestMethod]
    public async Task RunAsync_WhenWebApiThrowsException_ShouldReturnError()
    {
        _mockNodeJsWebApi.GetInstallableNodeVersions("").Returns<List<Version>>(x => throw new Exception("Test exception"));

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(1);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("");
        _mockConsoleWriter.Received(1).WriteErrorLine("Test exception");
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
        _mockNodeJsWebApi.GetInstallableNodeVersions("").Returns(testVersions);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("");
        testVersions.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task RunAsync_WhenExactVersionPrefix_ShouldReturnSuccessfully()
    {
        var singleVersion = new List<Version> { new(18, 17, 0) };
        _mockNodeJsWebApi.GetInstallableNodeVersions("18.17.0").Returns(singleVersion);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "18.17.0" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("18.17.0");
        _mockConsoleWriter.Received(2).WriteLine("");
    }

    [TestMethod]
    public async Task RunAsync_WhenEmptyStringPrefix_ShouldCallGetInstallableVersions()
    {
        var testVersions = new List<Version>
        {
            new(20, 11, 0),
            new(18, 17, 0),
        };
        _mockNodeJsWebApi.GetInstallableNodeVersions("").Returns(testVersions);

        var availCommand = new AvailCommand(_mockNodeJsWebApi, _mockConsoleWriter) { Prefix = "" };

        var result = await availCommand.RunAsync();

        result.ShouldBe(0);
        await _mockNodeJsWebApi.Received(1).GetInstallableNodeVersions("");
        _mockConsoleWriter.Received(2).WriteLine("");
    }
}