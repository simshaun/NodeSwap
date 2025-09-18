#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodeSwap.Interfaces;

namespace NodeSwap.Tests.TestUtils;

public class MockProcessElevation : IProcessElevation
{
    public bool IsAdministratorReturn { get; set; } = true;
    public int ElevateApplicationReturn { get; set; }
    public bool ElevateApplicationCalled { get; private set; }

    public bool IsAdministrator() => IsAdministratorReturn;

    public int ElevateApplication()
    {
        ElevateApplicationCalled = true;
        return ElevateApplicationReturn;
    }
}

public class MockConsoleWriter : IConsoleWriter
{
    public List<string> Messages { get; } = [];
    public List<string> ErrorMessages { get; } = [];

    public void WriteLine(string message) => Messages.Add(message);
    public void WriteErrorLine(string message) => ErrorMessages.Add(message);
}

public class MockFileSystem : IFileSystem
{
    public Dictionary<string, string> WriteAllTextCalls { get; } = new();
    public List<string> DeleteDirectoryCalls { get; } = [];
    public bool DirectoryExistsReturn { get; set; } = false;
    public bool CreateSymbolicLinkReturn { get; set; } = true;
    public bool CreateSymbolicLinkCalled { get; private set; }
    public bool FileExistsReturn { get; set; } = false;
    public string ReadAllTextReturn { get; set; } = "";

    public bool FileExists(string path) => FileExistsReturn;
    public string ReadAllText(string path) => ReadAllTextReturn;
    public void WriteAllText(string path, string contents) => WriteAllTextCalls[path] = contents;

    public void DeleteFile(string path)
    {
    }

    public bool DirectoryExists(string path) => DirectoryExistsReturn;

    public void CreateDirectory(string path)
    {
    }

    public void DeleteDirectory(string path, bool recursive = false) => DeleteDirectoryCalls.Add(path);

    public string[] GetDirectories(
        string path,
        string searchPattern = "*",
        System.IO.SearchOption searchOption = System.IO.SearchOption.TopDirectoryOnly
    ) => [];

    public bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        CreateSymbolicLinkCalled = true;
        return CreateSymbolicLinkReturn;
    }
}

public class MockNodeJs : INodeJs
{
    public List<NodeJsVersion> InstalledVersions { get; set; } = [];
    public Version? ActiveVersion { get; set; }
    public Version? PreviousVersion { get; set; }

    public NodeJsVersion? GetLatestInstalledVersion()
    {
        return InstalledVersions.FirstOrDefault();
    }

    public List<NodeJsVersion> GetInstalledVersions()
    {
        return InstalledVersions;
    }

    public Version? GetActiveVersion()
    {
        return ActiveVersion;
    }

    public Version? GetPreviousVersion()
    {
        return PreviousVersion;
    }
}

public class MockNodeJsWebApi : INodeJsWebApi
{
    public Version GetLatestNodeVersionReturn { get; set; } = new(20, 11, 0);
    public Version GetLatestNodeVersionWithPrefixReturn { get; set; } = new(18, 17, 0);
    public List<Version> GetInstallableNodeVersionsReturn { get; set; } = [];
    public string GetDownloadUrlReturn { get; set; } = "https://example.com/node.zip";
    public bool ShouldThrowException { get; set; } = false;

    public bool GetLatestNodeVersionCalled { get; private set; }
    public bool GetLatestNodeVersionWithPrefixCalled { get; private set; }
    public bool GetInstallableNodeVersionsCalled { get; private set; }
    public bool GetDownloadUrlCalled { get; private set; }
    public string? LastPrefixUsed { get; private set; }

    public Task<Version> GetLatestNodeVersion(string? prefix = null)
    {
        if (ShouldThrowException)
            throw new Exception("Test exception");

        if (!string.IsNullOrEmpty(prefix))
        {
            GetLatestNodeVersionWithPrefixCalled = true;
            LastPrefixUsed = prefix;
            return Task.FromResult(GetLatestNodeVersionWithPrefixReturn);
        }

        GetLatestNodeVersionCalled = true;
        return Task.FromResult(GetLatestNodeVersionReturn);
    }

    public Task<List<Version>> GetInstallableNodeVersions(string prefix = "")
    {
        GetInstallableNodeVersionsCalled = true;
        
        if (ShouldThrowException)
            throw new Exception("Test exception");

        return Task.FromResult(GetInstallableNodeVersionsReturn);
    }

    public string GetDownloadUrl(Version version)
    {
        GetDownloadUrlCalled = true;
        return GetDownloadUrlReturn;
    }
}